using Eventix.Common.Constants.SystemData;
using Eventix.Common.Exceptions;
using Eventix.Share.Common.Models;
using System.Net;
using System.Text.Json;

namespace EventTicketingSystem.Middleware
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);

                // Only handle status codes if response hasn't started
                if (!context.Response.HasStarted)
                {
                    if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
                    {
                        await WriteJsonResponse(context, HttpStatusCode.Unauthorized,
                            new ApiResponseModel<EmptyResponseModel>(SystemError.UNAUTHORIZED, null));
                    }
                    else if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
                    {
                        await WriteJsonResponse(context, HttpStatusCode.Forbidden,
                            new ApiResponseModel<EmptyResponseModel>(SystemError.NOT_PERMISSION, null));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }
        private static async Task WriteJsonResponse<T>(HttpContext context, HttpStatusCode statusCode, ApiResponseModel<T> response)
        {
            if (context.Response.HasStarted) return;

            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Don't try to modify response if it has already started
            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.ContentType = "application/json";

            var response = exception switch
            {
                ApiException apiEx => new
                {
                    StatusCode = (int)apiEx.StatusCode,
                    Response = apiEx.ToApiResponse<object>()
                },
                _ => new
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Response = new ApiResponseModel<object>(
                        "An internal server error occurred",
                        false,
                        default(object))
                }
            };

            context.Response.StatusCode = response.StatusCode;

            var jsonResponse = JsonSerializer.Serialize(response.Response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }

    public static class GlobalExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
        }
    }
}