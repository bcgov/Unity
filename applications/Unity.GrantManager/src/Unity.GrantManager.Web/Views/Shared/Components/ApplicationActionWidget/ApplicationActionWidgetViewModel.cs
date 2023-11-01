using System;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationActionWidget;

public class ApplicationActionWidgetViewModel
{
    public Guid ApplicationId { get; set; }
    public ListResultDto<ApplicationActionDto> ApplicationActions { get; set; } = new();
}
