DO $$ 
DECLARE
    json_data jsonb := '[
  {
    "SectorCode": "1",
    "SectorName": "Arts & Culture",
    "SubSectors": [
      {
        "SubSectorCode": "101",
        "SubSectorName": "Dance – Performance/ Education"
      },
      {
        "SubSectorCode": "102",
        "SubSectorName": "Fair – Agriculture"
      },
      {
        "SubSectorCode": "103",
        "SubSectorName": "Fair – Community"
      },
      {
        "SubSectorCode": "104",
        "SubSectorName": "Festival"
      },
      {
        "SubSectorCode": "105",
        "SubSectorName": "Museum/Heritage/Archives"
      },
      {
        "SubSectorCode": "106",
        "SubSectorName": "Music – Performance/ Education"
      },
      {
        "SubSectorCode": "107",
        "SubSectorName": "Theatre – Performance/ Education"
      },
      {
        "SubSectorCode": "108",
        "SubSectorName": "Visual Arts – Exhibition/ Education"
      },
      {
        "SubSectorCode": "109",
        "SubSectorName": "Media Arts – Exhibition/ Performance/Education"
      },
      {
        "SubSectorCode": "110",
        "SubSectorName": "Literature – Exhibition/ Performance/Education"
      },
      {
        "SubSectorCode": "111",
        "SubSectorName": "Culture/Multicultural"
      },
      {
        "SubSectorCode": "112",
        "SubSectorName": "Performance/Education"
      },
      {
        "SubSectorCode": "113",
        "SubSectorName": "Other – Arts and CultureOther"
      }
    ]
  },
  {
    "SectorCode": "2",
    "SectorName": "Sport",
    "SubSectors": [
      {
        "SubSectorCode": "201",
        "SubSectorName": "Aquatics/Swimming"
      },
      {
        "SubSectorCode": "202",
        "SubSectorName": "Baseball/Softball"
      },
      {
        "SubSectorCode": "203",
        "SubSectorName": "Basketball"
      },
      {
        "SubSectorCode": "204",
        "SubSectorName": "Biking/Cycling"
      },
      {
        "SubSectorCode": "205",
        "SubSectorName": "Bowling/Lawn Bowling"
      },
      {
        "SubSectorCode": "206",
        "SubSectorName": "Curling"
      },
      {
        "SubSectorCode": "207",
        "SubSectorName": "Equestrian"
      },
      {
        "SubSectorCode": "208",
        "SubSectorName": "Football/Rugby"
      },
      {
        "SubSectorCode": "209",
        "SubSectorName": "Golf Gymnastics"
      },
      {
        "SubSectorCode": "210",
        "SubSectorName": "Hockey"
      },
      {
        "SubSectorCode": "211",
        "SubSectorName": "Lacrosse"
      },
      {
        "SubSectorCode": "212",
        "SubSectorName": "Martial Arts/Combat Sports"
      },
      {
        "SubSectorCode": "213",
        "SubSectorName": "Racquet Sports Ringette/Rowing/Sailing/Boating/ Waterski & Wakeboard"
      },
      {
        "SubSectorCode": "214",
        "SubSectorName": "Seniors Games"
      },
      {
        "SubSectorCode": "215",
        "SubSectorName": "Skating"
      },
      {
        "SubSectorCode": "216",
        "SubSectorName": "Skiing/Snow Sports"
      },
      {
        "SubSectorCode": "217",
        "SubSectorName": "Soccer"
      },
      {
        "SubSectorCode": "218",
        "SubSectorName": "Special Olympics"
      },
      {
        "SubSectorCode": "219",
        "SubSectorName": "Track & Field"
      },
      {
        "SubSectorCode": "220",
        "SubSectorName": "Volleyball"
      },
      {
        "SubSectorCode": "221",
        "SubSectorName": "Other – Sport"
      }
    ]
  },
  {
    "SectorCode": "3",
    "SectorName": "Human & Social Services",
    "SubSectors": [
      {
        "SubSectorCode": "301",
        "SubSectorName": "Disability Supports "
      },
      {
        "SubSectorCode": "302",
        "SubSectorName": "Mental Health"
      },
      {
        "SubSectorCode": "303",
        "SubSectorName": "Substance Use"
      },
      {
        "SubSectorCode": "304",
        "SubSectorName": "Health/Health Condition Programs"
      },
      {
        "SubSectorCode": "305",
        "SubSectorName": "Food and Nutrition"
      },
      {
        "SubSectorCode": "306",
        "SubSectorName": "Hospice"
      },
      {
        "SubSectorCode": "307",
        "SubSectorName": "Bereavement"
      },
      {
        "SubSectorCode": "308",
        "SubSectorName": "Immigrant/Refugee Supports"
      },
      {
        "SubSectorCode": "309",
        "SubSectorName": "Scouts/Cadets"
      },
      {
        "SubSectorCode": "310",
        "SubSectorName": "Seniors Service/Activities"
      },
      {
        "SubSectorCode": "311",
        "SubSectorName": "Service Clubs/Community Donations"
      },
      {
        "SubSectorCode": "312",
        "SubSectorName": "Emergency Social Services, Outreach"
      },
      {
        "SubSectorCode": "313",
        "SubSectorName": "Education/Tutoring Services"
      },
      {
        "SubSectorCode": "314",
        "SubSectorName": "Children, Youth and Family Services"
      },
      {
        "SubSectorCode": "315",
        "SubSectorName": "Other – Human and Social Services"
      }
    ]
  },
  {
    "SectorCode": "4",
    "SectorName": "Environment",
    "SubSectors": [
      {
        "SubSectorCode": "401",
        "SubSectorName": "Agriculture"
      },
      {
        "SubSectorCode": "402",
        "SubSectorName": "Animal Welfare"
      },
      {
        "SubSectorCode": "403",
        "SubSectorName": "Climate Change Adaptation"
      },
      {
        "SubSectorCode": "404",
        "SubSectorName": "Ecosystem Conservation"
      },
      {
        "SubSectorCode": "405",
        "SubSectorName": " Education/Outreach "
      },
      {
        "SubSectorCode": "406",
        "SubSectorName": "Other – Environment"
      }
    ]
  }
]';
BEGIN
        -- Insert into "Sectors" table
    INSERT INTO public."Sectors"
    ("Id", "SectorName", "SectorCode", "ExtraProperties", "ConcurrencyStamp", "CreationTime") 
    SELECT
        gen_random_uuid(),
        data->>'SectorName',
        data->>'SectorCode',
        '',
        '',
        pg_catalog.now()
    FROM jsonb_array_elements(json_data::jsonb) AS data;
   
    -- Insert into "SubSectors" table
    INSERT INTO public."SubSectors" 
    ("Id", "SectorId", "SubSectorCode", "SubSectorName", "ExtraProperties", "ConcurrencyStamp", "CreationTime")
    SELECT
        gen_random_uuid(),
        sector."Id",
        subsector->>'SubSectorCode',
        subsector->>'SubSectorName',
        '',
        '',
        pg_catalog.now()
    FROM
        jsonb_array_elements(json_data::jsonb) AS data,
        LATERAL (
            SELECT "Id"
            FROM public."Sectors"
            WHERE "SectorCode" = data->>'SectorCode'
            LIMIT 1
        ) AS sector,
        jsonb_array_elements(data->'SubSectors') AS subsector;
END $$;

