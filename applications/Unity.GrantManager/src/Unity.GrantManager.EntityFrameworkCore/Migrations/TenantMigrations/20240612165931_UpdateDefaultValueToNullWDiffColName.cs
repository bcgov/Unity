using Microsoft.EntityFrameworkCore.Migrations;
using System.Collections.Generic;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class UpdateDefaultValueToNullWDiffColName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            List<(string TableName, string ColumnName, string UpdatedName, bool IsNullable)> columnsToUpdate = GetFieldsToUpdate();

            foreach (var (tableName, colName, updatedName, isNullable) in columnsToUpdate)
            {
                var val = isNullable ? "NULL" : "''";
                var updateSql = $"UPDATE \"public\".\"{tableName}\" SET \"{colName}\" = {val} WHERE \"{colName}\" = '{{{updatedName}}}'";
                migrationBuilder.Sql(updateSql);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            List<(string TableName, string ColumnName, string UpdatedName, bool IsNullable)> columnsToUpdate = GetFieldsToUpdate();

            foreach (var (tableName, colName, updatedName, isNullable) in columnsToUpdate)
            {
                var val = isNullable ? "IS NULL" : "= ''";
                var updateSql = $"UPDATE \"public\".\"{tableName}\" SET \"{colName}\" = '{{{updatedName}}}' WHERE \"{colName}\" {val}";
                migrationBuilder.Sql(updateSql);
            }
        }

        private static List<(string TableName, string ColumnName, string UpdatedName, bool IsNullable)> GetFieldsToUpdate()
        {
            return new List<(string TableName, string ColumnName, string UpdatedName, bool IsNullable)>
            {
                ("Applications", "EconomicRegion", "Region", false),
                ("ApplicantAgents", "Name", "ContactName", false),
                ("ApplicantAgents", "Phone", "ContactPhone", false),
                ("ApplicantAgents", "Phone2", "ContactPhone2", false),
                ("ApplicantAgents", "Email", "ContactEmail", false),
                ("ApplicantAgents", "Title", "ContactTitle", false),
            };
        }
    }
}
