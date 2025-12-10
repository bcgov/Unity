"""
SQL generation module for natural language to SQL conversion.
Uses LLM with majority voting for robust SQL generation.
"""
import re
import json
import hashlib
import asyncio
import aiohttp
import tiktoken
import datetime as dt
import logging
from typing import Dict, Any, List, Optional, Tuple
from collections import Counter
from config import config
from embeddings import embedding_manager
from metabase import metabase_client
import time

# Configure logging
logger = logging.getLogger(__name__)


class SQLGenerator:
    """Generates SQL from natural language queries"""
    
    def __init__(self):
        self.config = config.ai
        self.metabase = metabase_client
        self.embeddings = embedding_manager
        self.tokenizer = tiktoken.encoding_for_model("gpt-4o-mini")
        
        # Regex patterns for extraction
        self.sql_pattern = re.compile(r"```sql\s*(.+?)```", re.I | re.S)
        self.metadata_pattern = re.compile(
            r"""\#\#\#\s*Metadata:\s*
                (?:```json\s*)?
                (\{.*?})
                (?:\s*```)?
            """,
            re.IGNORECASE | re.DOTALL | re.VERBOSE
        )
    
    def extract_sql(self, text: str) -> Optional[str]:
        """Extract SQL from LLM response"""
        # Try code fence first
        match = self.sql_pattern.search(text)
        if match:
            return match.group(1).strip()
        
        # Fallback: look for '### SQL:' header
        sql_header = re.search(r"### SQL:\s*([\s\S]+?)(?:\n###|\Z)", text)
        if sql_header:
            return sql_header.group(1).strip()
        
        return None
    
    def extract_metadata(self, text: str) -> Optional[Dict[str, Any]]:
        """Extract metadata from LLM response"""
        match = self.metadata_pattern.search(text)
        if not match:
            return None
        
        raw = match.group(1).strip()
        try:
            return json.loads(raw)
        except json.JSONDecodeError:
            return None
    
    def fingerprint_results(self, sql: str, db_id: int) -> Tuple[str, Tuple[str, ...], str]:
        """
        Create a fingerprint of SQL results for comparison.
        
        Returns:
            Tuple of (row_count, column_names, hash_of_first_5_rows)
        """
        data = self.metabase.execute_sql(sql, db_id)
        rows = data["rows"]
        cols = tuple(
            c["name"] if isinstance(c, dict) else c
            for c in data["cols"]
        )
        head = rows[:5]
        digest = hashlib.md5(json.dumps(head, default=str).encode()).hexdigest()
        return str(len(rows)), cols, digest
    
    def find_majority(self, items: List) -> Optional[Any]:
        """Find the most common item if it appears more than once"""
        if not items:
            return None
        counts = Counter(items)
        winner, freq = counts.most_common(1)[0]
        return winner if freq > 1 else None
    
    async def fetch_completion(self, prompt: str, session: aiohttp.ClientSession,
                              index: int) -> Optional[Tuple[str, Dict[str, int]]]:
        """Fetch a single completion from the LLM

        Returns:
            Tuple of (completion_text, usage_dict) where usage_dict contains
            prompt_tokens, completion_tokens, and total_tokens
        """
        logger.debug(f"[{index}] Tokens in prompt: {len(self.tokenizer.encode(prompt))}")

        if self.config.use_azure:
            # Use Azure OpenAI
            headers = {
                "api-key": self.config.azure_api_key,
                "Content-Type": "application/json"
            }
            
            endpoint = f"{self.config.azure_endpoint}/openai/deployments/{self.config.azure_deployment}/chat/completions?api-version={self.config.azure_api_version}"
            
            json_data = {
                "messages": [
                    {"role": "system", "content": "You are a professional SQL programmer."},
                    {"role": "user", "content": prompt}
                ],
                "temperature": self.config.temperature
            }
        else:
            # Use standard OpenAI
            headers = {
                "Authorization": f"Bearer {self.config.completion_key}",
                "Content-Type": "application/json"
            }
            
            endpoint = self.config.completion_endpoint
            
            json_data = {
                "model": self.config.model,
                "messages": [
                    {"role": "system", "content": "You are a professional SQL programmer."},
                    {"role": "user", "content": prompt}
                ],
                "temperature": self.config.temperature
            }
        
        async with session.post(
            endpoint,
            headers=headers,
            json=json_data
        ) as response:
            if response.status != 200:
                logger.error(f"[{index}] Error: {response.status}")
                logger.error(await response.text())
                return None

            data = await response.json()
            usage = data.get('usage', {})
            logger.debug(f"[{index}] Tokens used: {usage.get('total_tokens', 0)}")
            return data["choices"][0]["message"]["content"], usage
    
    def load_examples(self) -> List[str]:
        """Load example queries for few-shot prompting"""
        try:
            with open("QDECOMP_examples.json", "r") as file:
                examples = json.load(file)
            
            formatted = []
            for ex in examples:
                newline = '\n'
                metadata_dict = {
                    'title': ex['title'],
                    'x_axis': ex['x_axis'], 
                    'y_axis': ex['y_axis'],
                    'visualization_options': ex['visualization_options']
                }
                formatted.append(
                    f"### Schema:{newline}{newline.join(ex['Schema'])}{newline}"
                    f"### Question:{newline}{ex['Question']}{newline}"
                    f"### Reasoning:{newline}{ex['Reasoning']}{newline}"
                    f"### SQL:{newline}{ex['SQL']}{newline}"
                    f"### Metadata:{newline}{json.dumps(metadata_dict)}"
                )
            return formatted
        except FileNotFoundError:
            logger.warning("QDECOMP_examples.json not found, using empty examples")
            return []
    
    def build_prompt(self, question: str, schemas: str, 
                    past_questions: List[Dict]) -> str:
        """Build the prompt for SQL generation"""
        examples = self.load_examples()
        newline = '\n'
        
        # Add past question context if available
        past_context = ""
        if past_questions and len(past_questions) > 1:
            last_q = past_questions[-2]
            past_context = (
                f'Note that the previous question in this conversation was: '
                f'"{last_q["question"]}" and the generated SQL was: "{last_q["SQL"]}". '
            )
        
        prompt = (
            f"{f'{newline}{newline}'.join(examples)}{newline}{newline}"
            f"### Schema:{newline}{schemas}{newline}"
            f"### Question:{newline}"
            f"The current date is {dt.datetime.now().strftime('%Y-%m-%d')}. "
            f"{past_context}"
            f"Please generate sql and metadata for the following question, "
            f"with reasoning but no explanation. "
            f"Please enable map option only for questions involving regional districts: "
            f"{question}{newline}"
            f"### Reasoning:"
        )
        
        return prompt
    
    async def generate_sql(self, question: str, past_questions: List[Dict],
                          db_id: int) -> Tuple[Optional[str], Optional[Dict], Optional[Dict]]:
        """
        Generate SQL from natural language question using majority voting.

        Args:
            question: Natural language question
            past_questions: List of past questions and SQL
            db_id: Database ID

        Returns:
            Tuple of (sql, metadata, token_usage) or (None, None, None) if generation fails
            where token_usage contains prompt_tokens, completion_tokens, total_tokens
        """

        # Check for hardcoded examples first (can be removed in production)
        hardcoded = self._check_hardcoded_examples(question)
        if hardcoded:
            # Hardcoded examples have no token usage
            sql, metadata = hardcoded
            return sql, metadata, {"prompt_tokens": 0, "completion_tokens": 0, "total_tokens": 0}
        
        # Get relevant schemas
        schemas = self.embeddings.get_formatted_schemas(question, db_id)

        # Generate multiple completions in parallel
        async with aiohttp.ClientSession() as session:
            parsed_schema = await self.fetch_completion(f'''Please parse this schema to return only tables and columns relevant to the users question. Never add to the schema, only remove as necessary.
                                  <question>{question}</question>
                                  <schema>{schemas}</schema>
                                  In the case that the question is NSFW or completely unrelated please return NSFW''', session, 0)

            print("Schema:", schemas)
            print("Parsed Schema:", parsed_schema[0])

            if parsed_schema[0] == "NSFW":
                logger.error(f"Error: NSFW or irrelevant question.", exc_info=True)
                return None, None, None

            # Build prompt
            prompt = self.build_prompt(question, parsed_schema[0], past_questions)
            logger.debug(f"Prompt: {prompt[:200]}...")
            tasks = [
                self.fetch_completion(prompt, session, i)
                for i in range(self.config.k_samples)
            ]
            completions = await asyncio.gather(*tasks)

        # Aggregate token usage from all completions
        total_prompt_tokens = 0
        total_completion_tokens = 0
        total_tokens = 0

        # Process completions and extract valid candidates
        candidates = []
        for completion_result in completions:
            if not completion_result:
                continue

            # Unpack the tuple (text, usage)
            raw, usage = completion_result
            logger.debug(f"Raw completion:\n{raw}")

            # Aggregate tokens
            total_prompt_tokens += usage.get('prompt_tokens', 0)
            total_completion_tokens += usage.get('completion_tokens', 0)
            total_tokens += usage.get('total_tokens', 0)

            sql = self.extract_sql(raw)
            if not sql:
                logger.debug("No SQL found in completion")
                continue

            metadata = self.extract_metadata(raw)
            if not metadata:
                logger.debug("No metadata found in completion")
                continue
            
            # Validate SQL
            is_valid, error = self.metabase.validate_sql(sql, db_id)
            if not is_valid:
                logger.warning(f"SQL validation failed: {error}\nFor sql: {sql}")
                continue

            # Generate fingerprint
            try:
                fingerprint = self.fingerprint_results(sql, db_id)
                candidates.append((fingerprint, sql, metadata))
            except Exception as e:
                logger.error(f"Error generating fingerprint: {e}", exc_info=True)
                continue
        
        # Prepare token usage dict
        token_usage = {
            "prompt_tokens": total_prompt_tokens,
            "completion_tokens": total_completion_tokens,
            "total_tokens": total_tokens
        }

        # Majority voting on fingerprints
        if candidates:
            fingerprints = [fp for fp, _, _ in candidates]
            winner_fp = self.find_majority(fingerprints)

            if winner_fp:
                # Return the first candidate with winning fingerprint
                for fp, sql, metadata in candidates:
                    if fp == winner_fp:
                        logger.info(f"Majority vote winner: {sql[:100]}...")
                        return sql, metadata, token_usage

            # Fallback to first valid candidate
            logger.info("No majority, using first candidate")
            return candidates[0][1], candidates[0][2], token_usage

        logger.warning("No valid candidates generated")
        return None, None, token_usage
    
    def _check_hardcoded_examples(self, question: str) -> Optional[Tuple[str, Dict]]:
        """Check for hardcoded example queries (for demo/testing)"""
        examples = {
            "How many applications were approved in each subsector?": (
                '''SELECT COALESCE(applicants."SubSector", 'Unspecified') AS SubSector, 
COUNT(*) AS TotalApplications
FROM "public"."Applications" AS applications
JOIN "public"."Applicants" AS applicants ON applications."ApplicantId" = applicants."Id"
WHERE applicants."SubSector" IS NOT NULL 
AND applicants."SubSector" != '' 
AND LOWER(applicants."SubSector") != 'other'
GROUP BY applicants."SubSector"
ORDER BY TotalApplications DESC 
LIMIT 15;''',
                {
                    "title": "Approved Applications Per Subsector",
                    "x_axis": ['SubSector'],
                    "y_axis": ['TotalApplications'],
                    "visualization_options": ["bar", "pie"]
                }
            ),
            "Total applications and distributed funding per month in 2024": (
                '''SELECT 
    EXTRACT(MONTH FROM applications."SubmissionDate") AS month, 
    COUNT(DISTINCT applicants."Id") AS total_applicants, 
    SUM(applications."ApprovedAmount") AS total_approved_funding
FROM 
    "public"."Applications" AS applications
JOIN 
    "public"."Applicants" AS applicants ON applications."ApplicantId" = applicants."Id"
WHERE 
    EXTRACT(MONTH FROM applications."SubmissionDate") IS NOT NULL
GROUP BY 
    month;''',
                {
                    "title": "Total Applicants and Approved Funding Per Month in 2024",
                    "x_axis": ["month"],
                    "y_axis": ["total_applicants", "total_approved_funding"],
                    "visualization_options": ["bar", "line"]
                }
            ),
            "Distribution of funding by regional district": (
                '''SELECT "public"."Applications"."RegionalDistrict" AS "RegionalDistrict",
SUM("public"."Applications"."ApprovedAmount") AS "sum"
FROM
"public"."Applications"
LEFT JOIN "public"."ApplicationStatuses" AS "ApplicationStatuses - ApplicationStatusId" ON "public"."Applications"."ApplicationStatusId" = "ApplicationStatuses - ApplicationStatusId"."Id"
WHERE
"ApplicationStatuses - ApplicationStatusId"."ExternalStatus" = 'Approved'
AND
"public"."Applications"."RegionalDistrict" IS NOT NULL
AND
"public"."Applications"."RegionalDistrict" != '' 
GROUP BY
"public"."Applications"."RegionalDistrict"
ORDER BY
"public"."Applications"."RegionalDistrict" ASC''',
                {
                    "title": "Approved Amount per Regional District",
                    "x_axis": ["RegionalDistrict"],
                    "y_axis": ["sum"],
                    "visualization_options": ["bar", "pie", "map"]
                }
            ),
            "Only 2024 Q3": (
                '''SELECT "public"."Applications"."RegionalDistrict" AS "RegionalDistrict",
SUM("public"."Applications"."ApprovedAmount") AS "sum"
FROM
"public"."Applications"

LEFT JOIN "public"."ApplicationStatuses" AS "ApplicationStatuses - ApplicationStatusId" ON "public"."Applications"."ApplicationStatusId" = "ApplicationStatuses - ApplicationStatusId"."Id"
WHERE
"ApplicationStatuses - ApplicationStatusId"."ExternalStatus" = 'Approved'
AND
"public"."Applications"."RegionalDistrict" IS NOT NULL
AND
"public"."Applications"."RegionalDistrict" != ''
AND "public"."Applications"."SubmissionDate" >= '2024-07-01'
AND "public"."Applications"."SubmissionDate" <= '2024-09-30'    
GROUP BY
"public"."Applications"."RegionalDistrict"
ORDER BY
"public"."Applications"."RegionalDistrict" ASC''',
                {
                    "title": "Approved Amount per Regional District - 2024 Q3",
                    "x_axis": ["RegionalDistrict"],
                    "y_axis": ["sum"],
                    "visualization_options": ["bar", "pie", "map"]
                }
            ),
            "Show results from last quarter": (
                '''SELECT a."RegionalDistrict", SUM(a."ApprovedAmount") AS total_approved
FROM "public"."Applications" AS a
JOIN "public"."Applicants" AS ap ON a."ApplicantId" = ap."Id"
LEFT JOIN "public"."ApplicationStatuses" AS s ON a."ApplicationStatusId" = s."Id"
WHERE s."ExternalStatus" = 'Approved'
AND ap."IndigenousOrgInd" = 'Yes'
AND a."SubmissionDate" >= DATE_TRUNC('quarter', CURRENT_DATE) - INTERVAL '3 months'
AND a."SubmissionDate" < DATE_TRUNC('quarter', CURRENT_DATE)
GROUP BY a."RegionalDistrict"
ORDER BY a."RegionalDistrict" ASC;''',
                {
                    "title": "Total Approved Amount by Indigenous Organizations from Last Quarter",
                    "x_axis": ["RegionalDistrict"],
                    "y_axis": ["sum"],
                    "visualization_options": ["bar", "pie", "map"]
                }
            ),
            "For indegenous organizations only": (
                '''SELECT "public"."Applications"."RegionalDistrict" AS "RegionalDistrict",
SUM("public"."Applications"."ApprovedAmount") AS "sum"
FROM
"public"."Applications"
JOIN 
    "public"."Applicants" AS ap ON "public"."Applications"."ApplicantId" = ap."Id"

LEFT JOIN "public"."ApplicationStatuses" AS "ApplicationStatuses - ApplicationStatusId" ON "public"."Applications"."ApplicationStatusId" = "ApplicationStatuses - ApplicationStatusId"."Id"
WHERE
"ApplicationStatuses - ApplicationStatusId"."ExternalStatus" = 'Approved'
AND
"public"."Applications"."RegionalDistrict" IS NOT NULL
AND
"public"."Applications"."RegionalDistrict" != ''
AND
ap."IndigenousOrgInd" = 'Yes'   
GROUP BY
"public"."Applications"."RegionalDistrict"
ORDER BY
"public"."Applications"."RegionalDistrict" ASC''',
                {
                    "title": "Approved Amount per Regional District - Indigenous Org's",
                    "x_axis": ["RegionalDistrict"],
                    "y_axis": ["sum"],
                    "visualization_options": ["bar", "pie", "map"]
                }
            )
        }
        
        if question in examples:
            time.sleep(2)
            return examples[question]
        return None
    
    async def explain_sql(self, sql: str) -> Tuple[str, Dict[str, int]]:
        """
        Generate a concise explanation of the given SQL query.

        Args:
            sql: The SQL query to explain

        Returns:
            Tuple of (explanation, token_usage) where token_usage contains
            prompt_tokens, completion_tokens, and total_tokens
        """
        try:
            prompt = f"""Please provide an extremely succinct explanation of this report you created. Start with "I've...":

{sql}"""
            
            if self.config.use_azure:
                # Use Azure OpenAI
                headers = {
                    "api-key": self.config.azure_api_key,
                    "Content-Type": "application/json"
                }
                
                endpoint = f"{self.config.azure_endpoint}/openai/deployments/{self.config.azure_deployment}/chat/completions?api-version={self.config.azure_api_version}"
                
                json_data = {
                    "messages": [
                        {"role": "system", "content": "You are a helpful assistant that explains SQL queries in simple terms."},
                        {"role": "user", "content": prompt}
                    ],
                    "temperature": 0.3
                }
            else:
                # Use standard OpenAI
                headers = {
                    "Authorization": f"Bearer {self.config.completion_key}",
                    "Content-Type": "application/json"
                }
                
                endpoint = self.config.completion_endpoint
                
                json_data = {
                    "model": "gpt-4o-mini",
                    "messages": [
                        {"role": "system", "content": "You are a helpful assistant that explains SQL queries in simple terms."},
                        {"role": "user", "content": prompt}
                    ],
                    "temperature": 0.3
                }
            
            async with aiohttp.ClientSession() as session:
                async with session.post(
                    endpoint,
                    headers=headers,
                    json=json_data
                ) as response:
                    if response.status != 200:
                        logger.error(f"Error explaining SQL: {response.status}")
                        return "This query retrieves and analyzes your data.", {"prompt_tokens": 0, "completion_tokens": 0, "total_tokens": 0}

                    data = await response.json()
                    explanation = data["choices"][0]["message"]["content"].strip()
                    usage = data.get('usage', {})
                    return explanation, usage

        except Exception as e:
            logger.error(f"Error generating SQL explanation: {e}", exc_info=True)
            return "This query retrieves and analyzes your data.", {"prompt_tokens": 0, "completion_tokens": 0, "total_tokens": 0}


# Global SQL generator instance
sql_generator = SQLGenerator()