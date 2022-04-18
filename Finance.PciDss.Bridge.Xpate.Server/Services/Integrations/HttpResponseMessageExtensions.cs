using System;
using System.Net.Http;
using System.Threading.Tasks;
using Finance.PciDss.Bridge.Xpate.Server.Services.Extensions;
using Newtonsoft.Json;
using Serilog;

namespace Finance.PciDss.Bridge.Xpate.Server.Services.Integrations
{
    public static class HttpResponseMessageExtensions
    {
        public static async Task<Response<TSuccessResponse, TFailedResponse>> DeserializeTo<TSuccessResponse,
            TFailedResponse>(this HttpResponseMessage httpResponseMessage)
            where TSuccessResponse : class
            where TFailedResponse : class
        {
            string resultData = await httpResponseMessage.Content.ReadAsStringAsync();
            Log.Logger.Information("Xpate return response : {resultData}", resultData);
            try
            {
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    var response = JsonConvert.DeserializeObject<TSuccessResponse>(resultData.GenerateJsonStringFromKeyValue());
                    return Response<TSuccessResponse, TFailedResponse>.CreateSuccess(response);
                }
                else
                {
                    var response = JsonConvert.DeserializeObject<TFailedResponse>(resultData.GenerateJsonStringFromKeyValue());
                    return Response<TSuccessResponse, TFailedResponse>.CreateFailed(response);
                }
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "DeserializeTo failed. Response : {resultData}", resultData);
                throw;
            }
        }
    }
}