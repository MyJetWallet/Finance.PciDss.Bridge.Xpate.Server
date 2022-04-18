using Destructurama.Attributed;
using Finance.PciDss.Bridge.Xpate.Server.Services.Extensions;
using Newtonsoft.Json;

namespace Finance.PciDss.Bridge.Xpate.Server.Services.Integrations.Contracts.Requests
{
    public class SalePaymentInvoiceRequest
    {
        [JsonProperty("client_orderid")] public string Id { get; set; }
        
        [JsonProperty("order_desc")] public string Description { get; set; }
        
        [LogMasked(ShowFirst = 1, ShowLast = 1, PreserveLength = true)]
        [JsonProperty("card_printed_name")] public string CardHolder { get; set; }
        
        [LogMasked(ShowFirst = 1, ShowLast = 1, PreserveLength = true)]
        [JsonProperty("first_name")] public string FirstName { get; set; }
        
        [LogMasked(ShowFirst = 1, ShowLast = 1, PreserveLength = true)]
        [JsonProperty("last_name")] public string LastName { get; set; }
        
        [LogMasked(ShowFirst = 1, ShowLast = 1, PreserveLength = true)]
        [JsonProperty("address1")] public string Address { get; set; }
        
        [JsonProperty("city")] public string City { get; set; }
        
        [JsonProperty("zip_code")] public string Zip { get; set; }
        
        [JsonProperty("country")] public string Country { get; set; }
        
        [LogMasked(ShowFirst = 3, ShowLast = 3, PreserveLength = true)]
        [JsonProperty("phone")] public string Phone { get; set; }
        
        [LogMasked(ShowFirst = 3, ShowLast = 3, PreserveLength = true)]
        [JsonProperty("email")] public string Email { get; set; }
        
        [JsonProperty("amount")] public double Amount { get; set; }
        
        [JsonProperty("currency")] public string Currency { get; set; }
        
        [LogMasked(ShowFirst = 6, ShowLast = 4, PreserveLength = true)]
        [JsonProperty("credit_card_number")] public string CardNumber { get; set; }

        [LogMasked(PreserveLength = true)]
        [JsonProperty("expire_month")] public string Month { get; set; }

        [LogMasked(PreserveLength = true)]
        [JsonProperty("expire_year")] public string Year { get; set; }

        [LogMasked(PreserveLength = true)]
        [JsonProperty("cvv2")] public string Cvv { get; set; }
        
        [JsonProperty("ipaddress")] public string IpAddress { get; set; }
        
        [JsonProperty("control")] public string CheckSum { get; set; }
        
        [JsonProperty("server_callback_url")] public string CallbackUrl { get; set; }
        
        [JsonProperty("redirect_url")] public string RedirectTo { get; set; }

        public void InitCheckSum(int endpointId, string merchantControl)
        {
            var amount2digits = string.Format("{0:0}", Amount * 100);
            var str = $"{endpointId}{Id}{amount2digits}{Email}{merchantControl}";
            CheckSum = str.GenerateSha1();
        }
    }
}