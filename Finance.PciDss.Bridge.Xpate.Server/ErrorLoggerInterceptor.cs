using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Serilog;

namespace Finance.PciDss.Bridge.Xpate.Server
{
    public sealed class ErrorLoggerInterceptor : Interceptor
    {
        private readonly ILogger _logger;

        public ErrorLoggerInterceptor(ILogger logger)
        {
            _logger = logger;
        }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
               TRequest request,
               ServerCallContext context,
               UnaryServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                return await continuation(request, context);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                throw new RpcException(Status.DefaultCancelled, ex.Message);
            }
        }
    }
}
