using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class Add_UnitySequenceCounters_Table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PRIMARY KEY on (tenant_id, prefix) enforces uniqueness and provides the index
            // required by the ON CONFLICT clause in the upsert counter query.
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""unity_sequence_counters"" (
                    ""tenant_id""     UUID   NOT NULL,
                    ""prefix""        TEXT   NOT NULL,
                    ""current_value"" BIGINT NOT NULL DEFAULT 0,
                    CONSTRAINT ""PK_unity_sequence_counters"" PRIMARY KEY (""tenant_id"", ""prefix"")
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""unity_sequence_counters"";");
        }
    }
}
