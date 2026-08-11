"""
SQL generation module for natural language to SQL conversion.
Uses LLM with majority voting for robust SQL generation.
"""
import re
import json
import hashlib
import asyncio
import tiktoken
import datetime as dt
import logging
from typing import Dict, Any, List, Optional, Tuple
from collections import Counter
from config import config
from embeddings import embedding_manager
from llm_client import build_async_client, chat_completion, usage_to_dict
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
        # Backtrack-free: body is any run of non-backtick chars, or 1-2 backticks
        # not followed by a third (the closing fence). Possessive quantifiers (3.11+).
        self.sql_pattern = re.compile(r"```sql\s*+((?:[^`]++|`(?!``))*+)```", re.I)
        # Only locates where the metadata JSON starts; extract_metadata uses
        # json.JSONDecoder().raw_decode from here so nested objects/arrays in
        # the metadata (e.g. "columns": [{...}]) are parsed correctly instead
        # of truncating at the first inner '}'.
        self.metadata_header_pattern = re.compile(
            r"(?:\#\#\#\s*)?Metadata:\s*(?:```json\s*)?",
            re.IGNORECASE
        )
    
    def extract_sql(self, text: str) -> Optional[str]:
        """Extract SQL from LLM response"""
        # Try code fence first
        match = self.sql_pattern.search(text)
        if match:
            return match.group(1).strip()
        
        # Fallback: look for '### SQL:' or plain 'SQL:' header (model sometimes drops ### at high token counts)
        # Reluctant quantifier is intentional: match shortest content up to next section header or end
        sql_header = re.search(r"^(?:###\s*)?SQL:\s*(.+?)(?:\n(?:###\s*)?(?:Metadata:|Reasoning:)|\Z)", text, re.DOTALL | re.MULTILINE) # NOSONAR
        if sql_header:
            return sql_header.group(1).strip()
        
        return None
    
    def extract_metadata(self, text: str) -> Optional[Dict[str, Any]]:
        """Extract metadata from LLM response"""
        header_match = self.metadata_header_pattern.search(text)
        if not header_match:
            return None

        start = text.find('{', header_match.end())
        if start == -1:
            return None

        try:
            metadata, _ = json.JSONDecoder().raw_decode(text, start)
        except json.JSONDecodeError:
            return None

        return metadata
    
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
        digest = hashlib.md5(json.dumps(head, default=str).encode(), usedforsecurity=False).hexdigest()
        return str(len(rows)), cols, digest
    
    def find_majority(self, items: List) -> Optional[Any]:
        """Find the most common item if it appears more than once"""
        if not items:
            return None
        counts = Counter(items)
        winner, freq = counts.most_common(1)[0]
        return winner if freq > 1 else None
    
    async def fetch_completion(self, prompt: str, client,
                              index: int, system_message: str = "You are a professional SQL programmer.") -> Optional[Tuple[str, Dict[str, int]]]:
        """Fetch a single completion from the LLM.

        Typed SDK errors (RateLimitError, APITimeoutError, APIConnectionError,
        APIStatusError) are NOT caught here — they propagate so the caller can
        classify them (e.g. a 429 becomes ``rate_limit`` rather than being
        silently swallowed). Returns None only when the model returns no content.

        Returns:
            Tuple of (completion_text, usage_dict) where usage_dict contains
            prompt_tokens, completion_tokens, and total_tokens
        """
        logger.debug(f"[{index}] Tokens in prompt: {len(self.tokenizer.encode(prompt))}")

        response = await chat_completion(
            client,
            system_message=system_message,
            user_message=prompt,
            temperature=self.config.temperature,
        )

        usage = usage_to_dict(response.usage)
        logger.debug(f"[{index}] Tokens used: {usage.get('total_tokens', 0)}")

        content = response.choices[0].message.content
        if not content:
            logger.error(f"[{index}] Completion returned no content")
            return None
        return content, usage
    
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
            f"Please generate SQL and metadata for the following question, with reasoning but no explanation.{newline}"
            f"{newline}"
            f"Rules:{newline}"
            f"- The schema context above is divided into sections: PUBLIC TABLES, WORKSHEET VIEWS, and SCORESHEET VIEWS.{newline}"
            f"- Use PUBLIC TABLES for application-level data (applicants, applications, statuses, funding amounts).{newline}"
            f"- Use WORKSHEET VIEWS for program-specific form data and custom applicant fields collected on worksheets.{newline}"
            f"- Use SCORESHEET VIEWS for evaluation, scoring, reviewer assessments, or scorecard data.{newline}"
            f"- Do not mix WORKSHEET VIEWS and SCORESHEET VIEWS in the same query unless explicitly requested; prefer JOINing via a shared applicant or application identifier.{newline}"
            f"- When using columns from WORKSHEET VIEWS or SCORESHEET VIEWS that have type/Text but contain numeric values (e.g. currency amounts), "
            f"always cast them using ::numeric before applying any aggregation (e.g. SUM(\"m2Cost\"::numeric)).{newline}"
            f"- When using type/Text date columns from WORKSHEET VIEWS or SCORESHEET VIEWS, cast them using ::date when filtering or ordering by date.{newline}"
            f"- Enable the map visualization option only for questions involving regional districts.{newline}"
            f"{newline}"
            f"Question: {question}{newline}"
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
            logger.exception(f"Error generating fingerprint: {e}")
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
            _, usage = result

            # Aggregate tokens
            total_prompt += usage.get('prompt_tokens', 0)
            total_completion += usage.get('completion_tokens', 0)
            total += usage.get('total_tokens', 0)
        return {
            "prompt_tokens": total_prompt,
            "completion_tokens": total_completion,
            "total_tokens": total
        }

    def _sum_token_usages(self, usages: List[Dict[str, int]]) -> Dict[str, int]:
        """Sum a list of per-attempt token-usage dicts into one."""
        return {
            "prompt_tokens": sum(u.get("prompt_tokens", 0) for u in usages),
            "completion_tokens": sum(u.get("completion_tokens", 0) for u in usages),
            "total_tokens": sum(u.get("total_tokens", 0) for u in usages),
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

    async def _attempt_generation(
        self, question: str, schemas: str, past_questions: List[Dict],
        client, *, db_id: int, tenant_id: Optional[str],
        is_retry: bool, retry_error_type: Optional[str],
        retry_error_detail: Optional[str], k_samples: int,
    ) -> Tuple[Optional[str], Optional[Dict], Dict[str, int], Optional[str]]:
        """Run one SQL generation attempt: fan out k completions, validate, pick best.

        Iteration-agnostic — the caller owns retry logic. Raises if every
        completion raised (so a 429 from the first attempt can still be
        classified as rate_limit upstream).
        """
        prompt = self.build_prompt(
            question, schemas, past_questions,
            is_retry=is_retry,
            retry_error_type=retry_error_type,
            retry_error_detail=retry_error_detail,
        )
        logger.debug(f"Prompt: {prompt[:200]}...")
        tasks = [self.fetch_completion(prompt, client, i) for i in range(k_samples)]
        # return_exceptions=True so one sample failing doesn't discard the
        # others. If EVERY sample failed, re-raise so the error can be
        # classified upstream (e.g. a 429 → rate_limit).
        results = await asyncio.gather(*tasks, return_exceptions=True)

        completions = []
        first_error: Optional[BaseException] = None
        for result in results:
            if isinstance(result, BaseException):
                first_error = first_error or result
                logger.warning(f"Sample completion failed: {result}")
            else:
                completions.append(result)

        if not completions and first_error is not None:
            raise first_error

        token_usage = self._aggregate_token_usage(completions)

        validation_errors: List[str] = []
        candidates = [
            c for completion_result in completions
            if (c := self._process_completion(completion_result, db_id, tenant_id=tenant_id, errors=validation_errors)) is not None
        ]

        MAX_ERROR_DETAIL_LENGTH = 200
        combined_error = "; ".join(validation_errors[:2]) if validation_errors else None
        error_detail = combined_error[:MAX_ERROR_DETAIL_LENGTH] if combined_error else None

        if not candidates:
            return None, None, token_usage, error_detail

        sql, metadata = self._select_best_candidate(candidates)
        return sql, metadata, token_usage, None

    async def _check_question_relevance(self, question: str, schemas: str, client) -> bool:
        """One-shot RELATED/UNRELATED schema-relevance filter. Logs and
        returns False on UNRELATED, parse-failure, or empty completion."""
        parsed_schema = await self.fetch_completion(
            f'''Your ONLY task is to decide if the question is related to the database schema.
DO NOT generate SQL.
DO NOT explain anything.
DO NOT infer missing information.
Output EXACTLY one word: RELATED or UNRELATED.

<question>{question}</question>
<schema>{schemas}</schema>''',
            client, 0,
            system_message="You are a schema relevance filter. Output only RELATED or UNRELATED."
        )

        if not parsed_schema:
            logger.error("Schema parsing failed — no completion returned")
            return False

        logger.info(f"[RelevanceCheck] Q: {question!r} | Raw: {parsed_schema[0]!r}")

        if parsed_schema[0].strip().upper() != "RELATED":
            logger.warning(f"[RelevanceCheck] UNRELATED | Raw: {parsed_schema[0]!r}")
            return False
        return True

    async def generate_sql(self, question: str, past_questions: List[Dict],
                          db_id: int, tenant_id: Optional[str] = None,
                          is_retry: bool = False, retry_error_type: Optional[str] = None,
                          retry_error_detail: Optional[str] = None) -> Tuple[Optional[str], Optional[Dict], Optional[Dict], Optional[str]]:
        """
        Generate SQL from natural language question using majority voting plus
        an internal self-correction loop: after a failed validation
        the loop regenerates with the error fed back into the prompt, up to
        ``config.ai.max_self_correction_iterations`` attempts.

        Args:
            question: Natural language question
            past_questions: List of past questions and SQL
            db_id: Database ID
            tenant_id: Optional tenant ID for tenant-specific Metabase API key
            is_retry: Caller-driven retry flag — seeds iteration 1's prompt
            retry_error_type: Error type from a caller-driven retry
            retry_error_detail: Validation error from a caller-driven retry,
                fed into iteration 1's prompt

        Returns:
            Tuple of (sql, metadata, token_usage, error_detail). On failure the
            leading elements are None; error_detail carries the most actionable
            validation error (preferred over infra exceptions) when no valid
            candidate could be generated within the iteration limit.
            token_usage aggregates prompt/completion tokens across every
            iteration's LLM calls.
        """

        # Check for hardcoded examples first (can be removed in production)
        hardcoded = self._check_hardcoded_examples(question)
        if hardcoded:
            # Hardcoded examples have no token usage
            sql, metadata = hardcoded
            return sql, metadata, {"prompt_tokens": 0, "completion_tokens": 0, "total_tokens": 0}, None

        # Get relevant schemas
        schemas = self.embeddings.get_formatted_schemas(question, db_id, tenant_id=tenant_id)
        if not schemas:
            logger.error(f"No schemas found for db_id={db_id}. Embeddings may not have been generated yet.")
            return None, None, None, None

        max_iter = max(1, self.config.max_self_correction_iterations)
        retry_k = max(1, self.config.retry_k_samples)
        first_k = self.config.k_samples

        effective_is_retry = is_retry
        effective_error_type = retry_error_type
        effective_error_detail = retry_error_detail
        attempt_tokens: List[Dict[str, int]] = []
        last_validation_error: Optional[str] = None
        last_exception: Optional[Exception] = None

        loop_start = time.monotonic()

        # The client is built per request (not a module singleton) so its httpx
        # pool stays bound to this request's event loop — every endpoint runs
        # through its own asyncio.run. The relevance check + every iteration
        # share the same client to avoid per-attempt connection setup.
        async with build_async_client() as client:
            if not await self._check_question_relevance(question, schemas, client):
                return None, None, None, None

            for iteration in range(1, max_iter + 1):
                k = first_k if iteration == 1 else retry_k
                attempt_start = time.monotonic()
                try:
                    sql, metadata, tokens, error_detail = await self._attempt_generation(
                        question, schemas, past_questions, client,
                        db_id=db_id, tenant_id=tenant_id,
                        is_retry=effective_is_retry,
                        retry_error_type=effective_error_type,
                        retry_error_detail=effective_error_detail,
                        k_samples=k,
                    )
                except Exception as e:
                    elapsed_ms = int((time.monotonic() - attempt_start) * 1000)
                    if iteration == 1:
                        # Let the API route classify (rate_limit / connection_error / …)
                        raise
                    # Infra failure mid-loop: stop. Retrying after 429/timeout
                    # rarely helps and would lose validation-error telemetry.
                    last_exception = e
                    logger.warning(
                        f"[SelfCorrection] iter={iteration}/{max_iter} k={k} "
                        f"elapsed_ms={elapsed_ms} outcome=exception err={e!r}"
                    )
                    break

                attempt_tokens.append(tokens)
                elapsed_ms = int((time.monotonic() - attempt_start) * 1000)

                if sql is not None:
                    total_ms = int((time.monotonic() - loop_start) * 1000)
                    logger.info(
                        f"[SelfCorrection] iter={iteration}/{max_iter} k={k} "
                        f"elapsed_ms={elapsed_ms} total_ms={total_ms} outcome=success"
                    )
                    metadata = self._annotate_self_correction(metadata, iteration)
                    return sql, metadata, self._sum_token_usages(attempt_tokens), None

                last_validation_error = error_detail
                # Feed ONLY the latest error into the next prompt — accumulating
                # iteration history would clutter the prompt and dilute the signal.
                effective_is_retry = True
                effective_error_type = "ai_failure"
                effective_error_detail = error_detail
                logger.warning(
                    f"[SelfCorrection] iter={iteration}/{max_iter} k={k} "
                    f"elapsed_ms={elapsed_ms} outcome=validation_failed err={error_detail!r}"
                )

        # Prefer the validation error: a SQL-level message ("Unknown column abc")
        # is more actionable than a transient infra exception from a late iteration.
        final_error_detail = last_validation_error or (repr(last_exception) if last_exception else None)
        logger.error(
            f"[SelfCorrection] exhausted max_iter={max_iter} "
            f"total_ms={int((time.monotonic() - loop_start) * 1000)} "
            f"final_error={final_error_detail!r}"
        )
        return None, None, self._sum_token_usages(attempt_tokens), final_error_detail

    @staticmethod
    def _annotate_self_correction(metadata: Dict, iteration: int) -> Dict:
        """Tag metadata with the iteration count when self-correction kicked in.

        Iteration 1 succeeded on the first try, so it carries no annotation.
        """
        if iteration > 1:
            return {**metadata, "self_correction": {"iterations": iteration}}
        return metadata

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
        fallback = ("This query retrieves and analyzes your data.",
                    {"prompt_tokens": 0, "completion_tokens": 0, "total_tokens": 0})
        try:
            prompt = f"""Please provide an extremely succinct explanation of this report you created. Start with "I've...":

{sql}"""

            async with build_async_client() as client:
                response = await chat_completion(
                    client,
                    system_message="You are a helpful assistant that explains SQL queries in simple terms.",
                    user_message=prompt,
                    temperature=0.3,
                )

            content = response.choices[0].message.content
            if not content:
                logger.error("SQL explanation returned no content")
                return fallback
            return content.strip(), usage_to_dict(response.usage)

        except Exception as e:
            logger.exception(f"Error generating SQL explanation: {e}")
            return fallback


# Global SQL generator instance
sql_generator = SQLGenerator()