using System;
using System.Threading.Tasks;
using Finance.PciDss.Abstractions;
using Finance.PciDss.Bridge.Xpate.Server.Services.Extensions;
using Finance.PciDss.Bridge.Xpate.Server.Services.Integrations;
using Finance.PciDss.Bridge.Xpate.Server.Services.Integrations.Contracts.Responses;
using Finance.PciDss.PciDssBridgeGrpc;
using Finance.PciDss.PciDssBridgeGrpc.Contracts;
using Finance.PciDss.PciDssBridgeGrpc.Contracts.Enums;
using Flurl;
using MyCrm.AuditLog.Grpc;
using MyCrm.AuditLog.Grpc.Models;
using Newtonsoft.Json;
using Serilog;
using SimpleTrading.Common.Helpers;
using SimpleTrading.ConvertService.Grpc;
using SimpleTrading.ConvertService.Grpc.Contracts;
using SimpleTrading.GrpcTemplate;

namespace Finance.PciDss.Bridge.Xpate.Server.Services
{
    public class XpateEndpoint
    {
        public string PsCurrency { get; set; }
        public string EndpointId { get; set; }
    }

    public class XpateGrpcService : IFinancePciDssBridgeGrpcService
    {
        private const string PaymentSystemId = "pciDssXpateBankCards";
        private const string UsdCurrency = "USD";
        private const string EurCurrency = "EUR";
        private const string KztCurrency = "KZT";
        private readonly ILogger _logger;
        private readonly GrpcServiceClient<IMyCrmAuditLogGrpcService> _myCrmAuditLogGrpcService;
        private readonly ISettingsModelProvider _optionsMonitorSettingsModelProvider;
        private readonly IXpateHttpClient _xpateHttpClient;
        private readonly GrpcServiceClient<IConvertService> _convertServiceClient;

        public XpateGrpcService(IXpateHttpClient xpateHttpClient,
            GrpcServiceClient<IMyCrmAuditLogGrpcService> myCrmAuditLogGrpcService,
            GrpcServiceClient<IConvertService> convertServiceClient,
            ISettingsModelProvider optionsMonitorSettingsModelProvider,
            ILogger logger)
        {
            _xpateHttpClient = xpateHttpClient;
            _myCrmAuditLogGrpcService = myCrmAuditLogGrpcService;
            _convertServiceClient = convertServiceClient;
            _optionsMonitorSettingsModelProvider = optionsMonitorSettingsModelProvider;
            _logger = logger;
        }

        private SettingsModel _settingsModel => _optionsMonitorSettingsModelProvider.Get();

