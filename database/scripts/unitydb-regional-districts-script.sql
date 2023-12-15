DO $$ 
DECLARE
    json_data jsonb := '[ { "RegionalDistrictCode": "1", "RegionalDistrictName": "Capital" }, { "RegionalDistrictCode": "2", "RegionalDistrictName": "Cowichan Valley" }, { "RegionalDistrictCode": "3", "RegionalDistrictName": "Nanaimo" }, { "RegionalDistrictCode": "4", "RegionalDistrictName": "Alberni-Clayoquot" }, { "RegionalDistrictCode": "5", "RegionalDistrictName": "Strathcona" }, { "RegionalDistrictCode": "6", "RegionalDistrictName": "Comox Valley" }, { "RegionalDistrictCode": "7", "RegionalDistrictName": "qathet" }, { "RegionalDistrictCode": "8", "RegionalDistrictName": "Mount Waddington" }, { "RegionalDistrictCode": "9", "RegionalDistrictName": "Central Coast" }, { "RegionalDistrictCode": "10", "RegionalDistrictName": "Fraser Valley" }, { "RegionalDistrictCode": "11", "RegionalDistrictName": "Metro Vancouver" }, { "RegionalDistrictCode": "12", "RegionalDistrictName": "Sunshine Coast" }, { "RegionalDistrictCode": "13", "RegionalDistrictName": "Squamish-Lillooet" }, { "RegionalDistrictCode": "14", "RegionalDistrictName": "Okanagan-Similkameen" }, { "RegionalDistrictCode": "15", "RegionalDistrictName": "Thompson-Nicola" }, { "RegionalDistrictCode": "16", "RegionalDistrictName": "Central Okanagan" }, { "RegionalDistrictCode": "17", "RegionalDistrictName": "North Okanagan" }, { "RegionalDistrictCode": "18", "RegionalDistrictName": "Columbia-Shuswap" }, { "RegionalDistrictCode": "19", "RegionalDistrictName": "East Kootenay" }, { "RegionalDistrictCode": "20", "RegionalDistrictName": "Central Kootenay" }, { "RegionalDistrictCode": "21", "RegionalDistrictName": "Kootenay Boundary" }, { "RegionalDistrictCode": "22", "RegionalDistrictName": "Cariboo" }, { "RegionalDistrictCode": "23", "RegionalDistrictName": "Fraser-Fort George" }, { "RegionalDistrictCode": "24", "RegionalDistrictName": "Skeena-Queen Charlotte" }, { "RegionalDistrictCode": "25", "RegionalDistrictName": "Kitimat-Stikine" }, { "RegionalDistrictCode": "26", "RegionalDistrictName": "Bulkley-Nechako" }, { "RegionalDistrictCode": "27", "RegionalDistrictName": "Stikine Region" }, { "RegionalDistrictCode": "28", "RegionalDistrictName": "Peace River" }, { "RegionalDistrictCode": "29", "RegionalDistrictName": "Northern Rockies Regional Municipality" } ]';
BEGIN
    -- Insert into "RegionalDistricts" table
    INSERT INTO public."RegionalDistricts"
    ("Id", "RegionalDistrictName", "RegionalDistrictCode", "ExtraProperties", "ConcurrencyStamp", "CreationTime")
    SELECT
        gen_random_uuid(),
        data->>'RegionalDistrictName',
        data->>'RegionalDistrictCode',
        '',
        '',
        pg_catalog.now()
    FROM jsonb_array_elements(json_data::jsonb) AS data;
   
END $$;
