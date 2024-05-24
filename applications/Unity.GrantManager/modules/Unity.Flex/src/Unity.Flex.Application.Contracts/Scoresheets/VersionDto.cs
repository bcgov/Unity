using System;
using System.Collections.Generic;
using System.Text;

namespace Unity.Flex.Scoresheets;

public class VersionDto
{
    public Guid ScoresheetId { get; set; }
    public uint Version {  get; set; }
}
