using System.Text;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;
using Serilog.Sinks.ApplicationInsights.Formatters;

namespace WebApi.Middlewares
{
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LoggingMiddleware> _logger;

        public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            string requestInfo = await LogRequest(context);
            string responseInfo = "";
            var originalResponseBody = context.Response.Body;

            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;
                await _next.Invoke(context);
                responseInfo = await LogResponse(context, responseBody, originalResponseBody);
            }

            var completeRequestInfo = $"{requestInfo} {Environment.NewLine} {responseInfo}";

            // If completeRequestInfo info is heavy, then we can log request and response separately
            _logger.LogInformation(completeRequestInfo);
        }

        private async Task<string> LogResponse(HttpContext context, MemoryStream responseBody, Stream originalResponseBody)
        {
            var responseContent = new StringBuilder();
            responseContent.AppendLine("=== Response Info ===");

            responseContent.AppendLine("-- headers");
            foreach (var (headerKey, headerValue) in context.Response.Headers)
            {
                responseContent.AppendLine($"header = {headerKey}    value = {headerValue}");
            }

            responseContent.AppendLine("-- statusCode");
            responseContent.AppendLine($"statusCode = {context.Response.StatusCode}");

            responseContent.AppendLine("-- body");
            responseBody.Position = 0;
            var content = await new StreamReader(responseBody).ReadToEndAsync();
            responseContent.AppendLine($"body = {content}");
            responseBody.Position = 0;
            await responseBody.CopyToAsync(originalResponseBody);
            context.Response.Body = originalResponseBody;

            return responseContent.ToString();
        }

        private async Task<string> LogRequest(HttpContext context)
        {
            var requestContent = new StringBuilder();

            requestContent.AppendLine("=== Request Info ===");
            requestContent.AppendLine($"method = {context.Request.Method.ToUpper()}");
            requestContent.AppendLine($"path = {context.Request.Path}");

            requestContent.AppendLine("-- headers");
            foreach (var (headerKey, headerValue) in context.Request.Headers)
            {
                requestContent.AppendLine($"header = {headerKey}    value = {headerValue}");
            }

            requestContent.AppendLine("-- body");
            context.Request.EnableBuffering();
            var requestReader = new StreamReader(context.Request.Body);
            var content = await requestReader.ReadToEndAsync();
            requestContent.AppendLine($"body = {content}");

            string requestInfo = requestContent.ToString();
            context.Request.Body.Position = 0;
            return requestInfo;
        }
    }
}