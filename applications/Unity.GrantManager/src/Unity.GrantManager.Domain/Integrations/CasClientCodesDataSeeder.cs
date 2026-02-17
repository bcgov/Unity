using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Integrations
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(CasClientCodesDataSeeder), typeof(IDataSeedContributor))]
    public class CasClientCodesDataSeeder(ICasClientCodeRepository CasClientCodeRepository) : IDataSeedContributor, ITransientDependency
    {
        public async Task SeedAsync(DataSeedContext context)
        {
            await SeedCasClientCodesAsync();
        }

        private async Task SeedCasClientCodesAsync()
        {
#pragma warning disable S1192 // Use 'new(...)
                var clientCodes = new List<CasClientCode>
                {
                    new() { ClientCode = "002", Description = "Legislative Assembly", FinancialMinistry = "", IsActive = true },
                    new() { ClientCode = "003", Description = "Auditor General", FinancialMinistry = "Office of the Auditor General ", IsActive = true },
                    new() { ClientCode = "004", Description = "Office of the Premier", FinancialMinistry = "Office of the Premier", IsActive = true },
                    new() { ClientCode = "005", Description = "Conflict of Interest Commissioner", FinancialMinistry = "", IsActive = true },
                    new() { ClientCode = "007", Description = "Ombudsperson", FinancialMinistry = "Office of the Ombudsperson", IsActive = true },
                    new() { ClientCode = "009", Description = "Info and Privacy Commissioner", FinancialMinistry = "Information and Privacy Commission", IsActive = true },
                    new() { ClientCode = "010", Description = "Public Safety and Solicitor General", FinancialMinistry = "Solicitor General", IsActive = true },
                    new() { ClientCode = "015", Description = "Elections BC", FinancialMinistry = "", IsActive = true },
                    new() { ClientCode = "019", Description = "Post-Secondary Education and Future Skills", FinancialMinistry = "Post Secondary Education and Future Skills", IsActive = true },
                    new() { ClientCode = "022", Description = "Finance", FinancialMinistry = "Finance", IsActive = true },
                    new() { ClientCode = "025", Description = "Police Complaint Commissioner", FinancialMinistry = "Police Complaint Commissioner Office", IsActive = true },
                    new() { ClientCode = "026", Description = "Health", FinancialMinistry = "Health", IsActive = true },
                    new() { ClientCode = "027", Description = "Mental Health and Addictions", FinancialMinistry = "Mental Health and Addictions", IsActive = true },
                    new() { ClientCode = "029", Description = "Vital Statistics CHIPS", FinancialMinistry = "", IsActive = true },
                    new() { ClientCode = "031", Description = "Social Development and Poverty Reduction", FinancialMinistry = "Social Development and Poverty Reduction", IsActive = true },
                    new() { ClientCode = "034", Description = "Transportation and Transit", FinancialMinistry = "Transportation and Transit", IsActive = true },
                    new() { ClientCode = "039", Description = "Children and Family Development", FinancialMinistry = "Children and Family Development", IsActive = true },
                    new() { ClientCode = "046", Description = "Liquor Distribution Branch", FinancialMinistry = "", IsActive = true },
                    new() { ClientCode = "048", Description = "Environment and Parks", FinancialMinistry = "Environment and Parks", IsActive = true },
                    new() { ClientCode = "050", Description = "Forests and Lands", FinancialMinistry = "Forests", IsActive = true },
                    new() { ClientCode = "055", Description = "British Columbia Utilities Commission", FinancialMinistry = "", IsActive = true },
                    new() { ClientCode = "057", Description = "Energy and Climate Solutions", FinancialMinistry = "Energy and Climate Solutions", IsActive = true },
                    new() { ClientCode = "058", Description = "Natural Gas Development", FinancialMinistry = "Energy and Climate Solutions", IsActive = true },
                    new() { ClientCode = "060", Description = "Municipal Affairs", FinancialMinistry = "Housing and Municipal Affairs", IsActive = true },
                    new() { ClientCode = "062", Description = "Education and Child Care", FinancialMinistry = "Education and Child Care", IsActive = true },
                    new() { ClientCode = "063", Description = "Management of Public Funds and Debt", FinancialMinistry = "Finance", IsActive = true },
                    new() { ClientCode = "067", Description = "Product Sales and Services BC", FinancialMinistry = "Citizens Services", IsActive = true },
                    new() { ClientCode = "068", Description = "Public Sector Employers Council", FinancialMinistry = "Finance", IsActive = true },
                    new() { ClientCode = "074", Description = "Government Agents", FinancialMinistry = "Citizens Services", IsActive = true },
                    new() { ClientCode = "079", Description = "Forest Practices Board", FinancialMinistry = "Forest Practices Board ", IsActive = true },
                    new() { ClientCode = "080", Description = "Env Appeal Board and Forest Appeals Commission", FinancialMinistry = "Attorney General", IsActive = true },
                    new() { ClientCode = "085", Description = "Tax Transfers", FinancialMinistry = "Finance", IsActive = true },
                    new() { ClientCode = "087", Description = "Contingencies", FinancialMinistry = "Finance", IsActive = true },
                    new() { ClientCode = "089", Description = "Provincial Treasury", FinancialMinistry = "Finance", IsActive = true },
                    new() { ClientCode = "090", Description = "Pymt Diversion Legal Encumbrance", FinancialMinistry = "Finance", IsActive = true },
                    new() { ClientCode = "099", Description = "BCGOV", FinancialMinistry = "", IsActive = true },
                    new() { ClientCode = "100", Description = "Public Service Agency", FinancialMinistry = "BC Public Service Agency", IsActive = true },
                    new() { ClientCode = "105", Description = "Attorney General", FinancialMinistry = "Attorney General", IsActive = true },
                    new() { ClientCode = "106", Description = "Merit Commissioner", FinancialMinistry = "Office of the Merit Commissioner", IsActive = true },
                    new() { ClientCode = "109", Description = "Representative for Children and Youth", FinancialMinistry = "Representative for Children and Youth", IsActive = true },
                    new() { ClientCode = "112", Description = "Citizens' Services", FinancialMinistry = "Citizens Services", IsActive = true },
                    new() { ClientCode = "113", Description = "Human Rights Commissioner", FinancialMinistry = "Office of the BC Human Rights Commission", IsActive = true },
                    new() { ClientCode = "114", Description = "Auditor General for Local Government", FinancialMinistry = "Housing and Municipal Affairs", IsActive = true },
                    new() { ClientCode = "115", Description = "Environmental Assessment Office", FinancialMinistry = "Environment and Parks", IsActive = true },
                    new() { ClientCode = "120", Description = "Indigenous Relations and Reconciliation", FinancialMinistry = "Indigenous Relations and Reconciliation", IsActive = true },
                    new() { ClientCode = "125", Description = "Jobs and Economic Growth", FinancialMinistry = "Jobs and Economic Growth", IsActive = true },
                    new() { ClientCode = "126", Description = "Tourism, Arts, Culture and Sport", FinancialMinistry = "Tourism Arts Culture and Sport", IsActive = true },
                    new() { ClientCode = "127", Description = "Labour", FinancialMinistry = "Labour", IsActive = true },
                    new() { ClientCode = "128", Description = "Forests", FinancialMinistry = "Forests", IsActive = true },
                    new() { ClientCode = "130", Description = "Agriculture and Food", FinancialMinistry = "Agriculture and Food", IsActive = true },
                    new() { ClientCode = "131", Description = "Housing", FinancialMinistry = "Housing and Municipal Affairs", IsActive = true },
                    new() { ClientCode = "133", Description = "Water, Land and Resource Stewardship", FinancialMinistry = "Water Land and Resource Stewardship", IsActive = true },
                    new() { ClientCode = "134", Description = "Emergency Management and Climate Readiness", FinancialMinistry = "Emergency Management and Climate Readiness", IsActive = true },
                    new() { ClientCode = "135", Description = "Mining and Critical Minerals", FinancialMinistry = "Mining and Critical Minerals", IsActive = true },
                    new() { ClientCode = "136", Description = "Infrastructure", FinancialMinistry = "Infrastructure", IsActive = true },
                    // Special accounts
                    new() { ClientCode = "300", Description = "BC Arts and Culture Endowment special account", FinancialMinistry = "Tourism Arts Culture and Sport", IsActive = true },
                    new() { ClientCode = "301", Description = "Park Enhancement Fund special account", FinancialMinistry = "Environment and Parks", IsActive = true },
                    new() { ClientCode = "302", Description = "Housing Endowment Fund special account", FinancialMinistry = "Housing and Municipal Affairs", IsActive = true },
                    new() { ClientCode = "303", Description = "Long Term Disability Fund special account", FinancialMinistry = "BC Public Service Agency", IsActive = true },
                    new() { ClientCode = "304", Description = "Housing Priority Initiatives special account", FinancialMinistry = "Finance", IsActive = true },
                    new() { ClientCode = "305", Description = "Provincial Home Acquisition Wind Up special account", FinancialMinistry = "Finance", IsActive = true },
                    new() { ClientCode = "307", Description = "Civil Forfeiture Account", FinancialMinistry = "Solicitor General", IsActive = true },
                    new() { ClientCode = "310", Description = "Production Insurance Account", FinancialMinistry = "Agriculture and Food", IsActive = true },
                    new() { ClientCode = "311", Description = "British Columbia Training and Education Savings Program", FinancialMinistry = "Education and Child Care", IsActive = true },
                    new() { ClientCode = "315", Description = "Innovative Clean Energy Fund special account", FinancialMinistry = "Energy and Climate Solutions", IsActive = true },
                    new() { ClientCode = "320", Description = "First Nations Clean Energy Business Fund special account", FinancialMinistry = "Indigenous Relations and Reconciliation", IsActive = true },
                    new() { ClientCode = "321", Description = "First Nations Equity Financing special account", FinancialMinistry = "", IsActive = true },
                    new() { ClientCode = "324", Description = "British Columbia Strategic Investments special account", FinancialMinistry = "Jobs and Economic Growth", IsActive = true },
                    // Consolidation accounts
                    new() { ClientCode = "400", Description = "Consolidation", FinancialMinistry = "", IsActive = true },
                    new() { ClientCode = "401", Description = "BC Prosperity Fund", FinancialMinistry = "", IsActive = true },
                    new() { ClientCode = "700", Description = "Finance-TBS Budget", FinancialMinistry = "Finance", IsActive = true },
                    // Other accounts with alpha-numeric codes
                    new() { ClientCode = "0AK", Description = "Public Guardian and Trustee Operating Account", FinancialMinistry = "Attorney General", IsActive = true },
                    new() { ClientCode = "0AT", Description = "BC Timber Sales Account", FinancialMinistry = "Forests", IsActive = true },
                    new() { ClientCode = "0BH", Description = "University Endowment Lands Administration Account", FinancialMinistry = "Housing and Municipal Affairs", IsActive = true },
                    new() { ClientCode = "0BM", Description = "Crown Land Small Business and Revenue", FinancialMinistry = "Finance", IsActive = true },
                    new() { ClientCode = "0BR", Description = "Corrections Work Program Account", FinancialMinistry = "Solicitor General", IsActive = true },
                    new() { ClientCode = "0ET", Description = "Teachers Act Special Account", FinancialMinistry = "Education and Child Care", IsActive = true },
                    new() { ClientCode = "0F3", Description = "Physical Fitness and Amateur Sports Fund", FinancialMinistry = "Tourism Arts Culture and Sport", IsActive = true },
                    new() { ClientCode = "0F9", Description = "First Citizens Fund", FinancialMinistry = "Indigenous Relations and Reconciliation", IsActive = true },
                    new() { ClientCode = "0FC", Description = "Criminal Asset Management Fund", FinancialMinistry = "Solicitor General", IsActive = true },
                    new() { ClientCode = "0FE", Description = "Forest Stand Management Fund", FinancialMinistry = "Forests", IsActive = true },
                    new() { ClientCode = "0FK", Description = "Insurance and Risk Management Account", FinancialMinistry = "Finance", IsActive = true },
                    new() { ClientCode = "0FS", Description = "Victim Surcharge Special Account", FinancialMinistry = "Solicitor General", IsActive = true },
                    new() { ClientCode = "0HL", Description = "Health Special Account", FinancialMinistry = "Health", IsActive = true },
                    new() { ClientCode = "0KR", Description = "Crown Land special account", FinancialMinistry = "Forests", IsActive = true },
                    new() { ClientCode = "0MH", Description = "Medical and Health Care Services", FinancialMinistry = "", IsActive = true },
                    new() { ClientCode = "0NF", Description = "Northern Development Fund", FinancialMinistry = "Jobs and Economic Growth", IsActive = true },
                    new() { ClientCode = "0SE", Description = "Sustainable Environment Fund", FinancialMinistry = "Environment and Parks", IsActive = true },
                    // Trust accounts
                    new() { ClientCode = "0T6", Description = "Trust-Public Safety and Solicitor General", FinancialMinistry = "Solicitor General", IsActive = true },
                    new() { ClientCode = "0TA", Description = "Trust-Agriculture and Food", FinancialMinistry = "Agriculture and Food", IsActive = true },
                    new() { ClientCode = "0TB", Description = "Trust-Attorney General", FinancialMinistry = "Attorney General", IsActive = true },
                    new() { ClientCode = "0TD", Description = "Trust-Education and Child Care", FinancialMinistry = "Education and Child Care", IsActive = true },
                    new() { ClientCode = "0TE", Description = "Energy and Climate Solutions", FinancialMinistry = "Energy and Climate Solutions", IsActive = true },
                    new() { ClientCode = "0TF", Description = "Environment and Parks", FinancialMinistry = "Environment and Parks", IsActive = true },
                    new() { ClientCode = "0TG", Description = "Trust-Finance", FinancialMinistry = "Finance", IsActive = true },
                    new() { ClientCode = "0TH", Description = "Trust-Forests", FinancialMinistry = "Forests", IsActive = true },
                    new() { ClientCode = "0TJ", Description = "Trust-Health", FinancialMinistry = "Health", IsActive = true },
                    new() { ClientCode = "0TK", Description = "Trust-Children and Family Dev", FinancialMinistry = "Children and Family Development", IsActive = true },
                    new() { ClientCode = "0TL", Description = "Trust-Small Business, Technology and Economic Development", FinancialMinistry = "Finance", IsActive = true },
                    new() { ClientCode = "0TM", Description = "Trust-Crown Lands", FinancialMinistry = "Forests", IsActive = true },
                    new() { ClientCode = "0TN", Description = "Trust-Crown Lands Minor Trusts", FinancialMinistry = "Forests", IsActive = true },
                    new() { ClientCode = "0TQ", Description = "Trust-Municipal Affairs", FinancialMinistry = "Housing and Municipal Affairs", IsActive = true },
                    new() { ClientCode = "0TR", Description = "Trust-Social Dev and Poverty Reduction", FinancialMinistry = "Social Development and Poverty Reduction", IsActive = true },
                    new() { ClientCode = "0TV", Description = "Trust-Natural Gas Development", FinancialMinistry = "Energy and Climate Solutions", IsActive = true },
                    new() { ClientCode = "0TW", Description = "Trust-Labour", FinancialMinistry = "Labour", IsActive = true },
                    new() { ClientCode = "0TX", Description = "Trust-Jobs and Economic Growth", FinancialMinistry = "Jobs and Economic Growth", IsActive = true },
                    new() { ClientCode = "0TZ", Description = "Trust-Post-Secondary Education and Future Skills", FinancialMinistry = "Post Secondary Education and Future Skills", IsActive = true },
                    new() { ClientCode = "0VB", Description = "Royal British Columbia Museum", FinancialMinistry = "", IsActive = true },
                    new() { ClientCode = "0VC", Description = "Vital Statistics", FinancialMinistry = "Health", IsActive = true }
                };
#pragma warning restore S1192 // Use 'new(...)'
                foreach (var clientCode in clientCodes)
                {
                    var existing = await CasClientCodeRepository.FirstOrDefaultAsync(s => s.ClientCode == clientCode.ClientCode);
                    if (existing == null)
                    {
                        await CasClientCodeRepository.InsertAsync(clientCode);
                    }
                }
        }
    }
}