        public async ValueTask<MakeBridgeDepositGrpcResponse> MakeDepositAsync(MakeBridgeDepositGrpcRequest request)
        {
            _logger.Information("XpateGrpcService start process MakeBridgeDepositGrpcRequest {@request}", request);
            try
            {
                //Get endpoint for kyc verified/other traders
                var xpateEndpoint = GetEndpointId(request);

                //Modify request data
                request.PciDssInvoiceGrpcModel.KycVerification = string.IsNullOrEmpty(request.PciDssInvoiceGrpcModel.KycVerification) ?
                    "Empty" : request.PciDssInvoiceGrpcModel.KycVerification;
                request.PciDssInvoiceGrpcModel.Country = CountryManager.Iso3ToIso2(request.PciDssInvoiceGrpcModel.Country);

                var validateResult = request.Validate(_settingsModel);
                if (validateResult.IsFailed)
                {
                    _logger.Warning("Xpate request is not valid. Errors {@validateResult}", validateResult);
                    await SendMessageToAuditLogAsync(request.PciDssInvoiceGrpcModel,
                        $"Fail Xpate create invoice. Error {validateResult}");
                    return MakeBridgeDepositGrpcResponse.Failed(DepositBridgeRequestGrpcStatus.ServerError,
                        validateResult.ToString());
                }
                //Preapare invoice
                var createInvoiceRequest = request.PciDssInvoiceGrpcModel.ToCreatePaymentInvoiceRequest(_settingsModel);
                createInvoiceRequest.InitCheckSum(int.Parse(xpateEndpoint.EndpointId),
                    _settingsModel.XpateMerchantControl);

                _logger.Information("XpateGrpcService send invoice {@invoice}", createInvoiceRequest);
                await SendMessageToAuditLogAsync(request.PciDssInvoiceGrpcModel,
                    @"Xpate send sale request amount: {createInvoiceRequest.Amount} currency: {createInvoiceRequest.Currency}");
                
                var createInvoiceResult =
                    await _xpateHttpClient.RegisterInvoiceAsync(createInvoiceRequest, _settingsModel.XpatePciDssBaseUrl, xpateEndpoint.EndpointId);

                if (createInvoiceResult.IsFailed)
                {
                    await SendMessageToAuditLogAsync(request.PciDssInvoiceGrpcModel,
                        $"{PaymentSystemId}. Fail Xpate create invoice with kyc: {request.PciDssInvoiceGrpcModel.KycVerification}. Message: {createInvoiceResult.FailedResult.Message}. " +
                        $"Error: {JsonConvert.SerializeObject(createInvoiceResult.FailedResult.FieldError)}");
                    return MakeBridgeDepositGrpcResponse.Failed(DepositBridgeRequestGrpcStatus.ServerError,
                        createInvoiceResult.FailedResult.Message);
                }


                if (!string.IsNullOrEmpty(createInvoiceResult.SuccessResult.ErrorCode))
                {
                    await SendMessageToAuditLogAsync(request.PciDssInvoiceGrpcModel,
                        $"{PaymentSystemId}. Fail Xpate create invoice with kyc: {request.PciDssInvoiceGrpcModel.KycVerification}. Message: {createInvoiceResult.SuccessResult.ErrorMessage}. " +
                        $"Error: {JsonConvert.SerializeObject(createInvoiceResult.SuccessResult)}");
                    return MakeBridgeDepositGrpcResponse.Failed(DepositBridgeRequestGrpcStatus.ServerError,
                        createInvoiceResult.SuccessResult.ErrorMessage);
                }

                //Preapare status request
                var statusInvoiceRequest = createInvoiceResult.SuccessResult.ToStatusRequest(_settingsModel.XpateLogin);
                statusInvoiceRequest.InitCheckSum(_settingsModel.XpateMerchantControl);

                //var statusInvoceResult =
                //    await _xpateHttpClient.GetStatusInvoiceAsync(statusInvoiceRequest, _settingsModel.XpatePciDssBaseUrl, endpointId);

                Response<StatusPaymentResponse, StatusPaymentFailResponseDataPayment> statusInvoceResult = null;
                for (int i = 0; i < _settingsModel.XpateStatusRequestRetriesCount; i++)
                {
                    _logger.Information("Xpate send status request try: {@i} {@StatusRequest}", i, statusInvoiceRequest);
                    await SendMessageToAuditLogAsync(request.PciDssInvoiceGrpcModel,
                        $"Xpate send status request try: {i} ClientOrderId: {statusInvoiceRequest.ClientOrderId} " +
                        $"PaynetOrderId: {statusInvoiceRequest.PaynetOrderId} ControlSum: {statusInvoiceRequest.ControlSum}");

                    statusInvoceResult =
                        await _xpateHttpClient.GetStatusInvoiceAsync(statusInvoiceRequest, _settingsModel.XpatePciDssBaseUrl, xpateEndpoint.EndpointId);

                    if (!string.IsNullOrEmpty(statusInvoceResult.SuccessResult.RedirectUrl))
                    {
                        break;
                    }
                    await Task.Delay(_settingsModel.XpateStatusRequestRetriesDelayMs);
                }

                if (statusInvoceResult.IsFailed || statusInvoceResult.SuccessResult is null || statusInvoceResult.SuccessResult.IsFailed())
                {
                    _logger.Information("Fail Xpate status invoice. {@kyc} {@response}",
                        request.PciDssInvoiceGrpcModel.KycVerification, statusInvoceResult);
                    await SendMessageToAuditLogAsync(request.PciDssInvoiceGrpcModel,
                        $"Fail Xpate status invoice. Error {statusInvoceResult.FailedResult}");
                    return MakeBridgeDepositGrpcResponse.Failed(DepositBridgeRequestGrpcStatus.ServerError,
                        statusInvoceResult.FailedResult.Message);
                }

                if (statusInvoceResult.SuccessResult.IsSuccessWithoutRedirectTo3ds())
                {
                    statusInvoceResult.SuccessResult.RedirectUrl = _settingsModel.DefaultRedirectUrl
                        .SetQueryParam("orderId", request.PciDssInvoiceGrpcModel.OrderId);
                    _logger.Information("Xpate is success without redirect to 3ds. RedirectUrl {url} was built for traderId {traderId} and orderid {orderid} {kyc}",
                        statusInvoceResult.SuccessResult.RedirectUrl,
                        request.PciDssInvoiceGrpcModel.TraderId,
                        request.PciDssInvoiceGrpcModel.OrderId,
                        request.PciDssInvoiceGrpcModel.KycVerification);

                    await SendMessageToAuditLogAsync(request.PciDssInvoiceGrpcModel, 
                        $"Xpate was successful without redirect to 3ds. Orderid: {request.PciDssInvoiceGrpcModel.OrderId}");
                }
                else
                {
                    if (string.IsNullOrEmpty(statusInvoceResult.SuccessResult.RedirectUrl))
                        statusInvoceResult.SuccessResult.RedirectUrl = _settingsModel.DefaultRedirectUrl;
                    else
                        statusInvoceResult.SuccessResult.RedirectUrl =
                            Uri.UnescapeDataString(statusInvoceResult.SuccessResult.RedirectUrl);
                }

                _logger.Information($"Created deposit invoice with id: {request.PciDssInvoiceGrpcModel.OrderId} " +
                    $"kyc: {request.PciDssInvoiceGrpcModel.KycVerification} " +
                    $"redirectUrl: {statusInvoceResult.SuccessResult.RedirectUrl}");
                await SendMessageToAuditLogAsync(request.PciDssInvoiceGrpcModel,
                    $"Created deposit invoice with id: {request.PciDssInvoiceGrpcModel.OrderId} " +
                    $"kyc: {request.PciDssInvoiceGrpcModel.KycVerification} " +
                    $"redirectUrl: {statusInvoceResult.SuccessResult.RedirectUrl}");

                return MakeBridgeDepositGrpcResponse.Create(statusInvoceResult.SuccessResult.RedirectUrl,
                    statusInvoceResult.SuccessResult.PaynetOrderId, DepositBridgeRequestGrpcStatus.Success);
            }
            catch (Exception e)
            {
                _logger.Error(e, "MakeDepositAsync failed for traderId {traderId}",
                    request.PciDssInvoiceGrpcModel.TraderId);
                await SendMessageToAuditLogAsync(request.PciDssInvoiceGrpcModel,
                    $"{PaymentSystemId}. MakeDeposit failed");
                return MakeBridgeDepositGrpcResponse.Failed(DepositBridgeRequestGrpcStatus.ServerError, e.Message);
            }
        }

