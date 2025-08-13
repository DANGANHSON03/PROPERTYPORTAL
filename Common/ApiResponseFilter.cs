using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;
using System.Reflection;

namespace PropertyPortal.Common;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class SkipApiResponseAttribute : Attribute { }

public sealed class ApiResponseFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext ctx, ResultExecutionDelegate next)
    {
        // Bỏ qua nếu gắn [SkipApiResponse]
        if (ctx.ActionDescriptor?.EndpointMetadata?.Any(m => m is SkipApiResponseAttribute) == true)
        {
            await next();
            return;
        }

        // Không wrap các kết quả đặc biệt
        if (ctx.Result is FileResult || ctx.Result is EmptyResult)
        {
            await next();
            return;
        }

        // 204 -> 200 + ApiResponse<object>.Ok(null)
        if (ctx.Result is NoContentResult)
        {
            ctx.Result = new ObjectResult(ApiResponse<object>.Ok(null)) { StatusCode = 200 };
            await next();
            return;
        }

        if (ctx.Result is ObjectResult obj)
        {
            var status = obj.StatusCode ?? 200;
            var value  = obj.Value;

            // Đã là ApiResponse<> thì thôi
            if (value != null)
            {
                var t = value.GetType();
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ApiResponse<>))
                {
                    await next();
                    return;
                }
            }

            if (status is >= 200 and < 300)
            {
                var payloadType = value?.GetType() ?? typeof(object);
                var respType    = typeof(ApiResponse<>).MakeGenericType(payloadType);
                var okMethod    = respType.GetMethod("Ok", BindingFlags.Public | BindingFlags.Static);

                // data có thể null → truyền null an toàn
                var envelope = okMethod!.Invoke(null, new object?[] { value, "Thành công", null });

                ctx.Result = new ObjectResult(envelope) { StatusCode = status };
                await next();
                return;
            }
            else
            {
                // Non-2xx → Fail
                var (message, errorsObj) = BuildError(status, value);
                var payloadType = value?.GetType() ?? typeof(object);
                var respType    = typeof(ApiResponse<>).MakeGenericType(payloadType);
                var failMethod  = respType.GetMethod("Fail", BindingFlags.Public | BindingFlags.Static);

                var envelope = failMethod!.Invoke(null, new object?[] { message, errorsObj, null });

                ctx.Result = new ObjectResult(envelope) { StatusCode = status };
                await next();
                return;
            }
        }

        if (ctx.Result is StatusCodeResult sc && sc.StatusCode == 204)
        {
            ctx.Result = new ObjectResult(ApiResponse<object>.Ok(null)) { StatusCode = 200 };
            await next();
            return;
        }

        await next();
    }

    private static (string message, object? errors) BuildError(int status, object? value)
    {
        string message = status switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            409 => "Conflict",
            422 => "Unprocessable Entity",
            500 => "Internal Server Error",
            _   => "Error"
        };

        if (value is ValidationProblemDetails vpd) return (message, vpd.Errors);
        if (value is ProblemDetails pd)          return (pd.Title ?? message, pd);

        return (message, value);
    }
}
