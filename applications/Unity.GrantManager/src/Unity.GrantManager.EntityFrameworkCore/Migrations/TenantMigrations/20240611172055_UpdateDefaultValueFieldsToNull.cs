using Microsoft.EntityFrameworkCore.Migrations;
using System.Collections.Generic;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class UpdateDefaultValueFieldsToNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            List<(string TableName, string ColumnName, bool IsNullable)> columnsToUpdate = GetFieldsToUpdate();

            foreach (var (tableName, colName, isNullable) in columnsToUpdate)
            {
                var val = isNullable ? "NULL" : "''";
                var updateSql = $"UPDATE \"public\".\"{tableName}\" SET \"{colName}\" = {val} WHERE \"{colName}\" = '{{{colName}}}'";
                migrationBuilder.Sql(updateSql);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            List<(string TableName, string ColumnName, bool IsNullable)> columnsToUpdate = GetFieldsToUpdate();

            foreach (var (tableName, colName, isNullable) in columnsToUpdate)
            {
                var val = isNullable ? "IS NULL" : "= ''";
                var updateSql = $"UPDATE \"public\".\"{tableName}\" SET \"{colName}\" = '{{{colName}}}' WHERE \"{colName}\" {val}";
                migrationBuilder.Sql(updateSql);
            }
        }

        private static List<(string TableName, string ColumnName, bool IsNullable)> GetFieldsToUpdate()
        {
            return new List<(string TableName, string ColumnName, bool IsNullable)>
            {
                ("Applications", "ProjectName", false),
                ("Applications", "ReferenceNo", false),
                ("Applications", "Acquisition", true),
                ("Applications", "Forestry", true),
                ("Applications", "ForestryFocus", true),
                ("Applications", "City", true),
                ("Applications", "Community", true),
                ("Applications", "ElectoralDistrict", true),
                ("Applications", "RegionalDistrict", true),
                ("Applications", "SigningAuthorityFullName", true),
                ("Applications", "SigningAuthorityTitle", true),
                ("Applications", "SigningAuthorityEmail", true),
                ("Applications", "SigningAuthorityBusinessPhone", true),
                ("Applications", "SigningAuthorityCellPhone", true),
                ("Applications", "Place", true),
                ("Applicants", "ApplicantName", false),
                ("Applicants", "NonRegisteredBusinessName", true),
                ("Applicants", "OrgName", true),
                ("Applicants", "OrgNumber", true),
                ("Applicants", "OrganizationType", true),
                ("Applicants", "Sector", true),
                ("Applicants", "SubSector", true),
                ("Applicants", "SectorSubSectorIndustryDesc", true),
                ("Applicants", "ApproxNumberOfEmployees", true)
            };
        }
    }
}
