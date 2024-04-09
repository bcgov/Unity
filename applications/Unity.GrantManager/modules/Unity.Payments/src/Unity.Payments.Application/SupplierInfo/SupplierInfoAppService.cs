using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Payments.Enums;
using Unity.Payments.Settings;
using Unity.Payments.Suppliers;
using Volo.Abp.Features;
using Volo.Abp.Users;

using Microsoft.AspNetCore.Authorization.Infrastructure;

using System.Collections;

using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;

using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Local;
using Volo.Abp;

namespace Unity.Payments.SupplierInfo
{
    public class SupplierInfoAppService : PaymentsAppService, ISupplierInfoAppService
    {
        private readonly ISupplierRepository _supplierRepository;

        public SupplierInfoAppService(ISupplierRepository supplierRepository)
        {
            _supplierRepository = supplierRepository;
        }

        public async Task<Supplier?> GetSupplierAsync(Guid applicantId)
        {
            var query = from supplier in await _supplierRepository.GetQueryableAsync()
                        where supplier.CorrelationId == applicantId
                        select supplier;
            var queryResult = await AsyncExecuter.FirstOrDefaultAsync(query);
            return queryResult;
        }

        public async Task<List<SiteDto>> GetSitesAsync(GetSitesRequestDto requestDto)
        {
            var query = from supplier in await _supplierRepository.GetQueryableAsync()
                        where supplier.Number.ToString() == requestDto.SupplierNumber && supplier.CorrelationId == requestDto.ApplicantId
                        select supplier.Sites.ToList();
            var queryResult = await AsyncExecuter.FirstOrDefaultAsync(query);
            if (queryResult != null)
            {
                return ObjectMapper.Map<List<Site>, List<SiteDto>>(queryResult);
            }
            else
            {
                return new List<SiteDto>();
            }
            
            
        }

        public async Task InsertSiteAsync(Guid applicantId, string supplierNumber, string siteNumber, int payGroup, string? addressLine1, string? addressLine2, string? addressLine3, string? city, string? province, string? postalCode)
        {
            var query = from supplier in await _supplierRepository.GetQueryableAsync()
                        where supplier.Number.ToString() == supplierNumber && supplier.CorrelationId == applicantId
                        select supplier;
            var queryResult = await AsyncExecuter.FirstOrDefaultAsync(query);
            if(queryResult != null)
            {
                queryResult.Sites.Add(new Site()
                {
                    SupplierId = queryResult.Id,
                    Number = uint.Parse(siteNumber),
                    AddressLine1 = addressLine1,
                    AddressLine2 = addressLine2,
                    AddressLine3 = addressLine3,
                    City = city,
                    Province = province,
                    PostalCode = postalCode
                });
                await _supplierRepository.UpdateAsync(queryResult);
            }
            else
            {
                throw new UserFriendlyException("Invalid Supplier Number!");
            }
            

        }

        public async Task InsertSupplierAsync(string? supplierNumber, Guid applicantId)
        {
            if (string.IsNullOrEmpty(supplierNumber) || applicantId == Guid.Empty)
            {
                return;
            }
            string correlationProvider = "Applicant"; //TODO check if this is correct
            var query = from supplier in await _supplierRepository.GetQueryableAsync()
                        where supplier.CorrelationId == applicantId && supplier.CorrelationProvider == correlationProvider
                        select supplier;
            var queryResult = await AsyncExecuter.FirstOrDefaultAsync(query);
            if (queryResult == null)
            {
                await _supplierRepository.InsertAsync(new Supplier() 
                { 
                    Name = "", //TODO check where to get name
                    Number = uint.Parse(supplierNumber),
                    CorrelationId = applicantId,
                    CorrelationProvider = correlationProvider
                });
            }
            else
            {
                queryResult.Number = uint.Parse(supplierNumber);
                await _supplierRepository.UpdateAsync(queryResult);
            }
            
        }
    }
}
