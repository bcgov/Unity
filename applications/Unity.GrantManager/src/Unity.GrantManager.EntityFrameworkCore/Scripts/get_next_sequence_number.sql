CREATE OR REPLACE FUNCTION public.get_next_sequence_number(p_tenant_id uuid, p_prefix text) RETURNS bigint
    LANGUAGE plpgsql
    AS $$
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
                $$;
