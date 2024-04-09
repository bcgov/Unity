using System;
using Unity.Payments.Enums;
using Volo.Abp.Application.Dtos;

namespace Unity.Payments.SupplierInfo;

[Serializable]
public class GetSitesRequestDto
{
    public Guid ApplicantId { get; set; }
    public string? SupplierNumber { get; set; }    
}
