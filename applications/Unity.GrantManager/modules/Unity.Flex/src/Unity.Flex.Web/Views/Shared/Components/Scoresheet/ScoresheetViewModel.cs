using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Unity.Flex.Scoresheets;

namespace Unity.Flex.Web.Views.Shared.Components.Scoresheet;

public class ScoresheetViewModel
{
    public List<ScoresheetDto> Scoresheets { get; set; } = [];
}
