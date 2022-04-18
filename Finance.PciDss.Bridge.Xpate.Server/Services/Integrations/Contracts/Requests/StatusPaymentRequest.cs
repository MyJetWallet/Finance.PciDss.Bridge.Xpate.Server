using Finance.PciDss.Bridge.Xpate.Server.Services.Extensions;
using Newtonsoft.Json;

namespace Finance.PciDss.Bridge.Xpate.Server.Services.Integrations.Contracts.Requests
{
    public class StatusPaymentRequest
    {
        [JsonProperty("login")] public string Login { get; set; }
        [JsonProperty("client_orderid")] public string ClientOrderId { get; set; }
        [JsonProperty("orderid")] public string PaynetOrderId  { get; set; }
        [JsonProperty("control")] public string ControlSum { get; set; }

        public void InitCheckSum(string controlString)
        {
            var line = $"{Login}{ClientOrderId}{PaynetOrderId}{controlString}";
            ControlSum = line.GenerateSha1();
        }
    }
}