using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BachelorRoomFinding.Filters
{
    /// <summary>
    /// Ensures the logged-in Owner has been approved by an Admin before
    /// accessing owner-only actions. Unapproved owners are redirected to
    /// Account/PendingApproval so they see a clear status message.
    /// </summary>
    public class RequireApprovalAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var isApproved = context.HttpContext.Session.GetString("IsApproved");
            if (isApproved != "True")
            {
                context.Result = new RedirectToActionResult("PendingApproval", "Account", null);
            }
        }
    }
}
