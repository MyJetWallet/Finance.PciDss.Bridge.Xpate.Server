using System;
using System.Transactions;
using Newtonsoft.Json;

namespace Finance.PciDss.Bridge.Xpate.Server.Services.Integrations.Contracts.Responses
{
    public class StatusPaymentResponse
    {
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("status")] public string Status { get; set; }
        [JsonProperty("paynet-order-id")] public string PaynetOrderId { get; set; }
        [JsonProperty("merchant-order-id")] public string MerchantOrderId { get; set; }
        [JsonProperty("error-code")] public string ErrorCode { get; set; }
        [JsonProperty("error-message")] public string ErrorMessage { get; set; }
        [JsonProperty("redirect-to")] public string RedirectUrl { get; set; }
        //[JsonProperty("html")] public string Html { get; set; }

        public bool IsFailed()
        {
            return Status.Equals("declined", StringComparison.OrdinalIgnoreCase) ||
                   Status.Equals("error", StringComparison.OrdinalIgnoreCase) ||
                   Status.Equals("filtered", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsSuccessWithoutRedirectTo3ds()
        {
            return !IsFailed() && Status.Equals("approved", StringComparison.OrdinalIgnoreCase)
                               && string.IsNullOrEmpty(RedirectUrl);
        }

        public bool ShouldBeRedirectTo3ds()
        {
            return !IsFailed()
                   && Status.Contains("processing", StringComparison.OrdinalIgnoreCase) && 
                   !string.IsNullOrEmpty(RedirectUrl);
        }
    }
}