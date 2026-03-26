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

# Define constants
CONTENT_TYPE = "application/json"

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
        # Reluctant quantifier is intentional: match shortest content up to next section header or end
        sql_header = re.search(r"### SQL:\s*(.+?)(?:\n###|\Z)", text, re.DOTALL) # NOSONAR
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
    
    def fingerprint_results(self, sql: str, db_id: int, tenant_id: Optional[str] = None) -> Tuple[str, Tuple[str, ...], str]:
        """
        Create a fingerprint of SQL results for comparison.

        Returns:
            Tuple of (row_count, column_names, hash_of_first_5_rows)
        """
        data = self.metabase.execute_sql(sql, db_id, tenant_id=tenant_id)
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
                "Content-Type": CONTENT_TYPE
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
                "Content-Type": CONTENT_TYPE
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
                    past_questions: List[Dict], is_retry: bool = False,
                    retry_error_type: Optional[str] = None,
                    retry_error_detail: Optional[str] = None) -> str:
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

        retry_context = ""
        if is_retry:
            error_descriptions = {
                "rate_limit": "the previous request was rejected due to a rate limit on the AI service",
                "connection_error": "the previous request failed due to a connection error",
                "server_error": "the previous request failed due to a server error",
                "ai_failure": "the previous attempt failed to produce a valid SQL query",
            }
            error_reason = error_descriptions.get(
                retry_error_type or "", "an error occurred during the previous attempt"
            )
            # For service-level errors the model never ran, so SQL-specific guidance is not relevant
            service_errors = {"rate_limit", "connection_error"}
            if retry_error_type in service_errors:
                retry_context = f"Previous attempt failed: {error_reason}. Please regenerate the query. "
            else:
                retry_context = (
                    f"Previous attempt failed.\n"
                    f"Error type: {retry_error_type or 'unknown'}\n"
                )
                if retry_error_detail:
                    safe_detail = retry_error_detail.replace('"', "'")
                    retry_context += f'Validation error: "{safe_detail}"\n'
                retry_context += (
                    "Avoid repeating the same join path or table choice. "
                    "Fix the issue and generate a corrected SQL query. "
                )

        prompt = (
            f"{f'{newline}{newline}'.join(examples)}{newline}{newline}"
            f"### Schema:{newline}{schemas}{newline}"
            f"### Question:{newline}"
            f"The current date is {dt.datetime.now().strftime('%Y-%m-%d')}. "
            f"{past_context}"
            f"{retry_context}"
            f"Please generate sql and metadata for the following question, "
            f"with reasoning but no explanation. "
            f"Please enable map option only for questions involving regional districts: "
            f"{question}{newline}"
            f"### Reasoning:"
        )
        
        return prompt
    
    def _process_completion(self, completion_result, db_id: int,
                           tenant_id: Optional[str] = None,
                           errors: Optional[List[str]] = None) -> Optional[Tuple]:
        """Process a single LLM completion, returning (fingerprint, sql, metadata) or None."""
        if not completion_result:
            return None

        raw, _usage = completion_result
        logger.debug(f"Raw completion:\n{raw}")

        sql = self.extract_sql(raw)
        if not sql:
            logger.debug("No SQL found in completion")
            return None

        metadata = self.extract_metadata(raw)
        if not metadata:
            logger.debug("No metadata found in completion")
            return None

        # Validate SQL
        is_valid, error = self.metabase.validate_sql(sql, db_id, tenant_id=tenant_id)
        if not is_valid:
            logger.warning(f"SQL validation failed: {error}\nFor sql: {sql}")
            if errors is not None:
                errors.append(error)
            return None

        # Generate fingerprint
        try:
            fingerprint = self.fingerprint_results(sql, db_id, tenant_id=tenant_id)
            return (fingerprint, sql, metadata)
        except Exception as e:
            logger.error(f"Error generating fingerprint: {e}", exc_info=True)
            return None

    def _aggregate_token_usage(self, completions) -> Dict[str, int]:
        """Sum token usage across all completions."""
        total_prompt = 0
        total_completion = 0
        total = 0
        for result in completions:
            if not result:
                continue

            # Unpack the tuple (text, usage)
            raw, usage = result
            logger.debug(f"Raw completion:\n{raw}")

            # Aggregate tokens
            total_prompt += usage.get('prompt_tokens', 0)
            total_completion += usage.get('completion_tokens', 0)
            total += usage.get('total_tokens', 0)
        return {
            "prompt_tokens": total_prompt,
            "completion_tokens": total_completion,
            "total_tokens": total
        }

    def _select_best_candidate(self, candidates: List[Tuple]) -> Tuple[str, Dict]:
        """Pick the majority-vote winner or fall back to the first candidate."""
        fingerprints = [fp for fp, _, _ in candidates]
        winner_fp = self.find_majority(fingerprints)

        if winner_fp:
            # Return the first candidate with winning fingerprint
            for fp, sql, metadata in candidates:
                if fp == winner_fp:
                    logger.info(f"Majority vote winner: {sql[:100]}...")
                    return sql, metadata

        # Fallback to first valid candidate
        logger.info("No majority, using first candidate")
        return candidates[0][1], candidates[0][2]

    async def generate_sql(self, question: str, past_questions: List[Dict],
                          db_id: int, tenant_id: Optional[str] = None,
                          is_retry: bool = False, retry_error_type: Optional[str] = None,
                          retry_error_detail: Optional[str] = None) -> Tuple[Optional[str], Optional[Dict], Optional[Dict], Optional[str]]:
        """
        Generate SQL from natural language question using majority voting.

        Args:
            question: Natural language question
            past_questions: List of past questions and SQL
            db_id: Database ID
            tenant_id: Optional tenant ID for tenant-specific Metabase API key

        Returns:
            Tuple of (sql, metadata, token_usage) or (None, None, None) if generation fails
            where token_usage contains prompt_tokens, completion_tokens, total_tokens
        """

        # Check for hardcoded examples first (can be removed in production)
        hardcoded = self._check_hardcoded_examples(question)
        if hardcoded:
            # Hardcoded examples have no token usage
            sql, metadata = hardcoded
            return sql, metadata, {"prompt_tokens": 0, "completion_tokens": 0, "total_tokens": 0}, None

        # Get relevant schemas
        schemas = self.embeddings.get_formatted_schemas(question, db_id)

        # Generate multiple completions in parallel
        async with aiohttp.ClientSession() as session:
            parsed_schema = await self.fetch_completion(f'''Please parse this schema to return only tables and columns relevant to the users question. Never add to the schema, only remove as necessary.
                                  <question>{question}</question>
                                  <schema>{schemas}</schema>
                                  In the case that the question is NSFW or completely unrelated please return NSFW''', session, 0)

            if not parsed_schema:
                logger.error("Schema parsing failed — no completion returned")
                return None, None, None, None

            print("Schema:", schemas)
            print("Parsed Schema:", parsed_schema[0])

            if "NSFW" in parsed_schema[0].upper():
                logger.error("Error: NSFW or irrelevant question.", exc_info=True)
                return None, None, None, None

            # Build prompt
            prompt = self.build_prompt(question, parsed_schema[0], past_questions, is_retry=is_retry, retry_error_type=retry_error_type, retry_error_detail=retry_error_detail)
            logger.debug(f"Prompt: {prompt[:200]}...")
            tasks = [
                self.fetch_completion(prompt, session, i)
                for i in range(self.config.k_samples)
            ]
            completions = await asyncio.gather(*tasks)

        # Aggregate token usage from all completions
        token_usage = self._aggregate_token_usage(completions)

        # Process completions and extract valid candidates, collecting validation errors
        validation_errors: List[str] = []
        candidates = [
            c for completion_result in completions
            if (c := self._process_completion(completion_result, db_id, tenant_id=tenant_id, errors=validation_errors)) is not None
        ]

        # Join top 2 errors, truncate to keep prompt focused
        MAX_ERROR_DETAIL_LENGTH = 200
        combined_error = "; ".join(validation_errors[:2]) if validation_errors else None
        error_detail = combined_error[:MAX_ERROR_DETAIL_LENGTH] if combined_error else None

        if not candidates:
            logger.warning(f"No valid candidates generated. Error detail: {error_detail}")
            return None, None, token_usage, error_detail

        # Majority voting on fingerprints
        sql, metadata = self._select_best_candidate(candidates)
        return sql, metadata, token_usage, None
    
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
            "For indigenous organizations only": (
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
                    "Content-Type": CONTENT_TYPE
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
                    "Content-Type": CONTENT_TYPE
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