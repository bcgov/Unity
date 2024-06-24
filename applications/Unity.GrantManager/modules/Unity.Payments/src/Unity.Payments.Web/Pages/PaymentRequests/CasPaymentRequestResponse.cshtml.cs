using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using System;


namespace Unity.Payments.Web.Pages.Payments
{
    public class DisplayCasPaymentRequestResponseModel : AbpPageModel
    {

        [BindProperty]
        public string CasPaymentResponse { get; set; } = string.Empty;
        private static int OneMinuteMilliseconds = 60000;

        public ActionResult OnGet(string casResponse)
        {
            string pattern = ";";
            string replace = "<br>";

            string formattedResponse = Regex.Replace(casResponse,
                            pattern,
                            replace,
                            RegexOptions.None,
                            TimeSpan.FromMilliseconds(OneMinuteMilliseconds));
            CasPaymentResponse = formattedResponse;
            return Page();
        }
    }
}
