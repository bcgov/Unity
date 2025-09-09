import os
import json
import dotenv
import textwrap
import requests

dotenv.load_dotenv()
headers = {"x-api-key": os.getenv("METABASE_KEY")}

def get_sql(sql, db_id, metabase_url):
    ds_req = {
        "database": db_id,
        "type": "native",
        "native": {"query": sql},
    }
    r = requests.post(
        f"{metabase_url}/api/dataset",
        headers=headers,
        json=ds_req,
    )
    r.raise_for_status()
    return r.json()["data"]

def get_worksheets():
    sql = f'SELECT "Worksheets"."Name", "Worksheets"."Id" FROM "Flex"."Worksheets"'
    data = get_sql(sql, 3, os.getenv("MB_EMBED_URL"))
    return [{"name": r[0], "id": r[1]} for r in data["rows"]]

def get_worksheet_instances(worksheetId):
    sql = f'SELECT "WorksheetInstances"."CurrentValue", "WorksheetInstances"."WorksheetCorrelationId" FROM "Flex"."WorksheetInstances" WHERE "WorksheetInstances"."WorksheetId" = \'{worksheetId}\''
    data = get_sql(sql, 3, os.getenv("MB_EMBED_URL"))
    return data

def get_custom_labels():
    all_data = {}
    batch_size = 500
    offset = 0

    while True:
        sql = f'''
        SELECT "CustomFields"."Label", "CustomFields"."Key"
        FROM "Flex"."CustomFields"
        ORDER BY "CustomFields"."Key"
        LIMIT {batch_size} OFFSET {offset}
        '''
        rows = get_sql(sql, 3, os.getenv("MB_URL"))["rows"]

        if not rows:
            break

        all_data.update({r[1]: r[0] for r in rows})
        offset += batch_size

    return all_data

def get_parsed_worksheets():
    parsed = []
    worksheets = get_worksheets()
    custom_labels = get_custom_labels()
    for w in worksheets:
        instance = get_worksheet_instances(w["id"])
        if len(instance["rows"]) > 0 and "values" in json.loads(instance["rows"][0][0]):
            rows = instance["rows"]
            keys = [j["key"] for j in json.loads(rows[0][0])["values"]]

            samples = []
            for row in rows[:3]:
                keys = [j["key"] for j in json.loads(row[0])["values"]]
                values = [j["value"] for j in json.loads(row[0])["values"]]
                samples.append({custom_labels[keys[i]]: values[i][:300] for i in range(len(keys))})

            parsed_string = f'''### {w["name"]}
**Columns**
{"\n".join(["- **" + custom_labels[k] + "** type/Text" for k in keys])}


**Relations**
_(none)_

{"**Samples** (up to 3 rows)\n" + textwrap.indent(
    json.dumps(samples, indent=2, default=str),
    "    ")}
        '''
            parsed.append(parsed_string)
            break
    return parsed


def get_column_example(table, column):
    sql = f"SELECT \"{column}\" FROM \"Reporting\".\"{table}\" WHERE \"{column}\" IS NOT null and \"{column}\" <> ''"
    instance = get_sql(sql, 3, os.getenv("MB_EMBED_URL"))
    try:
        return instance["rows"][0][0]
    except:
        return None

def get_views_schemas():
    schema = requests.get(
        f"{os.getenv('MB_EMBED_URL')}/api/database/{os.getenv('MB_EMBED_ID')}/metadata",
        headers=headers
    ).json()

    junk_cols = {
        "CreatorId", "LastModificationTime", "LastModifierId",
        "ExtraProperties", "ConcurrencyStamp", "CreationTime",
    }

    docs = []

    for tbl in schema["tables"]:
        if tbl["schema"] != "Reporting" or "Worksheet" not in tbl['name']:
            continue

        cols = [
            f"{c['name']} ({c['base_type']})"
            for c in tbl["fields"]
            if c["name"] not in junk_cols
        ]

        # Find out if there are non-blank rows 
        sql = f"SELECT * FROM \"Reporting\".\"{tbl['name']}\" "
        instance = get_sql(sql, 3, os.getenv("MB_EMBED_URL"))
        rows = [r for r in instance["rows"] if set(r[3:]) != set([''])]
        if len(rows) > 0:

            # page = f"# Reporting: {tbl['name']}({', '.join(cols)})"
            page = f"# Reporting: {tbl['name']}"
            for c in cols:
                example = get_column_example(tbl['name'], c.split(' ')[0])
                if example is not None:
                    page += f"\n - {c}: '{example[:50]}{'...' if len(example) > 50 else ''}'"
            docs.append(page)
            print(page)

    return docs

get_views_schemas()
# get_column_example("Worksheet-cpworksheet-v1", "field2")

'''
**Extraction pattern (PostgreSQL)**
```sql
SELECT  wi."Id" AS WorksheetInstanceId,
        {"\n".join(["MAX(CASE WHEN elem ->> 'key' = '" + k + "' THEN elem ->> 'value' END) AS \"" + k + "\"," for k in keys])}
FROM    Flex."WorksheetInstances"  wi
CROSS   JOIN LATERAL jsonb_array_elements(wi."CurrentValue" -> 'values') elem
GROUP BY wi."Id"

Note: in worksheetinstances table: WorksheetCorrelationId, WorksheetId, ID
Virtual: Name: customLabel1, customLabel2,.. applicationId 

SELECT count(customLabel1) FROM public."Name" WHERE customLabel2 = 'field2'

SELECT COUNT(t."customLabel1")
FROM (
  SELECT
    wi."Id",
    MAX(elem->>'value') FILTER (WHERE elem->>'key' = 'customLabel1') AS "customLabel1",
    MAX(elem->>'value') FILTER (WHERE elem->>'key' = 'customLabel2') AS "customLabel2"
  FROM "Flex"."WorksheetInstances" wi
  CROSS JOIN LATERAL jsonb_array_elements(wi."CurrentValue"->'values') elem
  GROUP BY wi."Id"
) AS t
WHERE t."customLabel2" = 'field2';

See for example:
WITH applicant AS (
  SELECT
      wi."Id" AS "WorksheetInstanceId",
      MAX(CASE WHEN elem->>'key' = 'leadApplicant' THEN elem->>'value' END)           AS "leadApplicant",
      MAX(CASE WHEN elem->>'key' = 'projectIntendedOutcomes' THEN elem->>'value' END) AS "projectIntendedOutcomes"
  FROM "Flex"."WorksheetInstances" AS wi
  CROSS JOIN LATERAL jsonb_array_elements(wi."CurrentValue"->'values') AS elem
  GROUP BY wi."Id"
)
SELECT
    a."projectIntendedOutcomes",
    app."SubmissionDate"
FROM "Flex"."WorksheetInstances" AS wi
JOIN applicant AS a
  ON wi."Id" = a."WorksheetInstanceId"
JOIN "public"."Applications" AS app
  ON app."Id" = wi."CorrelationId"
WHERE a."leadApplicant" = 'Local Government';


May be impossible to programmatically convert virtual table to required SQL 
Talk to Andre about this, maybe create views/tables for each worksheet?

'''
