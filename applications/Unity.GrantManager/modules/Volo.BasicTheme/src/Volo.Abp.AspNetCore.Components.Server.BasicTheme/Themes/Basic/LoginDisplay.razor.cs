using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System;
using System.Threading.Tasks;
using Volo.Abp.UI.Navigation;

namespace Volo.Abp.AspNetCore.Components.Server.BasicTheme.Themes.Basic;

public partial class LoginDisplay : IDisposable
{
    [Inject]
    protected IMenuManager MenuManager { get; set; }

    protected ApplicationMenu Menu { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Menu = await MenuManager.GetAsync(StandardMenus.User);

        Navigation.LocationChanged += OnLocationChanged;
    }

    protected virtual void OnLocationChanged(object sender, LocationChangedEventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    #region IDisposable implementation
    // To detect redundant calls
    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // Protected implementation of Dispose pattern.
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Navigation.LocationChanged -= OnLocationChanged;
            }
            _disposed = true;
        }
    } 
    #endregion
}
