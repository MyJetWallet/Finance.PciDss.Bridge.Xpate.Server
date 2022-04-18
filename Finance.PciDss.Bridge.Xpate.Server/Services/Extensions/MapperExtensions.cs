using System.Diagnostics;
using Finance.PciDss.Abstractions;
using Finance.PciDss.Bridge.Xpate.Server.Services.Integrations.Contracts.Requests;
using Finance.PciDss.Bridge.Xpate.Server.Services.Integrations.Contracts.Responses;
using Finance.PciDss.PciDssBridgeGrpc;
using Flurl;

namespace Finance.PciDss.Bridge.Xpate.Server.Services.Extensions
{
    public static class MapperExtensions
    {
        public static SalePaymentInvoiceRequest ToCreatePaymentInvoiceRequest(this IPciDssInvoiceModel model, SettingsModel settingsModel)
        {
            var activityId = Activity.Current?.Id;
            return new SalePaymentInvoiceRequest
            {
                Id = model.OrderId,
                Description = "Platform deposit",
                CardHolder = model.FullName.Length > 35 ? model.FullName[..35].Trim() : model.FullName.Trim(),
                FirstName = model.GetName().Trim(),
                LastName = model.GetLastName().Trim(),
                Address = model.Address.Trim(),
                City = model.City.Trim(),
                Zip = model.Zip.GetClearZipCode(),
                Country = model.Country.Trim(),
                Email = model.Email.Trim(),
                Phone = model.PhoneNumber.Trim(),
                Amount = model.PsAmount,
                Currency = model.PsCurrency,
                CardNumber = model.CardNumber.Trim(),
                Month = model.ExpirationDate.ToString("MM"),
                Year = model.ExpirationDate.ToString("yyyy"),
                Cvv = model.Cvv.Trim(),
                IpAddress = model.Ip,
                
                CallbackUrl = settingsModel.CallbackUrl
                    .SetQueryParam(nameof(activityId), activityId),

                RedirectTo = model.GetRedirectUrlForInvoice(settingsModel.RedirectUrlMapping, settingsModel.DefaultRedirectUrl)
                    .SetQueryParam(nameof(activityId), activityId),
            };
        }

        public static StatusPaymentRequest ToStatusRequest(this SalePaymentInvoiceResponse src, string login)
        {
            var model = new StatusPaymentRequest
            {
                Login = login, 
                ClientOrderId = src.MerchantOrderId,
                PaynetOrderId = src.PaynetOrderId
            };
            return model;
        }
    }
}
