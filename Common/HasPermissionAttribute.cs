using Microsoft.AspNetCore.Authorization;

namespace PropertyPortal.Common;

public class HasPermissionAttribute : AuthorizeAttribute
{
    private const string Prefix = "PERM:";
    public HasPermissionAttribute(string permission)
    {
        Policy = Prefix + permission;
    }
}