        private XpateEndpoint GetEndpointId(MakeBridgeDepositGrpcRequest request)
        {
            decimal totalDeposit = Convert.ToDecimal(Math.Round(request.PciDssInvoiceGrpcModel.TotalDeposit, 2,
                MidpointRounding.AwayFromZero));
            decimal totalDepositTreshold = Convert.ToDecimal(_settingsModel.XpateKycVerifiedAmountinUsd);

            if (string.Equals(request.PciDssInvoiceGrpcModel.KycVerification, "Verified"
                    , StringComparison.OrdinalIgnoreCase) &&
                totalDeposit >= totalDepositTreshold )
            {
                return new XpateEndpoint
                {
                    EndpointId = _settingsModel.XpateKycVerifiedEndpointId,
                    PsCurrency = KztCurrency
                };
            }

            return new XpateEndpoint
            {
                EndpointId = _settingsModel.XpateEndpointId,
                PsCurrency = KztCurrency
            }; 
        }

        public ValueTask<GetPaymentSystemGrpcResponse> GetPaymentSystemNameAsync()
        {
            return new ValueTask<GetPaymentSystemGrpcResponse>(GetPaymentSystemGrpcResponse.Create(PaymentSystemId));
        }

        public ValueTask<GetPaymentSystemCurrencyGrpcResponse> GetPsCurrencyAsync()
        {
            return new ValueTask<GetPaymentSystemCurrencyGrpcResponse>(
                GetPaymentSystemCurrencyGrpcResponse.Create(KztCurrency));
        }

        public async ValueTask<GetPaymentSystemAmountGrpcResponse> GetPsAmountAsync(GetPaymentSystemAmountGrpcRequest request)
        {
            if (request.Currency.Equals(UsdCurrency, StringComparison.OrdinalIgnoreCase))
            {

                var convertResponse = await _convertServiceClient.Value.Convert(new CovertRequest
                {
                    InstrumentId = UsdCurrency + KztCurrency,
                    ConvertType = ConvertTypes.BaseToQuote,
                    Amount = request.Amount
                });

                return GetPaymentSystemAmountGrpcResponse.Create(convertResponse.ConvertedAmount, KztCurrency);
            }

            return default;
        }

        private ValueTask SendMessageToAuditLogAsync(IPciDssInvoiceModel invoice, string message)
        {
            return _myCrmAuditLogGrpcService.Value.SaveAsync(new AuditLogEventGrpcModel
            {
                TraderId = invoice.TraderId,
                Action = "deposit",
                ActionId = invoice.OrderId,
                DateTime = DateTime.UtcNow,
                Message = message
            });
        }

        public ValueTask<GetDepositStatusGrpcResponse> GetDepositStatusAsync(GetDepositStatusGrpcRequest request)
        {
            throw new NotImplementedException();
        }

        public ValueTask<DecodeBridgeInfoGrpcResponse> DecodeInfoAsync(DecodeBridgeInfoGrpcRequest request)
        {
            throw new NotImplementedException();
        }

        public ValueTask<MakeConfirmGrpcResponse> MakeDepositConfirmAsync(MakeConfirmGrpcRequest request)
        {
            throw new NotImplementedException();
        }
    }
}