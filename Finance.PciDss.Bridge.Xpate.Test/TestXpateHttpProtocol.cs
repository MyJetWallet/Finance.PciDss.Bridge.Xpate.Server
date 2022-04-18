using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Finance.PciDss.Abstractions;
using Finance.PciDss.Bridge.Xpate.Server;
using Finance.PciDss.Bridge.Xpate.Server.Services;
using Finance.PciDss.Bridge.Xpate.Server.Services.Extensions;
using Finance.PciDss.Bridge.Xpate.Server.Services.Integrations;
using Finance.PciDss.Bridge.Xpate.Server.Services.Integrations.Contracts.Responses;
using Finance.PciDss.PciDssBridgeGrpc.Contracts;
using Finance.PciDss.PciDssBridgeGrpc.Contracts.Enums;
using Finance.PciDss.PciDssBridgeGrpc.Models;
using Flurl;
using Newtonsoft.Json;
using NUnit.Framework;
using SimpleTrading.Common.Helpers;

namespace Finance.PciDss.Bridge.Xpate.Test
{
    public class Tests
    {
        private Activity _unitTestActivity;
        private SettingsModel _settingsModel;
        private XpateHttpClient _xpateHttpClient;
        private MakeBridgeDepositGrpcRequest _request;

        public void Dispose()
        {
            _unitTestActivity.Stop();
        }

        [OneTimeSetUp]
        public void StartTest()
        {
            Trace.Listeners.Add(new ConsoleTraceListener());
        }

        [OneTimeTearDown]
        public void EndTest()
        {
            Trace.Flush();
        }


        [SetUp]
        public void Setup()
        {
            _xpateHttpClient = new XpateHttpClient();
            _unitTestActivity = new Activity("UnitTest").Start();
            
            _settingsModel = new SettingsModel()
            {
                SeqServiceUrl = "http://192.168.1.80:5341",
                XpatePciDssBaseUrl = "https://sandbox.sg.xpate.com/paynet/api/v2",
                XpateMerchantControl = "CDA5F9F4-7D58-4CCE-AD23-11B93203CDCC",
                XpateEndpointId = "8938",
                XpateLogin = "Commerzgroup_International",
                DefaultRedirectUrl = "https://webhook.site/30de1976-0467-4ed0-8c3a-6125857f4dfc/?yuriy=default",
                CallbackUrl = "https://webhook.site/30de1976-0467-4ed0-8c3a-6125857f4dfc/?yuriy=callbak",
                RedirectUrlMapping = "Monfex@st@https://webhook.site/30de1976-0467-4ed0-8c3a-6125857f4dfc/?yuriy=redirect",
                AuditLogGrpcServiceUrl = "http://10.240.0.122:80",
                ConvertServiceGrpcUrl = "http://10.240.1.9:8080",
                XpateKycVerifiedEndpointId = "8938"
            };

            _request = MakeBridgeDepositGrpcRequest.Create(new PciDssInvoiceGrpcModel
            {
                CardNumber = "4111111111111111",
                FullName = "TEST TEST",
                Amount = 10,
                Zip = "test",
                City = "Madrid",
                Country = "ESP",
                Address = "test",
                OrderId = "QraDMvwquEaTYraMemOQ",
                Email = "testuserxpate1@mailinator.com",
                TraderId = "c300b7426e80431aa4300a793f020d19",
                AccountId = "stl00002349usd",
                PaymentProvider = "pciDssXpateBankCards",
                Currency = "USD",
                Ip = "213.141.131.96",
                PsAmount = 8.21,
                PsCurrency = "EUR",
                Brand = BrandName.Monfex,
                BrandName = "Monfex",
                PhoneNumber = "+380633985848",
                KycVerification = null,
                Cvv = "023",
                ExpirationDate = DateTime.Parse("2024-12")
            });
        }

