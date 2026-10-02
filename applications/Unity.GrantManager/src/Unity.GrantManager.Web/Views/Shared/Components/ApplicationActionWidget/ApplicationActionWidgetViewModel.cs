using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicationActionWidget;

public class ApplicationActionWidgetViewModel
{
    public Guid ApplicationId { get; set; }
    public ListResultDto<ApplicationActionDto> ApplicationActions { get; set; } = new();
    public bool IsRedStop { get; set; }
    public DateTime? FinalDecisionDate { get; set; }
    public string? DeclineRational { get; set; }
    public List<SelectListItem> DeclineRationalOptions { get; set; } = new();
}
