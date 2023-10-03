using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.Demo.Pages.Components;

public class DropdownsModel : PageModel
{
    [Required]
    [DataType(DataType.EmailAddress)]
    [Display(Name = "Email Address")]
    public string EmailAddress { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; }

    [Display(Name = "Remember Me")]
    public bool RememberMe { get; set; }


    public void OnGet()
    {
        //Placeholder. Nothing to do here yet.
    }
}
