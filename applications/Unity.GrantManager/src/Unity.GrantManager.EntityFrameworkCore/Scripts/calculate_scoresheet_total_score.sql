CREATE OR REPLACE FUNCTION "Reporting".calculate_scoresheet_total_score(scoresheet_instance_id uuid)
 RETURNS NUMERIC
 LANGUAGE plpgsql
AS $function$
DECLARE
    total_score NUMERIC := 0;
    question_record RECORD;
    answer_value TEXT;
    question_definition JSONB;
    yes_value INTEGER;
    no_value INTEGER;
    selected_option_value INTEGER;
BEGIN
    -- Loop through all questions for this scoresheet instance
    FOR question_record IN
        SELECT 
            q."Type" as question_type,
            q."Definition" as definition,
            a."CurrentValue" as current_value
        FROM "Flex"."Answers" a
        INNER JOIN "Flex"."Questions" q ON a."QuestionId" = q."Id"
        WHERE a."ScoresheetInstanceId" = scoresheet_instance_id
    LOOP
        -- Extract the answer value from the CurrentValue JSON
        BEGIN
            answer_value := (question_record.current_value::JSONB)->>'value';
        EXCEPTION
            WHEN OTHERS THEN
                answer_value := NULL;
        END;
        
        -- Skip if no answer value
        IF answer_value IS NULL OR answer_value = '' THEN
            CONTINUE;
        END IF;
        
        -- Calculate score based on question type (matching QuestionType enum)
        CASE question_record.question_type
            WHEN 1 THEN -- Number type
                -- For Number questions, the answer value is the score
                BEGIN
                    IF answer_value ~ '^-?[0-9]+\.?[0-9]*$' THEN
                        total_score := total_score + answer_value::NUMERIC;
                    END IF;
                EXCEPTION
                    WHEN OTHERS THEN
                        -- Skip invalid numeric values
                        NULL;
                END;
                
            WHEN 6 THEN -- YesNo type
                -- For YesNo questions, get the yes_value or no_value from definition
                BEGIN
                    question_definition := question_record.definition::JSONB;
                    yes_value := COALESCE((question_definition->>'yes_value')::INTEGER, 0);
                    no_value := COALESCE((question_definition->>'no_value')::INTEGER, 0);
                    
                    IF upper(answer_value) IN ('YES', 'TRUE', '1', 'T') THEN
                        total_score := total_score + yes_value;
                    ELSIF upper(answer_value) IN ('NO', 'FALSE', '0', 'F') THEN
                        total_score := total_score + no_value;
                    END IF;
                EXCEPTION
                    WHEN OTHERS THEN
                        -- Skip invalid definition parsing
                        NULL;
                END;
                
            WHEN 12 THEN -- SelectList type
                -- For SelectList questions, find the selected option and get its numeric_value
                BEGIN
                    question_definition := question_record.definition::JSONB;
                    
                    -- Find the matching option in the options array
                    SELECT COALESCE((option->>'numeric_value')::INTEGER, 0)
                    INTO selected_option_value
                    FROM jsonb_array_elements(question_definition->'options') AS option
                    WHERE option->>'value' = answer_value
                    LIMIT 1;
                    
                    IF selected_option_value IS NOT NULL THEN
                        total_score := total_score + selected_option_value;
                    END IF;
                EXCEPTION
                    WHEN OTHERS THEN
                        -- Skip invalid definition parsing or missing options
                        NULL;
                END;
                
            ELSE
                -- For Text (2) and TextArea (14), no score contribution
                NULL;
        END CASE;
    END LOOP;
    
    RETURN COALESCE(total_score, 0);
END;
$function$;