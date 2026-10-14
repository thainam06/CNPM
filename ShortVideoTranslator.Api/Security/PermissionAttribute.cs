using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ShortVideoTranslator.Business;

namespace ShortVideoTranslator.Api.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class PermissionAttribute(string permission) : Attribute, IAsyncAuthorizationFilter
{
    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var id = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id == null) context.Result = new UnauthorizedObjectResult(new { message = "Vui lòng đăng nhập." });
        else if (!context.HttpContext.RequestServices.GetRequiredService<PlatformService>().HasPermission(id, permission))
            context.Result = new ObjectResult(new { message = "Bạn không có quyền thực hiện thao tác này hoặc cần đổi mật khẩu ban đầu." }) { StatusCode = 403 };
        return Task.CompletedTask;
    }
}
