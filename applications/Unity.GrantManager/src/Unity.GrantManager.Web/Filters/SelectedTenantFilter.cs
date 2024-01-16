using Microsoft.AspNetCore.Mvc.Filters;

namespace Unity.GrantManager.Web.Filters
{
    public class SelectedTenantFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            //code here runs before the action method executes
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            //code here runs after the action method executes
        }
    }
}
