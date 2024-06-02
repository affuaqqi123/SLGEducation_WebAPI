using Newtonsoft.Json;
using System.Net;

namespace WebAPI.Middlewares
{
    public class ErrorLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorLoggingMiddleware> _logger;

        public ErrorLoggingMiddleware(RequestDelegate next, ILogger<ErrorLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);

                int statusCode = (int)HttpStatusCode.InternalServerError;
                var result = JsonConvert.SerializeObject(new
                {
                    StatusCode = statusCode,
                    ErrorMessage = e.Message, //There was some Error Happened. Please contact Dev Team for more details.
                });
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = statusCode;
                context.Response.WriteAsync(result);
            }
        }
    }
}