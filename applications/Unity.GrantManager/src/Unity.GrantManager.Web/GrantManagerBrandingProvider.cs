﻿using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Web;

[Dependency(ReplaceServices = true)]
public class GrantManagerBrandingProvider : DefaultBrandingProvider
{
    public override string AppName => "Unity Grant Manager";
    public override string LogoUrl => "/images/logo/bcgov/BCID_H_rgb_pos.png";
    public override string LogoReverseUrl => "/images/logo/bcgov/BCID_H_rgb_rev.png";
}