        [Test]
        public async Task Send_Xpate_SaleRequest_And_Check_Status()
        {
            MakeBridgeDepositGrpcResponse returnResult;
            Trace.WriteLine("This is Trace.WriteLine");

            var endpointId = string.Equals(_request.PciDssInvoiceGrpcModel.KycVerification, "Verified"
                , StringComparison.OrdinalIgnoreCase) ?
                _settingsModel.XpateKycVerifiedEndpointId : _settingsModel.XpateEndpointId;

            //Modify request data
            _request.PciDssInvoiceGrpcModel.KycVerification = string.IsNullOrEmpty(_request.PciDssInvoiceGrpcModel.KycVerification) ?
                "Empty" : _request.PciDssInvoiceGrpcModel.KycVerification;
            _request.PciDssInvoiceGrpcModel.Country = CountryManager.Iso3ToIso2(_request.PciDssInvoiceGrpcModel.Country);
            //Preapare invoice
            var createInvoiceRequest = _request.PciDssInvoiceGrpcModel.ToCreatePaymentInvoiceRequest(_settingsModel);
            createInvoiceRequest.InitCheckSum(int.Parse(endpointId),
                _settingsModel.XpateMerchantControl);

            var createInvoiceResult =
                await _xpateHttpClient.RegisterInvoiceAsync(createInvoiceRequest, _settingsModel.XpatePciDssBaseUrl, endpointId);

                if (createInvoiceResult.IsFailed)
            {
                //await SendMessageToAuditLogAsync(request.PciDssInvoiceGrpcModel,
                //    $"{PaymentSystemId}. Fail Xpate create invoice with kyc: {kycLogInfo}. Message: {createInvoiceResult.FailedResult.Message}. " +
                //    $"Error: {JsonConvert.SerializeObject(createInvoiceResult.FailedResult.FieldError)}");
                returnResult = MakeBridgeDepositGrpcResponse.Failed(DepositBridgeRequestGrpcStatus.ServerError,
                    createInvoiceResult.FailedResult.Message);
                Assert.IsNotNull(returnResult);
                return;
            }

            if (!string.IsNullOrEmpty(createInvoiceResult.SuccessResult.ErrorCode))
            {
                //await SendMessageToAuditLogAsync(request.PciDssInvoiceGrpcModel,
                //    $"{PaymentSystemId}. Fail Xpate create invoice with kyc: {kycLogInfo}. Message: {createInvoiceResult.SuccessResult.ErrorMessage}. " +
                //    $"Error: {JsonConvert.SerializeObject(createInvoiceResult.SuccessResult)}");
                returnResult = MakeBridgeDepositGrpcResponse.Failed(DepositBridgeRequestGrpcStatus.ServerError,
                    createInvoiceResult.SuccessResult.ErrorMessage);
                Assert.IsNotNull(returnResult);
                return;
            }

            //Preapare status
            var statusInvoiceRequest = createInvoiceResult.SuccessResult.ToStatusRequest(_settingsModel.XpateLogin);
            statusInvoiceRequest.InitCheckSum(_settingsModel.XpateMerchantControl);

            Response<StatusPaymentResponse, StatusPaymentFailResponseDataPayment> statusInvoceResult = null;
            for (int i=0; i < 10; i++)
            {
                statusInvoceResult =
                    await _xpateHttpClient.GetStatusInvoiceAsync(statusInvoiceRequest, _settingsModel.XpatePciDssBaseUrl, endpointId);

                if (statusInvoceResult.IsFailed || statusInvoceResult.SuccessResult is null || statusInvoceResult.SuccessResult.IsFailed())
                {
                    //_logger.Information("Fail Xpate status invoice. {@kyc} {@response}", kycLogInfo, statusInvoceResult);
                    //await SendMessageToAuditLogAsync(request.PciDssInvoiceGrpcModel,
                    //    $"Fail Xpate status invoice. Error {statusInvoceResult.FailedResult}");
                    returnResult = MakeBridgeDepositGrpcResponse.Failed(DepositBridgeRequestGrpcStatus.ServerError,
                        statusInvoceResult.FailedResult.Message);
                    Assert.IsNotNull(returnResult);
                    return;
                }

                if (!string.IsNullOrEmpty(statusInvoceResult.SuccessResult.RedirectUrl))
                {
                    break;
                }
                await Task.Delay(300);
            }
            
            if (statusInvoceResult.SuccessResult.IsSuccessWithoutRedirectTo3ds())
            {
                statusInvoceResult.SuccessResult.RedirectUrl = _settingsModel.DefaultRedirectUrl
                    .SetQueryParam("orderId", _request.PciDssInvoiceGrpcModel.OrderId);
                //_logger.Information("Xpate is success without redirect to 3ds. RedirectUrl {url} was built for traderId {traderId} and orderid {orderid} {kyc}",
                //    statusInvoceResult.SuccessResult.RedirectUrl,
                //    request.PciDssInvoiceGrpcModel.TraderId,
                //    request.PciDssInvoiceGrpcModel.OrderId,
                //    kycLogInfo);

                //await SendMessageToAuditLogAsync(request.PciDssInvoiceGrpcModel,
                //    $"Xpate was successful without redirect to 3ds. Orderid: {request.PciDssInvoiceGrpcModel.OrderId}");
            }
            else
            {
                if (string.IsNullOrEmpty(statusInvoceResult.SuccessResult.RedirectUrl))
                    statusInvoceResult.SuccessResult.RedirectUrl = _settingsModel.DefaultRedirectUrl;
                else
                    statusInvoceResult.SuccessResult.RedirectUrl =
                        Uri.UnescapeDataString(statusInvoceResult.SuccessResult.RedirectUrl);

            }

            //await SendMessageToAuditLogAsync(request.PciDssInvoiceGrpcModel,
            //    $"Created deposit invoice with id {request.PciDssInvoiceGrpcModel.OrderId} kyc: {kycLogInfo}");

            returnResult = MakeBridgeDepositGrpcResponse.Create(statusInvoceResult.SuccessResult.RedirectUrl,
                statusInvoceResult.SuccessResult.PaynetOrderId, DepositBridgeRequestGrpcStatus.Success);
            Assert.IsNotNull(returnResult);
            return;
        }
    }
}
