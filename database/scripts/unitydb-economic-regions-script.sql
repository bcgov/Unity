DO $$ 
DECLARE
    json_data jsonb := '[{"EconomicRegionCode":"001","EconomicRegionName":"Vancouver Island/Coast"},{"EconomicRegionCode":"002","EconomicRegionName":"Mainland/Southwest"},{"EconomicRegionCode":"003","EconomicRegionName":"Thompson/Okanagan"},{"EconomicRegionCode":"004","EconomicRegionName":"Kootenay"},{"EconomicRegionCode":"005","EconomicRegionName":"Cariboo"},{"EconomicRegionCode":"006","EconomicRegionName":"North Coast"},{"EconomicRegionCode":"007","EconomicRegionName":"Nechako"},{"EconomicRegionCode":"008","EconomicRegionName":"Northeast"}]';
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
