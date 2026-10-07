using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Unity.GrantManager.EntityFrameworkCore;

#nullable disable

namespace Unity.GrantManager.Migrations.HostMigrations;

[DbContext(typeof(GrantManagerDbContext))]
[Migration("20260916233018_FixCommunitiesNames")]
public partial class FixCommunitiesNames : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            -- Duplicate-name communities: the two rows per pair are currently indistinguishable
            -- (same Name + RegionalDistrictCode, nothing else to key on). Pick one deterministically
            -- (lowest Id) for the first corrected name, then the plain match picks up the remaining row.
            UPDATE public."Communities" SET "Name" = 'Esquimalt - District municipality'
            WHERE "Id" = (SELECT "Id" FROM public."Communities" WHERE "Name" = 'Esquimalt' AND "RegionalDistrictCode" = '1' ORDER BY "Id" LIMIT 1);
            UPDATE public."Communities" SET "Name" = 'Esquimalt - Indian reserve'
            WHERE "Name" = 'Esquimalt' AND "RegionalDistrictCode" = '1';

            UPDATE public."Communities" SET "Name" = 'Langley - District municipality'
            WHERE "Id" = (SELECT "Id" FROM public."Communities" WHERE "Name" = 'Langley' AND "RegionalDistrictCode" = '11' ORDER BY "Id" LIMIT 1);
            UPDATE public."Communities" SET "Name" = 'Langley - City'
            WHERE "Name" = 'Langley' AND "RegionalDistrictCode" = '11';

            UPDATE public."Communities" SET "Name" = 'North Vancouver - District municipality'
            WHERE "Id" = (SELECT "Id" FROM public."Communities" WHERE "Name" = 'North Vancouver' AND "RegionalDistrictCode" = '11' ORDER BY "Id" LIMIT 1);
            UPDATE public."Communities" SET "Name" = 'North Vancouver - City'
            WHERE "Name" = 'North Vancouver' AND "RegionalDistrictCode" = '11';

            UPDATE public."Communities" SET "Name" = 'Alert Bay - Village'
            WHERE "Id" = (SELECT "Id" FROM public."Communities" WHERE "Name" = 'Alert Bay' AND "RegionalDistrictCode" = '8' ORDER BY "Id" LIMIT 1);
            UPDATE public."Communities" SET "Name" = 'Alert Bay - Indian reserve'
            WHERE "Name" = 'Alert Bay' AND "RegionalDistrictCode" = '8';

            -- Already unique by RegionalDistrictCode; no ambiguity.
            UPDATE public."Communities" SET "Name" = 'Okanagan (Part) 1 - North Okanagan'
            WHERE "Name" = 'Okanagan (Part) 1' AND "RegionalDistrictCode" = '17';
            UPDATE public."Communities" SET "Name" = 'Okanagan (Part) 1 - Thompson/Okanagan'
            WHERE "Name" = 'Okanagan (Part) 1' AND "RegionalDistrictCode" = '18';

            UPDATE public."Communities" SET "Name" = 'Sechelt (Part) - Sunshine Coast'
            WHERE "Name" = 'Sechelt (Part)' AND "RegionalDistrictCode" = '12';
            UPDATE public."Communities" SET "Name" = 'Sechelt (Part) - qathet'
            WHERE "Name" = 'Sechelt (Part)' AND "RegionalDistrictCode" = '7';

            -- Hyphenate Columbia-Shuswap A-G (RD 18) to match the corrected reference data.
            UPDATE public."Communities" SET "Name" = 'Columbia-Shuswap A' WHERE "Name" = 'Columbia Shuswap A' AND "RegionalDistrictCode" = '18';
            UPDATE public."Communities" SET "Name" = 'Columbia-Shuswap B' WHERE "Name" = 'Columbia Shuswap B' AND "RegionalDistrictCode" = '18';
            UPDATE public."Communities" SET "Name" = 'Columbia-Shuswap C' WHERE "Name" = 'Columbia Shuswap C' AND "RegionalDistrictCode" = '18';
            UPDATE public."Communities" SET "Name" = 'Columbia-Shuswap D' WHERE "Name" = 'Columbia Shuswap D' AND "RegionalDistrictCode" = '18';
            UPDATE public."Communities" SET "Name" = 'Columbia-Shuswap E' WHERE "Name" = 'Columbia Shuswap E' AND "RegionalDistrictCode" = '18';
            UPDATE public."Communities" SET "Name" = 'Columbia-Shuswap F' WHERE "Name" = 'Columbia Shuswap F' AND "RegionalDistrictCode" = '18';
            UPDATE public."Communities" SET "Name" = 'Columbia-Shuswap G' WHERE "Name" = 'Columbia Shuswap G' AND "RegionalDistrictCode" = '18';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE public."Communities" SET "Name" = 'Esquimalt'
            WHERE "Name" IN ('Esquimalt - District municipality', 'Esquimalt - Indian reserve') AND "RegionalDistrictCode" = '1';

            UPDATE public."Communities" SET "Name" = 'Langley'
            WHERE "Name" IN ('Langley - District municipality', 'Langley - City') AND "RegionalDistrictCode" = '11';

            UPDATE public."Communities" SET "Name" = 'North Vancouver'
            WHERE "Name" IN ('North Vancouver - District municipality', 'North Vancouver - City') AND "RegionalDistrictCode" = '11';

            UPDATE public."Communities" SET "Name" = 'Alert Bay'
            WHERE "Name" IN ('Alert Bay - Village', 'Alert Bay - Indian reserve') AND "RegionalDistrictCode" = '8';

            UPDATE public."Communities" SET "Name" = 'Okanagan (Part) 1'
            WHERE "Name" IN ('Okanagan (Part) 1 - North Okanagan', 'Okanagan (Part) 1 - Thompson/Okanagan');

            UPDATE public."Communities" SET "Name" = 'Sechelt (Part)'
            WHERE "Name" IN ('Sechelt (Part) - Sunshine Coast', 'Sechelt (Part) - qathet');

            UPDATE public."Communities" SET "Name" = 'Columbia Shuswap A' WHERE "Name" = 'Columbia-Shuswap A' AND "RegionalDistrictCode" = '18';
            UPDATE public."Communities" SET "Name" = 'Columbia Shuswap B' WHERE "Name" = 'Columbia-Shuswap B' AND "RegionalDistrictCode" = '18';
            UPDATE public."Communities" SET "Name" = 'Columbia Shuswap C' WHERE "Name" = 'Columbia-Shuswap C' AND "RegionalDistrictCode" = '18';
            UPDATE public."Communities" SET "Name" = 'Columbia Shuswap D' WHERE "Name" = 'Columbia-Shuswap D' AND "RegionalDistrictCode" = '18';
            UPDATE public."Communities" SET "Name" = 'Columbia Shuswap E' WHERE "Name" = 'Columbia-Shuswap E' AND "RegionalDistrictCode" = '18';
            UPDATE public."Communities" SET "Name" = 'Columbia Shuswap F' WHERE "Name" = 'Columbia-Shuswap F' AND "RegionalDistrictCode" = '18';
            UPDATE public."Communities" SET "Name" = 'Columbia Shuswap G' WHERE "Name" = 'Columbia-Shuswap G' AND "RegionalDistrictCode" = '18';
            """);
    }
}
