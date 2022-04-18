using Newtonsoft.Json;

namespace Finance.PciDss.Bridge.Xpate.Server.Services.Integrations.Contracts.Responses
{
    public class SalePaymentInvoiceResponse
    {
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("paynet-order-id")] public string PaynetOrderId { get; set; }
        [JsonProperty("merchant-order-id")] public string MerchantOrderId { get; set; }
        [JsonProperty("serial-number")] public string SerialNumber { get; set; }
        [JsonProperty("end-point-id")] public string EndPointId { get; set; }
        [JsonProperty("error-message")] public string ErrorMessage { get; set; }
        [JsonProperty("error-code")] public string ErrorCode { get; set; }
    }
}