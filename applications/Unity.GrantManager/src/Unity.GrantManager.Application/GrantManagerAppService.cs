using System;
using System.Collections.Generic;
using System.Text;
using Unity.GrantManager.Localization;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager;

/* Inherit your application services from this class.
 */
public abstract class GrantManagerAppService : ApplicationService
{
    protected GrantManagerAppService()
    {
        LocalizationResource = typeof(GrantManagerResource);
    }
}
