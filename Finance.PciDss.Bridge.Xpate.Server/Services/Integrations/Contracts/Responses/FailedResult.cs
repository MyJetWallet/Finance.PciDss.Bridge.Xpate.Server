using Newtonsoft.Json;

namespace Finance.PciDss.Bridge.Xpate.Server.Services.Integrations.Contracts.Responses
{
    public class FailedResult
    {
        [JsonProperty("message")] public string Message { get; set; }
        [JsonProperty("statusCode")] public int StatusCode { get; set; }
        [JsonProperty("fieldError")] public object FieldError { get; set; }
    }
}