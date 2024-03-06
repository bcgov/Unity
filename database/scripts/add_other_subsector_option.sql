INSERT INTO public."SubSectors" 
    ("Id", "SectorId", "SubSectorCode", "SubSectorName", "ExtraProperties", "ConcurrencyStamp", "CreationTime")
    SELECT
        gen_random_uuid(),
        sector."Id",
        '0',
        'Other',
        '',
        '',
        pg_catalog.now()
    FROM public."Sectors" as sector;