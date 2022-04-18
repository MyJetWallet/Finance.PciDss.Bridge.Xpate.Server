using System.Threading.Tasks;
using Finance.PciDss.Bridge.Xpate.Server.Services.Integrations.Contracts.Requests;
using Finance.PciDss.Bridge.Xpate.Server.Services.Integrations.Contracts.Responses;

namespace Finance.PciDss.Bridge.Xpate.Server.Services.Integrations
{
    public interface IXpateHttpClient
    {
        /// <summary>
        /// In order to initiate a Sale transaction Merchant sends an HTTPS POST request with the parameters specified
        /// in Sale Request Parameters Table below
        /// https://doc.sg.xpate.com/card_payment_API/sale-transactions.html#sale-transaction-request-url
        /// </summary>
        /// <param name="request"></param>
        /// <param name="baseUrl"></param>
        /// <param name="endpointId"></param>
        /// <returns></returns>
        Task<Response<SalePaymentInvoiceResponse, SalePaymentInvoiceFailResponseDataPayment>> RegisterInvoiceAsync(
            SalePaymentInvoiceRequest request, string baseUrl, string endpointId);

        /// <summary>
        /// In most common cases, the best option is to include both client_orderid and orderid parameters to status request.
        /// Order status can be requested with only client_orderid if it’s unique to merchant and orderid is not received.
        /// If orderid is not received in response, but this response contains an error, see the received error message
        /// to get the information why transaction was not created in the system.
        /// https://doc.sg.xpate.com/card_payment_API/sale-transactions.html#status-api-url
        /// </summary>
        /// <param name="request"></param>
        /// <param name="baseUrl"></param>
        /// <param name="endpointId"></param>
        /// <returns></returns>
        Task<Response<StatusPaymentResponse, StatusPaymentFailResponseDataPayment>> GetStatusInvoiceAsync(
            StatusPaymentRequest request, string baseUrl, string endpointId);

    }
}