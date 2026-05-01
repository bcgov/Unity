/* ==========================================================================
   Unity AI Reporting — vanilla JS port of .working/unity-ai (Angular).
   ========================================================================== */
(function () {
    'use strict';

    // ─── DOM refs ───────────────────────────────────────────────────────────
    const root             = document.getElementById('ai-reporting-root');
    if (!root) return;

    const chatList         = document.getElementById('chat-list');
    const emptyState       = document.getElementById('empty-state');
    const chatContainer    = document.getElementById('chat-container');
    const turnsContainer   = document.getElementById('turns-container');
    const questionEmpty    = document.getElementById('question-input-empty');
    const questionActive   = document.getElementById('question-input-active');
    const navControls      = document.getElementById('nav-controls');
    const turnCounter      = document.getElementById('turn-counter');
    const btnPrev          = document.getElementById('btn-prev-turn');
    const btnNext          = document.getElementById('btn-next-turn');
    const btnMetabase      = document.getElementById('btn-metabase');
    const btnSql           = document.getElementById('btn-sql');
    const btnExplain       = document.getElementById('btn-explain');
    const btnDeleteQ       = document.getElementById('btn-delete-question');

    const apiBase = (window.reportingAiApiBaseUrl || '').replace(/\/+$/, '') + '/api';
    const MAX_RETRIES = 2;

    // ─── State ──────────────────────────────────────────────────────────────
    let turnIdSeq = 0;
    const newTurnId = () => `turn-${++turnIdSeq}`;

    const state = {
        conversation: [],          // Turn[]
        currentChatId: null,
        currentTurnIndex: 0,
        chats: [],
    };

    let resizeTimer = null;

    // ─── Utilities ──────────────────────────────────────────────────────────
    const escapeHtml = (v) => String(v ?? '')
        .replaceAll('&', '&amp;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
        .replaceAll('"', '&quot;')
        .replaceAll("'", '&#39;');

    const isLoading = () => state.conversation.some(t => t.safeUrl === 'loading');
    const findTurn  = (id) => state.conversation.find(t => t.id === id);

    const notify = abp.notify;

    // Fresh turn
    const createTurn = (question, retryCount = 0) => ({
        id: newTurnId(),
        question,
        embed: null,
        safeUrl: 'loading',                // 'loading' | 'failure' | null (success)
        loaded: false,
        sqlPanelOpen: false,
        sql_explanation_visible: false,
        sql_explanation_text: '',          // local rendered text (typewriter)
        sqlExplanationStreaming: false,
        errorType: null,
        errorMessage: null,
        errorDetail: null,
        retryCount,
        canRetry: true,
    });

    // ─── API ────────────────────────────────────────────────────────────────
    const authHeader = async () => {
        const token = await unity.grantManager.identity.jwtToken.generateJWTToken();
        return { Authorization: `Bearer ${token}` };
    };

    const apiFetch = async (path, options = {}) => {
        const headers = {
            'Content-Type': 'application/json',
            ...(await authHeader()),
            ...(options.headers || {}),
        };
        const response = await fetch(`${apiBase}${path}`, { ...options, headers });
        let body = null;
        try { body = await response.json(); } catch { /* no body */ }
        if (!response.ok) {
            const err = new Error(body?.message || body?.error || `Request failed: ${response.status}`);
            err.status      = response.status;
            err.errorType   = body?.error_type   ?? null;
            err.errorDetail = body?.detail       ?? null;
            throw err;
        }
        return body;
    };

    // Specific endpoints (mirror api.service.ts)
    const api = {
        ask: (question, conversation, isRetry, retryErrorType, retryErrorDetail) => {
            const payload = { question, conversation, is_retry: !!isRetry };
            if (isRetry && retryErrorType)   payload.retry_error_type   = retryErrorType;
            if (isRetry && retryErrorDetail) payload.retry_error_detail = retryErrorDetail;
            return apiFetch('/ask', { method: 'POST', body: JSON.stringify(payload) });
        },
        deleteCard:    (cardId) => apiFetch('/delete', { method: 'POST', body: JSON.stringify({ card_id: cardId }) }),
        explainSql:    (sql)    => apiFetch('/explain_sql', { method: 'POST', body: JSON.stringify({ sql }) }),
        getChats:      ()       => apiFetch('/chats', { method: 'POST', body: '{}' }),
        getChat:       (id)     => apiFetch(`/chats/${encodeURIComponent(id)}`, { method: 'POST', body: '{}' }),
        saveChat:      (chatId, conversation, title) =>
                                   apiFetch('/chats/save', { method: 'POST', body: JSON.stringify({ chat_id: chatId, conversation, title }) }),
        deleteChat:    (id)     => apiFetch(`/chats/${encodeURIComponent(id)}`, { method: 'DELETE', body: '{}' }),
        getMetabaseUrl:()       => apiFetch('/metabase-url', { method: 'GET' }),
    };

    // ─── Rendering ──────────────────────────────────────────────────────────

    // Build the inner HTML of a turn's bot bubble (just the bubble contents).
    const renderTurnInner = (turn) => {
        // Loading
        if (turn.safeUrl === 'loading') {
            return `
                <div class="sql-loader-container">
                    <div class="loading-animation-overlay">
                        <div class="loading-dots">
                            <div class="loading-dot"></div>
                            <div class="loading-dot"></div>
                            <div class="loading-dot"></div>
                        </div>
                        <div class="loading-text">Generating Report...</div>
                    </div>
                </div>`;
        }

        // Failure
        if (turn.safeUrl === 'failure') {
            const icon =
                turn.errorType === 'rate_limit'       ? '&#9203;' :
                turn.errorType === 'connection_error' ? '&#128268;' :
                                                        '&#9888;&#65039;';
            const title =
                turn.errorType === 'rate_limit'       ? 'Service Busy' :
                turn.errorType === 'connection_error' ? 'Connection Error' :
                turn.errorType === 'ai_failure'       ? 'Unable to Generate Report' :
                turn.errorType === 'server_error'     ? 'Something Went Wrong' :
                                                        'Something Went Wrong';
            const canRetry = (turn.retryCount ?? 0) < MAX_RETRIES && !!turn.canRetry;
            const hint = turn.errorType === 'ai_failure'
                ? 'Try rephrasing your question or adding more detail.'
                : 'Please start a new question.';
            const actions = canRetry
                ? `<button type="button" class="retry-btn" data-action="retry-question" data-turn-id="${escapeHtml(turn.id)}">Try Again</button>`
                : `<button type="button" class="retry-btn retry-btn--disabled" disabled>Try Again</button>
                   <p class="retry-hint">${escapeHtml(hint)}</p>`;
            return `
                <div class="failure-container">
                    <div class="failure-content">
                        <div class="failure-icon">${icon}</div>
                        <div class="failure-title">${escapeHtml(title)}</div>
                        <div class="failure-message">${escapeHtml(turn.errorMessage || '')}</div>
                        <div class="failure-actions">${actions}</div>
                    </div>
                </div>`;
        }

        // Success (safeUrl === null)
        const embed = turn.embed || {};

        // SQL explanation typewriter bubble
        const showCursor = turn.sqlExplanationStreaming || (!turn.sql_explanation_text && turn.sql_explanation_visible);
        const explanationHtml = turn.sql_explanation_visible
            ? `<div class="sql-explanation-bubble">
                   <div class="bubble-content">
                       <span>${escapeHtml(turn.sql_explanation_text || '')}</span>
                       ${showCursor ? '<span class="cursor">█</span>' : ''}
                   </div>
                   <div class="bubble-tail"></div>
               </div>`
            : '';

        // Cache badge
        const cacheBadgeHtml = embed.cache_hit_type === 'llm_judge_hit'
            ? `<div class="cache-badge">
                   Based on a similar previous question.
                   ${embed.cache_original_query
                       ? `<span class="cache-badge-hint">Previous question: &ldquo;${escapeHtml(embed.cache_original_query)}&rdquo;</span>`
                       : '<span class="cache-badge-hint">Check if this matches what you meant.</span>'}
                   <button type="button" class="cache-fresh-btn" data-action="fresh-answer" data-turn-id="${escapeHtml(turn.id)}"${isLoading() ? ' disabled' : ''}>Get fresh answer</button>
               </div>`
            : '';

        // Metabase view button
        const metabaseHtml = `
            <div class="metabase-view-container">
                <button type="button" class="metabase-view-btn" data-action="metabase-turn" data-turn-id="${escapeHtml(turn.id)}" title="View in Metabase">
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                        <path d="M18 13V19C18 20.1046 17.1046 21 16 21H5C3.89543 21 3 20.1046 3 19V8C3 6.89543 3.89543 6 5 6H11" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                        <path d="M15 3H21V9" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                        <path d="M10 14L21 3" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                    </svg>
                    <span>View Full Report in Metabase</span>
                </button>
            </div>`;

        // SQL panel overlay
        const sqlPanelHtml = turn.sqlPanelOpen
            ? `<div class="sql-panel">
                   <div class="sql-panel-header">Generated SQL</div>
                   <pre class="sql-code">${escapeHtml(embed.SQL || '')}</pre>
               </div>`
            : '';

        return `
            <div class="bot-inner loaded">
                ${explanationHtml}
                ${cacheBadgeHtml}
                ${metabaseHtml}
                ${sqlPanelHtml}
            </div>`;
    };

    const renderTurnElement = (turn) => `
        <div class="turn" data-turn-id="${escapeHtml(turn.id)}">
            <div class="bubble bot" data-bubble-id="${escapeHtml(turn.id)}">
                ${renderTurnInner(turn)}
            </div>
        </div>`;

    const renderConversation = () => {
        const has = state.conversation.length > 0;
        emptyState.hidden    = has;
        chatContainer.hidden = !has;
        turnsContainer.innerHTML = state.conversation.map(renderTurnElement).join('');
        syncControls();
    };

    // Re-render only one bubble (avoids destroying scroll/focus elsewhere).
    const updateBubble = (turnId) => {
        const turn = findTurn(turnId);
        if (!turn) return;
        const bubble = turnsContainer.querySelector(`[data-bubble-id="${CSS.escape(turnId)}"]`);
        if (bubble) bubble.innerHTML = renderTurnInner(turn);
    };

    const syncControls = () => {
        const len = state.conversation.length;
        const idx = state.currentTurnIndex;
        const current = state.conversation[idx] || null;
        const isSuccess = current?.safeUrl === null;
        const hasSql    = isSuccess && !!current?.embed?.SQL;

        turnCounter.textContent = len ? `${idx + 1} / ${len}` : '0 / 0';
        navControls.hidden = len <= 1;
        btnPrev.disabled = idx <= 0;
        btnNext.disabled = idx >= len - 1;

        btnMetabase.disabled = !isSuccess;
        btnSql.disabled      = !hasSql;
        btnExplain.disabled  = !hasSql;
        btnDeleteQ.disabled  = !isSuccess;
    };

    // Smooth scroll to a turn (matches reference scrollToTurn).
    const scrollToTurn = (index) => {
        const turns = turnsContainer.querySelectorAll('.turn');
        const el = turns[index];
        if (!el) return;
        const containerRect = turnsContainer.getBoundingClientRect();
        const turnRect      = el.getBoundingClientRect();
        const top = turnsContainer.scrollTop + (turnRect.top - containerRect.top);
        turnsContainer.scrollTo({ top, behavior: 'smooth' });
    };

    // ─── Sidebar ────────────────────────────────────────────────────────────
    const renderChats = () => {
        if (!state.chats.length) {
            chatList.innerHTML = '<div class="empty-state">No reports yet. Start a new one!</div>';
            return;
        }
        chatList.innerHTML = state.chats.map(c => `
            <div class="chat-item${c.id === state.currentChatId ? ' active' : ''}" data-action="select-chat" data-chat-id="${escapeHtml(c.id)}">
                <div class="chat-title">${escapeHtml(c.title || 'Untitled report')}</div>
                <button type="button" class="delete-chat-btn" data-action="delete-chat" data-chat-id="${escapeHtml(c.id)}" title="Delete report">×</button>
            </div>`).join('');
    };

    const loadChats = async () => {
        try {
            const chats = await api.getChats();
            state.chats = Array.isArray(chats) ? chats : [];
            renderChats();
        } catch (err) {
            console.error('Failed to load chats:', err);
            state.chats = [];
            chatList.innerHTML = '<div class="empty-state">Unable to load reports.</div>';
        }
    };

    // ─── Save / load chat ───────────────────────────────────────────────────
    const saveChat = async () => {
        if (state.conversation.length === 0) return;
        const turnsToSave = state.conversation.filter(t => t.safeUrl !== 'loading');
        if (!turnsToSave.length) return;

        const mostRecent = turnsToSave[turnsToSave.length - 1];
        const title = mostRecent?.embed?.title || state.conversation[0]?.question || 'New Report';
        const conversation = turnsToSave.map(t => ({
            question:                t.question,
            embed:                   t.embed,
            safeUrl:                 t.safeUrl,
            loaded:                  t.loaded,
            sqlPanelOpen:            t.sqlPanelOpen,
            sql_explanation_visible: t.sql_explanation_visible,
            errorType:               t.errorType,
            errorMessage:            t.errorMessage,
            errorDetail:             t.errorDetail,
            retryCount:              t.retryCount,
            canRetry:                t.canRetry,
        }));

        try {
            const res = await api.saveChat(state.currentChatId, conversation, title);
            if (res?.chat_id) state.currentChatId = res.chat_id;
            await loadChats();
        } catch (err) {
            console.error('Failed to save chat:', err);
            notify.error('Failed to save report. Please try again.');
        }
    };

    const loadChat = async (chatId) => {
        try {
            const data = await api.getChat(chatId);
            const raw = Array.isArray(data?.conversation) ? data.conversation : [];
            state.conversation = raw.map(r => ({
                id: newTurnId(),
                question: r.question || '',
                embed: r.embed || null,
                safeUrl: r.safeUrl ?? null,
                loaded: true,
                sqlPanelOpen: r.sqlPanelOpen ?? false,
                sql_explanation_visible: r.sql_explanation_visible ?? false,
                sql_explanation_text: r.embed?.sql_explanation || '',
                sqlExplanationStreaming: false,
                errorType: r.errorType ?? null,
                errorMessage: r.errorMessage ?? null,
                errorDetail: r.errorDetail ?? null,
                retryCount: r.retryCount ?? 0,
                canRetry: r.canRetry ?? true,
            }));
            state.currentChatId   = chatId;
            state.currentTurnIndex = Math.max(0, state.conversation.length - 1);
            renderConversation();
            renderChats();
            requestAnimationFrame(() => scrollToTurn(state.currentTurnIndex));
        } catch (err) {
            console.error('Failed to load chat:', err);
            notify.error('Failed to load chat. Please try again.');
        }
    };

    const newChat = () => {
        state.conversation   = [];
        state.currentChatId  = null;
        state.currentTurnIndex = 0;
        renderConversation();
        renderChats();
        questionEmpty?.focus();
    };

    // ─── Ask question (and retry / fresh answer) ────────────────────────────
    const askQuestion = async (text, opts = {}) => {
        const trimmed = (text || '').trim();
        if (!trimmed) {
            notify.info('Please enter a question.');
            return;
        }
        if (isLoading()) return;

        const { retryCount = 0, isRetry = false, retryErrorType = null, retryErrorDetail = null } = opts;

        const turn = createTurn(trimmed, retryCount);
        state.conversation.push(turn);
        state.currentTurnIndex = state.conversation.length - 1;
        renderConversation();
        requestAnimationFrame(() => scrollToTurn(state.currentTurnIndex));

        if (questionEmpty)  questionEmpty.value  = '';
        if (questionActive) questionActive.value = '';

        // Build conversation context (success turns only, excluding the new one).
        const context = state.conversation
            .slice(0, -1)
            .filter(t => t.safeUrl === null)
            .map(t => ({ question: t.question, embed: t.embed }));

        try {
            const result = await api.ask(trimmed, context, isRetry, retryErrorType, retryErrorDetail);
            const t = findTurn(turn.id);
            if (!t) return;
            t.embed = {
                card_id:                result?.card_id,
                x_field:                result?.x_field || '',
                y_field:                result?.y_field || '',
                title:                  result?.title   || trimmed,
                visualization_options:  result?.visualization_options || [],
                SQL:                    result?.SQL || '',
                sql_explanation:        result?.sql_explanation || '',
                tokens:                 result?.tokens || null,
                from_cache:             result?.from_cache,
                cache_similarity:       result?.cache_similarity,
                cache_hit_type:         result?.cache_hit_type || null,
                cache_original_query:   result?.cache_original_query || null,
            };
            t.safeUrl      = null;
            t.loaded = true;
            updateBubble(t.id);
            syncControls();
            await saveChat();
        } catch (err) {
            console.error('Failed to process question:', err);
            const t = findTurn(turn.id);
            if (!t) return;
            t.loaded = true;
            t.safeUrl      = 'failure';
            t.errorDetail  = err?.errorDetail ?? null;

            const status    = err?.status;
            const errorType = err?.errorType;
            const message   = err?.message;

            if (errorType === 'rate_limit' || status === 429) {
                t.errorType    = 'rate_limit';
                t.errorMessage = message || 'Rate limit exceeded. Please wait a moment and try again.';
                t.canRetry     = true;
            } else if (errorType === 'connection_error' || status === 503) {
                t.errorType    = 'connection_error';
                t.errorMessage = message || 'Connection error. The service may be temporarily unavailable.';
                t.canRetry     = true;
            } else if (errorType === 'ai_failure' || status === 422) {
                t.errorType    = 'ai_failure';
                t.errorMessage = message || "I couldn't generate a report from that question.";
                t.canRetry     = false;
            } else if (errorType === 'server_error' || (status && status >= 500)) {
                t.errorType    = 'server_error';
                t.errorMessage = message || 'Something went wrong on our end. Please try again.';
                t.canRetry     = true;
            } else {
                t.errorType    = 'unknown';
                t.errorMessage = message || 'Something went wrong. Please try again.';
                t.canRetry     = true;
            }

            updateBubble(t.id);
            syncControls();
        }
    };

    const retryQuestion = (turnId) => {
        const turn = findTurn(turnId);
        if (!turn) return;
        const nextRetry = (turn.retryCount ?? 0) + 1;
        const errType   = turn.errorType;
        const errDetail = turn.errorDetail;
        const question  = turn.question;
        const idx = state.conversation.findIndex(t => t.id === turnId);
        state.conversation.splice(idx, 1);
        if (state.currentTurnIndex >= state.conversation.length) {
            state.currentTurnIndex = Math.max(0, state.conversation.length - 1);
        }
        askQuestion(question, { retryCount: nextRetry, isRetry: true, retryErrorType: errType, retryErrorDetail: errDetail });
    };

    const getFreshAnswer = (turnId) => {
        if (isLoading()) return;
        const turn = findTurn(turnId);
        if (!turn) return;
        const question = turn.question;
        const idx = state.conversation.findIndex(t => t.id === turnId);
        state.conversation.splice(idx, 1);
        if (state.currentTurnIndex >= state.conversation.length) {
            state.currentTurnIndex = Math.max(0, state.conversation.length - 1);
        }
        // retryCount=1 + isRetry=true causes backend to skip the semantic cache.
        askQuestion(question, { retryCount: 1, isRetry: true });
    };

    // ─── SQL panel ──────────────────────────────────────────────────────────
    const toggleSqlPanel = (turnId) => {
        const turn = findTurn(turnId);
        if (!turn) return;
        turn.sqlPanelOpen = !turn.sqlPanelOpen;
        updateBubble(turnId);
    };

    // ─── SQL explanation (typewriter) ───────────────────────────────────────
    const streamExplanation = (turnId, text) => {
        const turn = findTurn(turnId);
        if (!turn) return;
        turn.sqlExplanationStreaming = true;
        turn.sql_explanation_text = '';
        let i = 0;
        const interval = setInterval(() => {
            const t = findTurn(turnId);
            if (!t || !t.sql_explanation_visible) {
                clearInterval(interval);
                if (t) t.sqlExplanationStreaming = false;
                return;
            }
            if (i < text.length) {
                t.sql_explanation_text += text[i++];
                updateBubble(turnId);
            } else {
                t.sqlExplanationStreaming = false;
                clearInterval(interval);
                updateBubble(turnId);
            }
        }, 10);
    };

    const generateSqlExplanation = async (turnId) => {
        const turn = findTurn(turnId);
        if (!turn?.embed?.SQL) return;

        // Toggle visibility (matches reference)
        turn.sql_explanation_visible = !turn.sql_explanation_visible;
        updateBubble(turnId);

        if (turn.sql_explanation_visible && !turn.embed.sql_explanation) {
            try {
                const res = await api.explainSql(turn.embed.SQL);
                const t = findTurn(turnId);
                if (!t || !t.sql_explanation_visible) return;
                t.embed.sql_explanation = res?.explanation || '';
                if (t.embed.tokens && res?.tokens) {
                    t.embed.tokens.prompt_tokens     += res.tokens.prompt_tokens     || 0;
                    t.embed.tokens.completion_tokens += res.tokens.completion_tokens || 0;
                    t.embed.tokens.total_tokens      += res.tokens.total_tokens      || 0;
                }
                streamExplanation(turnId, t.embed.sql_explanation);
            } catch (err) {
                console.error('Failed to generate SQL explanation:', err);
                const status = err?.status;
                let msg = 'Failed to generate SQL explanation. ';
                if (status === 429)        msg += 'Rate limit exceeded. Please try again later.';
                else if (status >= 500)    msg += 'Server error. Please try again.';
                else                       msg += 'Please try again or contact support if the issue persists.';
                notify.error(msg);
                const t = findTurn(turnId);
                if (t) {
                    t.embed.sql_explanation = 'Unable to generate explanation at this time.';
                    t.sql_explanation_text  = t.embed.sql_explanation;
                    updateBubble(turnId);
                }
            }
        } else if (turn.sql_explanation_visible && turn.embed.sql_explanation && !turn.sql_explanation_text) {
            // Already have the text from a saved chat — render it directly without streaming.
            turn.sql_explanation_text = turn.embed.sql_explanation;
            updateBubble(turnId);
        }

        await saveChat();
    };

    // ─── Metabase redirect ──────────────────────────────────────────────────
    const isValidCardId = (id) => Number.isInteger(Number(id)) && Number(id) > 0 && Number(id) <= 999999999;

    const isValidRedirectUrl = (full, base) => {
        try {
            const f = new URL(full);
            const b = new URL(base);
            if (f.origin !== b.origin) return false;
            return /^\/question\/\d+$/.test(f.pathname);
        } catch { return false; }
    };

    const redirectToMetabase = async (cardId) => {
        if (!isValidCardId(cardId)) {
            notify.error('Unable to open Metabase — invalid card ID');
            return;
        }
        try {
            const res = await api.getMetabaseUrl();
            const baseUrl = res?.metabase_url;
            if (!baseUrl) {
                notify.error('Unable to open Metabase — invalid configuration');
                return;
            }
            const full = `${baseUrl.replace(/\/+$/, '')}/question/${cardId}`;
            if (!isValidRedirectUrl(full, baseUrl)) {
                notify.error('Unable to open Metabase — security validation failed');
                return;
            }
            window.open(full, '_blank', 'noopener,noreferrer');
        } catch (err) {
            console.error('Error redirecting to Metabase:', err);
            notify.error('Unable to open Metabase');
        }
    };

    // ─── Delete question / chat ─────────────────────────────────────────────
    const deleteQuestion = async (turnId) => {
        const turn = findTurn(turnId);
        if (!turn) return;
        const ok = await abp.message.confirm('Are you sure you want to delete this question? This action cannot be undone.', 'Delete Question');
        if (!ok) return;

        try {
            if (turn.embed?.card_id) {
                await api.deleteCard(turn.embed.card_id);
            }
            const idx = state.conversation.findIndex(t => t.id === turnId);
            if (idx >= 0) state.conversation.splice(idx, 1);

            // Last turn → delete entire chat.
            if (state.conversation.length === 0 && state.currentChatId) {
                try {
                    await api.deleteChat(state.currentChatId);
                    state.currentChatId = null;
                    await loadChats();
                    notify.success('Report deleted successfully');
                } catch (e) {
                    console.error('Error deleting empty chat:', e);
                    notify.error('Failed to delete report. Please try again.');
                }
                renderConversation();
                return;
            }

            // Adjust currentTurnIndex
            if (idx <= state.currentTurnIndex && state.currentTurnIndex > 0) {
                state.currentTurnIndex = Math.max(0, state.currentTurnIndex - 1);
            } else if (state.currentTurnIndex >= state.conversation.length) {
                state.currentTurnIndex = Math.max(0, state.conversation.length - 1);
            }

            await saveChat();
            renderConversation();
            requestAnimationFrame(() => scrollToTurn(state.currentTurnIndex));
            notify.success('Question deleted successfully');
        } catch (err) {
            console.error('Error deleting question:', err);
            notify.error('Failed to delete question. Please try again.');
        }
    };

    const deleteChatPrompt = async (chatId) => {
        const chat = state.chats.find(c => c.id === chatId);
        const ok = await abp.message.confirm('Are you sure you want to delete this chat? This action cannot be undone.', 'Delete Chat');
        if (!ok) return;
        try {
            await api.deleteChat(chatId);
            state.chats = state.chats.filter(c => c.id !== chatId);
            if (state.currentChatId === chatId) newChat();
            else renderChats();
            notify.success(`Report${chat?.title ? ` "${chat.title}"` : ''} deleted successfully`);
        } catch (err) {
            console.error('Failed to delete chat:', err);
            notify.error('Failed to delete report. Please try again.');
        }
    };

    // ─── Helpers for current turn ───────────────────────────────────────────
    const currentTurn = () => state.conversation[state.currentTurnIndex] || null;

    // ─── Event delegation ───────────────────────────────────────────────────
    document.body.addEventListener('click', async (event) => {
        const actionEl = event.target.closest('[data-action]');
        if (!actionEl) return;
        const action = actionEl.getAttribute('data-action');

        switch (action) {
            // sidebar
            case 'new-chat':       newChat(); break;
            case 'select-chat':    loadChat(actionEl.getAttribute('data-chat-id')); break;
            case 'delete-chat':
                event.stopPropagation();
                deleteChatPrompt(actionEl.getAttribute('data-chat-id'));
                break;

            // ask row
            case 'ask': {
                const input = actionEl.closest('.ask-row-container')?.querySelector('input[type="text"]');
                askQuestion(input?.value || '');
                break;
            }

            // toolbar (operates on current turn)
            case 'metabase': {
                const t = currentTurn();
                if (t?.embed?.card_id) redirectToMetabase(t.embed.card_id);
                break;
            }
            case 'toggle-sql': {
                const t = currentTurn();
                if (t) toggleSqlPanel(t.id);
                break;
            }
            case 'explain-sql': {
                const t = currentTurn();
                if (t) generateSqlExplanation(t.id);
                break;
            }
            case 'delete-question': {
                const t = currentTurn();
                if (t) deleteQuestion(t.id);
                break;
            }
            case 'prev-turn':
                if (state.currentTurnIndex > 0) {
                    state.currentTurnIndex--;
                    syncControls();
                    scrollToTurn(state.currentTurnIndex);
                }
                break;
            case 'next-turn':
                if (state.currentTurnIndex < state.conversation.length - 1) {
                    state.currentTurnIndex++;
                    syncControls();
                    scrollToTurn(state.currentTurnIndex);
                }
                break;

            // in-bubble actions
            case 'metabase-turn': {
                const t = findTurn(actionEl.getAttribute('data-turn-id'));
                if (t?.embed?.card_id) redirectToMetabase(t.embed.card_id);
                break;
            }
            case 'fresh-answer':   getFreshAnswer(actionEl.getAttribute('data-turn-id')); break;
            case 'retry-question': retryQuestion(actionEl.getAttribute('data-turn-id')); break;
        }
    });

    // Enter key submits
    [questionEmpty, questionActive].forEach((input) => {
        input?.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') { e.preventDefault(); askQuestion(input.value); }
        });
    });

    // Resize listener — keep current turn in view
    window.addEventListener('resize', () => {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(() => {
            if (state.conversation.length > 0) scrollToTurn(state.currentTurnIndex);
        }, 150);
    });

    // ─── Init ───────────────────────────────────────────────────────────────
    renderConversation();
    loadChats();
})();
