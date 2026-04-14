export interface Embed {
    card_id: number;
    x_field: string;
    y_field: string;
    title: string;
    visualization_options: string[]; // Array to hold different visualization options
    SQL: string;
    current_visualization?: string; // Current visualization type being displayed
    sql_explanation?: string; // Explanation of the generated SQL
    tokens?: {
        prompt_tokens: number;      // Input tokens (combined from SQL generation + explanation)
        completion_tokens: number;  // Output tokens (combined from SQL generation + explanation)
        total_tokens: number;       // Sum of prompt + completion tokens
    };
    from_cache?: boolean;           // True when response was served from semantic cache
    cache_similarity?: number;      // Cosine similarity score (0–1) of the cache hit
    cache_hit_type?: 'exact_hit' | 'semantic_hit';  // Exact vs fuzzy semantic match
}
