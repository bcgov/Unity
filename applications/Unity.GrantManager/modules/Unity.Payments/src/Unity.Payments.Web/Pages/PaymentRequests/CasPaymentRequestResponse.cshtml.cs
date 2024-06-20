using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;


namespace Unity.Payments.Web.Pages.Payments
{
    public class DisplayCasPaymentRequestResponseModel : AbpPageModel
    {

        [BindProperty]
        public string CasPaymentResponse { get; set; }

        public DisplayCasPaymentRequestResponseModel()
        {
        }

        public async Task OnGetAsync(string casResponse)
        {
            string pattern = ";";
            string replace = "<br>";

            string formattedResponse = Regex.Replace(casResponse,
                            pattern,
                            replace,
                            RegexOptions.None);
            CasPaymentResponse = formattedResponse;
        }
    }
}
