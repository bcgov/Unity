using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class Add_UnityApplicationId_Sequence_Function : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION get_next_sequence_number(
                    p_tenant_id UUID,
                    p_prefix TEXT
                ) RETURNS BIGINT AS $$
                DECLARE
                    v_sequence_name TEXT;
                    v_schema_name TEXT;
                BEGIN
                    -- Get schema and build sequence name
                    v_schema_name := current_schema();
                    v_sequence_name := 'seq_unity_' || REPLACE(p_tenant_id::TEXT, '-', '') || '_' || 
                                       REPLACE(REPLACE(p_prefix, '-', ''), ' ', '');
                    
                    -- Create sequence if it doesn't exist
                    PERFORM 1 FROM pg_sequences 
                    WHERE schemaname = v_schema_name AND sequencename = v_sequence_name;
                    
                    IF NOT FOUND THEN
                        BEGIN
                            EXECUTE format('CREATE SEQUENCE %I.%I START WITH 1', v_schema_name, v_sequence_name);
                        EXCEPTION 
                            WHEN duplicate_table THEN
                                -- Another transaction created it - that's OK, continue
                                NULL;
                        END;
                    END IF;
                    
                    -- Get and return next value
                    RETURN nextval(format('%I.%I', v_schema_name, v_sequence_name));
                END;
                $$ LANGUAGE plpgsql;
                ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS get_next_sequence_number(UUID, TEXT);");
        }
    }
}
