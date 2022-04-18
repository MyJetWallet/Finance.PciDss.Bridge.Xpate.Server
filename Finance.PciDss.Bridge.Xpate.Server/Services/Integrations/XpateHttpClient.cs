using System.Threading.Tasks;
using Finance.PciDss.Bridge.Xpate.Server.Services.Extensions;
using Finance.PciDss.Bridge.Xpate.Server.Services.Integrations.Contracts.Requests;
using Finance.PciDss.Bridge.Xpate.Server.Services.Integrations.Contracts.Responses;
using Flurl;
using Flurl.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Finance.PciDss.Bridge.Xpate.Server.Services.Integrations
{
    public class XpateHttpClient : IXpateHttpClient
    {
        public async Task<Response<SalePaymentInvoiceResponse, SalePaymentInvoiceFailResponseDataPayment>> 
            RegisterInvoiceAsync(SalePaymentInvoiceRequest request, string baseUrl, string endpointId)
        {
            var serializedRequest = JObject.Parse(JsonConvert.SerializeObject(request)).GenerateKeyValue();
            var result = await baseUrl
                .AppendPathSegments("sale", endpointId)
                .PostUrlEncodedAsync(serializedRequest);
            return await result.DeserializeTo<SalePaymentInvoiceResponse, SalePaymentInvoiceFailResponseDataPayment>();
        }

        public async Task<Response<StatusPaymentResponse, StatusPaymentFailResponseDataPayment>> 
            GetStatusInvoiceAsync(StatusPaymentRequest request, string baseUrl, string endpointId)
        {
            var result = await baseUrl
                .AppendPathSegments("status", endpointId)
                .PostUrlEncodedAsync(JObject.Parse(JsonConvert.SerializeObject(request)).GenerateKeyValue());

            return await result.DeserializeTo<StatusPaymentResponse, StatusPaymentFailResponseDataPayment>();
        }
    }
}