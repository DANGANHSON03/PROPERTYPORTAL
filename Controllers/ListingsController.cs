using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertyPortal.Common;

namespace PropertyPortal.Controllers;

[ApiController]
[Route("api/listings")] // cố định route để tránh nhầm tên controller
public class ListingsController : ControllerBase
{
    // Yêu cầu có token + quyền listing.create
    [HttpPost]
    [Authorize]
    [HasPermission("listing.create")]
    public IActionResult Create()
    {
        // Trả thẳng; ApiResponseFilter của bạn sẽ bọc lại nếu cần
        return Ok(ApiResponse<object>.Ok(new { created = true }, "Bạn có quyền 'listing.create'"));
    }

    // Ví dụ endpoint khác cần quyền approve
    [HttpPost("{id:long}/approve")]
    [Authorize]
    [HasPermission("listing.approve")]
    public IActionResult Approve(long id)
    {
        return Ok(ApiResponse<object>.Ok(new { id, approved = true }, "Duyệt thành công"));
    }
}
