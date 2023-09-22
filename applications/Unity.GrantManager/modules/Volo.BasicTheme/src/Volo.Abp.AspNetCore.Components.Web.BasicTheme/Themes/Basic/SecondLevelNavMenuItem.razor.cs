using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System;
using Volo.Abp.UI.Navigation;

namespace Volo.Abp.AspNetCore.Components.Web.BasicTheme.Themes.Basic;

public partial class SecondLevelNavMenuItem : IDisposable
{
    [Inject] private NavigationManager NavigationManager { get; set; }

    [Parameter]
    public ApplicationMenuItem MenuItem { get; set; }

    public bool IsSubMenuOpen { get; set; }

    protected override void OnInitialized()
    {
        NavigationManager.LocationChanged += OnLocationChanged;
    }

    private void ToggleSubMenu()
    {
        IsSubMenuOpen = !IsSubMenuOpen;
    }

    private void OnLocationChanged(object sender, LocationChangedEventArgs e)
    {
        IsSubMenuOpen = false;
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
                NavigationManager.LocationChanged -= OnLocationChanged;
            }
            _disposed = true;
        }
    }
    #endregion
}
