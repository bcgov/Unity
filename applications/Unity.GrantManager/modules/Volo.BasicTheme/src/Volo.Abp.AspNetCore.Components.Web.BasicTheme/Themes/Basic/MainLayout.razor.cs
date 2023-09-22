using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System;

namespace Volo.Abp.AspNetCore.Components.Web.BasicTheme.Themes.Basic;

public partial class MainLayout : IDisposable
{
    [Inject] private NavigationManager NavigationManager { get; set; }

    private bool IsCollapseShown { get; set; }

    protected override void OnInitialized()
    {
        NavigationManager.LocationChanged += OnLocationChanged;
    }

    private void ToggleCollapse()
    {
        IsCollapseShown = !IsCollapseShown;
    }

    private void OnLocationChanged(object sender, LocationChangedEventArgs e)
    {
        IsCollapseShown = false;
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
