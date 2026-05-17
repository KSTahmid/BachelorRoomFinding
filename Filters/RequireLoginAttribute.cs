using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BachelorRoomFinding.Filters
{
    public class RequireLoginAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = context.HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                var returnUrl = context.HttpContext.Request.Path;
                context.Result = new RedirectToActionResult("Login", "Account",
                    new { returnUrl });
            }
        }
    }
}
