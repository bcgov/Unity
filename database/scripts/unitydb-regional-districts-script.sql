DO $$ 
DECLARE
    json_data jsonb := '[{"RegionalDistrictCode":"1","RegionalDistrictName":"Capital","EconomicRegionCode":"5910"},{"RegionalDistrictCode":"2","RegionalDistrictName":"Cowichan Valley","EconomicRegionCode":"5910"},{"RegionalDistrictCode":"3","RegionalDistrictName":"Nanaimo","EconomicRegionCode":"5910"},{"RegionalDistrictCode":"4","RegionalDistrictName":"Alberni-Clayoquot","EconomicRegionCode":"5910"},{"RegionalDistrictCode":"5","RegionalDistrictName":"Strathcona","EconomicRegionCode":"5910"},{"RegionalDistrictCode":"6","RegionalDistrictName":"Comox Valley","EconomicRegionCode":"5910"},{"RegionalDistrictCode":"7","RegionalDistrictName":"qathet","EconomicRegionCode":"5910"},{"RegionalDistrictCode":"8","RegionalDistrictName":"Mount Waddington","EconomicRegionCode":"5910"},{"RegionalDistrictCode":"9","RegionalDistrictName":"Central Coast","EconomicRegionCode":"5910"},{"RegionalDistrictCode":"10","RegionalDistrictName":"Fraser Valley","EconomicRegionCode":"5920"},{"RegionalDistrictCode":"11","RegionalDistrictName":"Metro Vancouver","EconomicRegionCode":"5920"},{"RegionalDistrictCode":"12","RegionalDistrictName":"Sunshine Coast","EconomicRegionCode":"5920"},{"RegionalDistrictCode":"13","RegionalDistrictName":"Squamish-Lillooet","EconomicRegionCode":"5920"},{"RegionalDistrictCode":"14","RegionalDistrictName":"Okanagan-Similkameen","EconomicRegionCode":"5930"},{"RegionalDistrictCode":"15","RegionalDistrictName":"Thompson-Nicola","EconomicRegionCode":"5930"},{"RegionalDistrictCode":"16","RegionalDistrictName":"Central Okanagan","EconomicRegionCode":"5930"},{"RegionalDistrictCode":"17","RegionalDistrictName":"North Okanagan","EconomicRegionCode":"5930"},{"RegionalDistrictCode":"18","RegionalDistrictName":"Columbia-Shuswap","EconomicRegionCode":"5930"},{"RegionalDistrictCode":"19","RegionalDistrictName":"East Kootenay","EconomicRegionCode":"5940"},{"RegionalDistrictCode":"20","RegionalDistrictName":"Central Kootenay","EconomicRegionCode":"5940"},{"RegionalDistrictCode":"21","RegionalDistrictName":"Kootenay Boundary","EconomicRegionCode":"5940"},{"RegionalDistrictCode":"22","RegionalDistrictName":"Cariboo","EconomicRegionCode":"5950"},{"RegionalDistrictCode":"23","RegionalDistrictName":"Fraser-Fort George","EconomicRegionCode":"5950"},{"RegionalDistrictCode":"24","RegionalDistrictName":"Skeena-Queen Charlotte","EconomicRegionCode":"5960"},{"RegionalDistrictCode":"25","RegionalDistrictName":"Kitimat-Stikine","EconomicRegionCode":"5960"},{"RegionalDistrictCode":"26","RegionalDistrictName":"Bulkley-Nechako","EconomicRegionCode":"5970"},{"RegionalDistrictCode":"27","RegionalDistrictName":"Stikine Region","EconomicRegionCode":"5970"},{"RegionalDistrictCode":"28","RegionalDistrictName":"Peace River","EconomicRegionCode":"5980"},{"RegionalDistrictCode":"29","RegionalDistrictName":"Northern Rockies Regional Municipality","EconomicRegionCode":"5980"}]';
BEGIN
    -- Insert into "RegionalDistricts" table
    INSERT INTO public."RegionalDistricts"
    ("Id", "RegionalDistrictName", "RegionalDistrictCode", "EconomicRegionCode", "ExtraProperties", "ConcurrencyStamp", "CreationTime")
    SELECT
        gen_random_uuid(),
        data->>'RegionalDistrictName',
        data->>'RegionalDistrictCode',
        data->>'EconomicRegionCode',
        '',
        '',
        pg_catalog.now()
    FROM jsonb_array_elements(json_data::jsonb) AS data;
   
END $$;
