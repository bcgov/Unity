﻿using System;
using Volo.Abp.Application.Dtos;
namespace Unity.Flex.Scoresheets
{
    [Serializable]
    public class ScoresheetDto : ExtensibleFullAuditedEntityDto<Guid>
    {
        public string Name { get; set; } = string.Empty;
    }
}