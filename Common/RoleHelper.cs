namespace PropertyPortal.Common;

public static class RoleHelper
{
    public static string ToName(int roleId) => roleId switch
    {
        1 => "admin",
        2 => "agent",
        3 => "private_seller",
        _ => "unknown"
    };
}
