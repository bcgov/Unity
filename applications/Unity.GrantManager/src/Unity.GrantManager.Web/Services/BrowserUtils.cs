using Microsoft.AspNetCore.Http;

namespace Unity.GrantManager.Web.Services
{
    public class BrowserUtils
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BrowserUtils(IHttpContextAccessor httpContextAccessor)
        {

            _httpContextAccessor = httpContextAccessor;
        }

        public int GetBrowserOffset()
        {
            if (_httpContextAccessor?.HttpContext?.Request != null)
            {
                var offsetValue = "0";
                if (_httpContextAccessor.HttpContext.Request.Cookies.ContainsKey("timezoneoffset"))
                {
                    _ = _httpContextAccessor.HttpContext.Request.Cookies.TryGetValue("timezoneoffset", out offsetValue);
                }

                _ = int.TryParse(offsetValue, out int offset);

                return offset;
            }

            return 0;
        }
    }
}
