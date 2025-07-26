
-- BEGIN; --uncomment this if you want a trial run to see if the execution has no errors, uncomment also the ROLLBACK code below

-- Stored procedure to populate ApplicationId in ApplicantAddresses table
-- This procedure is idempotent and can be run multiple times safely

CREATE OR REPLACE FUNCTION populate_application_addresses()
RETURNS void AS $$
DECLARE
    applicant_rec RECORD;
    app_rec RECORD;
    addr_rec RECORD;
    first_app_id uuid;
    is_first_app boolean;
    update_count integer := 0;
    insert_count integer := 0;
BEGIN
    -- Step 1: Update existing ApplicantAddresses with the first application for each applicant
    FOR applicant_rec IN 
        SELECT DISTINCT "ApplicantId" 
        FROM public."ApplicantAddresses" 
        WHERE "ApplicationId" IS NULL
    LOOP
        -- Find the first application for this applicant (by CreationTime)
        SELECT "Id" INTO first_app_id
        FROM public."Applications"
        WHERE "ApplicantId" = applicant_rec."ApplicantId"
        ORDER BY "CreationTime" ASC
        LIMIT 1;
        
        -- Update existing addresses to link to the first application
        IF first_app_id IS NOT NULL THEN
            UPDATE public."ApplicantAddresses"
            SET "ApplicationId" = first_app_id,
                "LastModificationTime" = NOW(),
                "LastModifierId" = "CreatorId" -- Keep the original creator as modifier for this update
            WHERE "ApplicantId" = applicant_rec."ApplicantId" 
            AND "ApplicationId" IS NULL;
            
            GET DIAGNOSTICS update_count = ROW_COUNT;
            RAISE NOTICE 'Updated % addresses for Applicant % with first Application %', update_count, applicant_rec."ApplicantId", first_app_id;
        END IF;
    END LOOP;
    
    -- Step 2: Create address copies for additional applications
    FOR applicant_rec IN 
        SELECT "ApplicantId", COUNT(*) as app_count
        FROM public."Applications"
        GROUP BY "ApplicantId"
        HAVING COUNT(*) > 1
    LOOP
        -- Get the first application ID for this applicant
        SELECT "Id" INTO first_app_id
        FROM public."Applications"
        WHERE "ApplicantId" = applicant_rec."ApplicantId"
        ORDER BY "CreationTime" ASC
        LIMIT 1;
        
        -- Process each additional application for this applicant
        FOR app_rec IN 
            SELECT "Id" as "ApplicationId"
            FROM public."Applications"
            WHERE "ApplicantId" = applicant_rec."ApplicantId"
            AND "Id" != first_app_id
            ORDER BY "CreationTime" ASC
        LOOP
            -- For each address type (1 and 2), check if address already exists for this application
            FOR addr_rec IN 
                SELECT *
                FROM public."ApplicantAddresses"
                WHERE "ApplicantId" = applicant_rec."ApplicantId"
                AND "ApplicationId" = first_app_id
            LOOP
                -- Check if address for this ApplicationId and AddressType already exists
                IF NOT EXISTS (
                    SELECT 1 
                    FROM public."ApplicantAddresses"
                    WHERE "ApplicationId" = app_rec."ApplicationId"
                    AND "AddressType" = addr_rec."AddressType"
                ) THEN
                    -- Insert cloned address for this additional application
                    INSERT INTO public."ApplicantAddresses" (
                        "Id",
                        "ApplicantId",
                        "City",
                        "Country",
                        "Province",
                        "Postal",
                        "Street",
                        "Street2",
                        "Unit",
                        "TenantId",
                        "ExtraProperties",
                        "ConcurrencyStamp",
                        "CreationTime",
                        "CreatorId",
                        "LastModificationTime",
                        "LastModifierId",
                        "AddressType",
                        "ApplicationId"
                    ) VALUES (
                        gen_random_uuid(), -- Generate new UUID
                        addr_rec."ApplicantId",
                        addr_rec."City",
                        addr_rec."Country",
                        addr_rec."Province",
                        addr_rec."Postal",
                        addr_rec."Street",
                        addr_rec."Street2",
                        addr_rec."Unit",
                        addr_rec."TenantId", -- Copy TenantId
                        addr_rec."ExtraProperties", -- Copy ExtraProperties
                        addr_rec."ConcurrencyStamp", -- Copy ConcurrencyStamp
                        NOW(), -- Set current time as CreationTime
                        addr_rec."CreatorId", -- Copy original CreatorId
                        NULL, -- Set LastModificationTime as NULL
                        NULL, -- Set LastModifierId as NULL
                        addr_rec."AddressType",
                        app_rec."ApplicationId" -- Link to the additional application
                    );
                    
                    insert_count := insert_count + 1;
                    RAISE NOTICE 'Created address clone for Application % (AddressType: %)', app_rec."ApplicationId", addr_rec."AddressType";
                END IF;
            END LOOP;
        END LOOP;
    END LOOP;
    
    RAISE NOTICE 'Address population completed successfully';
    RAISE NOTICE 'RUN SUMMARY: Total records inserted: %', insert_count;
END;
$$ LANGUAGE plpgsql;

-- Execute the function
SELECT populate_application_addresses();

-- Show some results to verify what would happen
SELECT 'RUN RESULTS - Showing sample of what would be created:' as message;

-- Show count of records that would be affected
SELECT 
    'Current NULL ApplicationId count' as description,
    COUNT(*) as count
FROM public."ApplicantAddresses" 
WHERE "ApplicationId" IS NULL

UNION ALL

SELECT 
    'Total ApplicantAddresses count' as description,
    COUNT(*) as count
FROM public."ApplicantAddresses"

UNION ALL

SELECT 
    'Applicants with multiple applications' as description,
    COUNT(*) as count
FROM (
    SELECT "ApplicantId"
    FROM public."Applications"
    GROUP BY "ApplicantId"
    HAVING COUNT(*) > 1
) multi_app_applicants;


-- ROLLBACK; --uncomment this if you want a trial run to see if the execution has no errors, uncomment also the BEGIN at the top

-- Show completion message
SELECT 'RUN COMPLETED.' as message;

