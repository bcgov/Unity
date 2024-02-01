DO $$ 
DECLARE
    json_data jsonb := '[{"EconomicRegionCode":"5910","EconomicRegionName":"Vancouver Island/Coast"},{"EconomicRegionCode":"5920","EconomicRegionName":"Mainland/Southwest"},{"EconomicRegionCode":"5930","EconomicRegionName":"Thompson/Okanagan"},{"EconomicRegionCode":"5940","EconomicRegionName":"Kootenay"},{"EconomicRegionCode":"5950","EconomicRegionName":"Cariboo"},{"EconomicRegionCode":"5960","EconomicRegionName":"North Coast"},{"EconomicRegionCode":"5970","EconomicRegionName":"Nechako"},{"EconomicRegionCode":"5980","EconomicRegionName":"Northeast"}]';
BEGIN
    -- Insert into "Sectors" table
    INSERT INTO public."EconomicRegions"
    ("Id", "EconomicRegionName", "EconomicRegionCode", "ExtraProperties", "ConcurrencyStamp", "CreationTime")
    SELECT
        gen_random_uuid(),
        data->>'EconomicRegionName',
        data->>'EconomicRegionCode',
        '',
        '',
        pg_catalog.now()
    FROM jsonb_array_elements(json_data::jsonb) AS data;
   
END $$;
