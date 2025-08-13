using Microsoft.AspNetCore.Authorization;

namespace PropertyPortal.Common;

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public PermissionRequirement(string permission) => Permission = permission;
}
