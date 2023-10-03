using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.JSInterop;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Components.Web.Security;
using Volo.Abp.UI.Navigation;

namespace Volo.Abp.AspNetCore.Components.WebAssembly.BasicTheme.Themes.Basic;

public partial class LoginDisplay
{
    [Inject]
    protected IMenuManager MenuManager { get; set; }

    [Inject]
    protected ApplicationConfigurationChangedService ApplicationConfigurationChangedService { get; set; }

    protected ApplicationMenu Menu { get; set; }

    protected async override Task OnInitializedAsync()
    {
        Menu = await MenuManager.GetAsync(StandardMenus.User);

        Navigation.LocationChanged += OnLocationChanged;

        ApplicationConfigurationChangedService.Changed += ApplicationConfigurationChanged;
    }

    protected virtual void OnLocationChanged(object sender, LocationChangedEventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    private async void ApplicationConfigurationChanged()
    {
        Menu = await MenuManager.GetAsync(StandardMenus.User);
        await InvokeAsync(StateHasChanged);
    }

    private async Task NavigateToAsync(string uri, string target = null)
    {
        if (target == "_blank")
        {
            await JsRuntime.InvokeVoidAsync("open", uri, target);
        }
        else
        {
            Navigation.NavigateTo(uri);
        }
    }

    private void BeginSignOut()
    {
        Navigation.NavigateToLogout("authentication/logout");
    }

    #region Disposable base class implementation
    // To detect redundant calls
    private bool _disposed;

    // Protected implementation of Dispose pattern.
    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Navigation.LocationChanged -= OnLocationChanged;
                ApplicationConfigurationChangedService.Changed -= ApplicationConfigurationChanged;
            }
            _disposed = true;
        }
        base.Dispose(disposing);
    }
    #endregion
}
