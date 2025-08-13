using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace PropertyPortal.Common;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, IWebHostEnvironment env)
    {
        _next = next;
        _env  = env;
    }

    public async Task Invoke(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            ctx.Response.ContentType = "application/json";
            ctx.Response.StatusCode  = (int)HttpStatusCode.InternalServerError;

            // message phải là string, chi tiết lỗi để ở 'errors'
            var message = "Internal Server Error";
            var errors = _env.IsDevelopment()
                ? new
                  {
                      ex.Message,
                      ex.Source,
                      ex.StackTrace
                  }
                : null;

            var payload = ApiResponse<object>.Fail(message, errors);

            await ctx.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
