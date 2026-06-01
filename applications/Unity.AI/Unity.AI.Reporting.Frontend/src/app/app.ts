import { Component, ViewChild, ElementRef, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { SafeResourceUrl, DomSanitizer, SafeHtml } from '@angular/platform-browser';

import { Embed } from './embed';
import { Turn } from './turn';
import { SqlExplanationComponent } from './sql-explanation/sql-explanation';
import { ToastComponent } from './toast/toast.component';
import { AuthService } from './services/auth.service';
import { ApiService } from './services/api.service';
import { ToastService } from './services/toast.service';
import { LoggerService } from './services/logger.service';
import { IframeDetectorService } from './iframe-detector.service';
import { ConfigService } from './services/config.service';
import { SidebarComponent, Chat } from './sidebar/sidebar';
import { AlertComponent } from './alert/alert';
import { environment } from '../environments/environment';

@Component({
  selector: 'app-root',
  imports: [FormsModule, DecimalPipe, RouterLink, SqlExplanationComponent, SidebarComponent, ToastComponent, AlertComponent],
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class App implements OnInit, OnDestroy {
  protected title = 'AI Reporting';
  protected api_url = environment.apiUrl;
  question: string = "";
  conversation: Turn[] = [];
  sidebarOpen: boolean = true;
  currentChatId: string | null = null;
  currentTurnIndex: number = 0;
  visualizationDropdownOpen: boolean = false;
  selectedVisualization: string = 'table';
  readonly MAX_RETRIES = 2;
  showDeleteQuestionAlert: boolean = false;
  private turnToDelete: Turn | null = null;

  constructor(
    private readonly authService: AuthService,
    private readonly apiService: ApiService,
    private readonly toastService: ToastService,
    private readonly logger: LoggerService,
    private readonly iframeDetector: IframeDetectorService,
    private readonly configService: ConfigService,
    private readonly cdr: ChangeDetectorRef,
    private readonly sanitizer: DomSanitizer
  ) {}

  @ViewChild('turnsContainer') private readonly turnsContainer!: ElementRef<HTMLDivElement>;
  @ViewChild('sqlAnimationContainer') private readonly sqlAnimationContainer!: ElementRef<HTMLDivElement>;
  @ViewChild('sidebar') private readonly sidebar!: SidebarComponent;

  ngOnInit(): void {
    this.initialize();
  }

  private async initialize(): Promise<void> {
    console.log('🔧 APP COMPONENT: Initialized (postMessage handling done by AuthService)');

    // Add iframe-specific styling
    this.iframeDetector.addIframeClass();

    // Check if running in iframe and authenticated
    if (this.iframeDetector.isInIframe()) {
      const isAuthenticated = await this.authService.isAuthenticated();
      if (!isAuthenticated) {
        // Block the application from functioning
        document.body.innerHTML = '<div style="text-align: center; padding: 50px; font-family: Arial;"><h2>Authentication Required</h2><p>Invalid or missing authentication token.</p></div>';
        return;
      }
    }

    // Add window resize listener to maintain scroll position
    window.addEventListener('resize', this.resizeListener);

    // Add click listener to close dropdown when clicking outside
    document.addEventListener('click', this.handleDocumentClick);
  }

  private onWindowResize(): void {
    // Debounce resize events to avoid excessive scrolling
    clearTimeout(this.resizeTimeout);
    this.resizeTimeout = setTimeout(() => {
      if (this.conversation.length > 0) {
        this.scrollToTurn(this.currentTurnIndex);
      }
    }, 150);
  }

  private resizeTimeout: any;
  private readonly resizeListener = () => this.onWindowResize();
  private readonly handleDocumentClick = (event: Event) => this.onDocumentClick(event);

  private onDocumentClick(event: Event): void {
    const target = event.target as HTMLElement;
    const dropdownElement = target.closest('.visualization-dropdown');
    
    // If click is outside the dropdown, close it
    if (!dropdownElement && this.visualizationDropdownOpen) {
      this.visualizationDropdownOpen = false;
      this.cdr.markForCheck();
    }
  }

  private scrollToBottom(): void {
    // Wait until the DOM update that adds the message is done
    setTimeout(() => {
      if (this.turnsContainer) {
        this.turnsContainer.nativeElement.scroll({
          top:  this.turnsContainer.nativeElement.scrollHeight,
          behavior: 'smooth'          // ↳ animated; drop for instant jump
        });
      }
    }, 0);
  }

  private scrollToBottomInstant(): void {
    // Wait until the DOM update that adds the message is done
    setTimeout(() => {
      if (this.turnsContainer) {
        this.turnsContainer.nativeElement.scrollTop = this.turnsContainer.nativeElement.scrollHeight;
      }
    }, 0);
  }

  scrollSqlToBottom(): void {
    if (this.sqlAnimationContainer) {
      this.sqlAnimationContainer.nativeElement.scrollTop = this.sqlAnimationContainer.nativeElement.scrollHeight;
    }
  }

  toggleSqlPanel(turn: Turn): void {
    turn.sqlPanelOpen = !turn.sqlPanelOpen;
  }

  async generateSqlExplanation(turn: Turn): Promise<void> {
    if (!turn.embed?.SQL) {
      // If no SQL exists, do nothing
      return;
    }

    // Toggle visibility
    turn.sql_explanation_visible = !turn.sql_explanation_visible;

    // If turning on and no explanation exists yet, generate it
    if (turn.sql_explanation_visible && !turn.embed.sql_explanation) {
      try {
        const explanationResponse = await firstValueFrom(
          this.apiService.explainSql<{
            explanation: string;
            tokens?: { prompt_tokens: number; completion_tokens: number; total_tokens: number; }
          }>(turn.embed.SQL)
        );
        turn.embed.sql_explanation = explanationResponse.explanation;

        // Combine explanation tokens with existing SQL generation tokens
        if (explanationResponse.tokens && turn.embed.tokens) {
          turn.embed.tokens.prompt_tokens += explanationResponse.tokens.prompt_tokens;
          turn.embed.tokens.completion_tokens += explanationResponse.tokens.completion_tokens;
          turn.embed.tokens.total_tokens += explanationResponse.tokens.total_tokens;
        }
      } catch (error: any) {
        this.logger.error('Failed to generate SQL explanation:', error);

        // Provide user feedback about the failure
        let errorMessage = 'Failed to generate SQL explanation. ';
        if (error?.status === 429) {
          errorMessage += 'Rate limit exceeded. Please try again later.';
        } else if (error?.status >= 500) {
          errorMessage += 'Server error. Please try again.';
        } else {
          errorMessage += 'Please try again or contact support if the issue persists.';
        }

        this.toastService.error(errorMessage);

        // Set a fallback explanation that indicates the failure
        turn.embed.sql_explanation = "Unable to generate explanation at this time.";
      }
    }

    // Save the chat to persist the visibility state
    await this.saveChat();
    this.cdr.markForCheck();
  }

  async redirectToMB(turn: Turn): Promise<Window | null> {
    try {
      // Get Metabase URL from backend configuration
      const metabaseUrlResponse = await firstValueFrom(
        this.apiService.getMetabaseUrl<{ metabase_url: string }>()
      );
      const metabaseUrl = metabaseUrlResponse.metabase_url;
      const cardId = turn.embed.card_id;

      // Check if we have a valid Metabase URL
      if (!metabaseUrl) {
        this.logger.error('Invalid or missing Metabase URL');
        this.toastService.error('Unable to open Metabase - invalid configuration');
        return null;
      }

      // Validate card ID
      if (!cardId || !this.isValidCardId(cardId)) {
        this.logger.error('Invalid card ID');
        this.toastService.error('Unable to open Metabase - invalid card ID');
        return null;
      }

      // Construct the URL
      const fullUrl = `${metabaseUrl}/question/${cardId}`;

      // Additional validation to ensure the constructed URL is still safe
      if (!this.isValidRedirectUrl(fullUrl, metabaseUrl)) {
        this.logger.error('Invalid redirect URL constructed');
        this.toastService.error('Unable to open Metabase - security validation failed');
        return null;
      }

      return window.open(fullUrl, '_blank');
    } catch (error) {
      this.logger.error('Error redirecting to Metabase:', error);
      this.toastService.error('Unable to open Metabase');
      return null;
    }
  }


  private isValidCardId(cardId: any): boolean {
    // Ensure card ID is a positive integer
    return Number.isInteger(cardId) && cardId > 0 && cardId <= 999999999;
  }

  private isValidRedirectUrl(fullUrl: string, expectedBaseUrl: string): boolean {
    try {
      const parsedFullUrl = new URL(fullUrl);
      const parsedBaseUrl = new URL(expectedBaseUrl);
      
      // Ensure the full URL starts with the expected base URL
      if (parsedFullUrl.origin !== parsedBaseUrl.origin) {
        return false;
      }
      
      // Ensure the path follows expected pattern
      const pathPattern = /^\/question\/\d+$/;
      if (!pathPattern.test(parsedFullUrl.pathname)) {
        return false;
      }
      
      return true;
    } catch {
      return false;
    }
  }

  deleteQuestion(turn: Turn): void {
    this.turnToDelete = turn;
    this.showDeleteQuestionAlert = true;
  }

  async confirmDeleteQuestion(): Promise<void> {
    if (!this.turnToDelete) return;
    const turn = this.turnToDelete;
    this.cancelDeleteQuestion();

    try {
      // Delete the card from the backend
      await firstValueFrom(
        this.apiService.deleteCard(turn.embed.card_id)
      );

      // Find the index of the turn being deleted
      const deletedIndex = this.conversation.findIndex(t => t === turn);

      // Remove the turn from the conversation
      this.conversation = this.conversation.filter(t => t !== turn);

      // If this was the last question in the chat, delete the chat entirely
      if (this.conversation.length === 0 && this.currentChatId) {
        try {
          await firstValueFrom(
            this.apiService.deleteChat(this.currentChatId)
          );
          this.currentChatId = null;

          // Refresh the sidebar to remove the deleted chat
          if (this.sidebar) {
            this.sidebar.loadChats();
          }

          // Show success toast for entire chat deletion
          this.toastService.success('Report deleted successfully');
        } catch (deleteError) {
          this.logger.error('Error deleting empty chat:', deleteError);
          this.toastService.error('Failed to delete report. Please try again.');
        }
        return; // Exit early since there's nothing left to update
      }

      // Adjust currentTurnIndex if necessary
      if (deletedIndex <= this.currentTurnIndex && this.currentTurnIndex > 0) {
        this.currentTurnIndex = Math.max(0, this.currentTurnIndex - 1);
      } else if (this.currentTurnIndex >= this.conversation.length) {
        this.currentTurnIndex = Math.max(0, this.conversation.length - 1);
      }

      // Update dropdown selection for the new current turn
      // this.updateDropdownSelection(); // VISUALIZATION: commented out

      // Save the updated conversation to the database
      await this.saveChat();

      // Show success toast for individual question deletion
      this.toastService.success('Question deleted successfully');

    } catch (error) {
      // Show error toast
      this.logger.error('Error deleting question:', error);
      this.toastService.error('Failed to delete question. Please try again.');
    } finally {
      this.cdr.markForCheck();
    }
  }

  cancelDeleteQuestion(): void {
    this.showDeleteQuestionAlert = false;
    this.turnToDelete = null;
  }

  // VISUALIZATION: Commented out - not functional since switching to Metabase redirect.
  //               Restore when custom visualization is implemented.
  // async changeDisplay(turn: Turn, mode: string) {
  //   try {
  //     await firstValueFrom(
  //       this.apiService.changeDisplay<Embed>(turn.embed.card_id, mode, turn.embed.x_field, turn.embed.y_field)
  //     );
  //     // Update the current visualization in the embed
  //     turn.embed.current_visualization = mode;
  //   } catch (error) {
  //     // Log the error and notify the user
  //     this.logger.error('Error changing display mode:', error);
  //     this.toastService.error('Failed to change display mode. Please try again.');
  //   } finally {
  //     this.cdr.markForCheck();
  //   }
  // }

  async resetConversation() {
    this.conversation = [];
    this.currentTurnIndex = 0;
  }

  async askQuestion(retryCount: number = 0, retryErrorType?: Turn['errorType'], retryErrorDetail?: string | null) {
    if (this.question.trim() === "") {
      alert("Please enter a question.");
      return;
    }
    
    // Always reset to table visualization for new questions
    this.selectedVisualization = 'table';
    
    const turn = {question: this.question.trim(), embed: {"url": "", "card_id": 0, "x_field": "", "y_field": "", "title": "", "visualization_options": [], "SQL": ""}, safeUrl: 'loading' as 'loading' | 'failure' | SafeResourceUrl, iframeLoaded: false, sqlPanelOpen: false, sql_explanation: "", sql_explanation_visible: false} as Turn;
    this.conversation.push(turn);
    if (retryCount > 0) {
      turn.retryCount = retryCount;
    }

    // Set the new turn as the current turn for navigation
    this.currentTurnIndex = this.conversation.length - 1;
    
    this.scrollToBottom();   
    this.question = "";
    try {
      if (! await this.authService.isAuthenticated()) throw new Error('Not authenticated');

      turn.embed = await firstValueFrom(
        this.apiService.askQuestion<Embed>(turn.question, this.conversation, retryCount > 0, retryErrorType, retryErrorDetail)
      );
      turn.embed.current_visualization = 'table';
      turn.iframeLoaded = true;
      turn.safeUrl = null;

      await this.saveChat();
    } catch (error: any) {
      this.logger.error('Failed to process question:', error);
      turn.iframeLoaded = true;
      turn.safeUrl = "failure";

      // Classify the error using the stable backend schema, falling back to HTTP status
      const errorType = error?.error?.error_type;
      const errorMsg = error?.error?.message;

      turn.errorDetail = error?.error?.detail ?? null;

      if (error?.message === 'Not authenticated') {
        turn.errorType = 'unknown';
        turn.errorMessage = 'Your session has expired. Please sign in again.';
        turn.canRetry = false;
      } else if (errorType === 'rate_limit' || error?.status === 429) {
        turn.errorType = 'rate_limit';
        turn.errorMessage = errorMsg || 'Rate limit exceeded. Please wait a moment and try again.';
        turn.canRetry = true;
      } else if (errorType === 'connection_error' || error?.status === 503) {
        turn.errorType = 'connection_error';
        turn.errorMessage = errorMsg || 'Connection error. The service may be temporarily unavailable.';
        turn.canRetry = true;
      } else if (errorType === 'ai_failure' || error?.status === 422) {
        turn.errorType = 'ai_failure';
        turn.errorMessage = errorMsg || "I couldn't generate a report from that question.";
        turn.canRetry = false;
      } else if (errorType === 'server_error' || (error?.status && error.status >= 500)) {
        turn.errorType = 'server_error';
        turn.errorMessage = errorMsg || 'Something went wrong on our end. Please try again.';
        turn.canRetry = true;
      } else {
        turn.errorType = 'unknown';
        turn.errorMessage = errorMsg || 'Something went wrong. Please try again.';
        turn.canRetry = true;
      }
    } finally {
      this.cdr.markForCheck();
    }
  }

  retryQuestion(turn: Turn): void {
    const nextRetryCount = (turn.retryCount ?? 0) + 1;
    const errorType = turn.errorType;

    // Reset the turn state and retry the question
    turn.safeUrl = "loading";
    turn.iframeLoaded = false;
    turn.sqlPanelOpen = false;

    // Retry the question with the existing turn
    this.question = turn.question;
    this.conversation = this.conversation.filter(t => t !== turn);
    this.askQuestion(nextRetryCount, errorType, turn.errorDetail);
  }

  getFreshAnswer(turn: Turn): void {
    if (this.isLoading) return;
    // Resubmit the question with retry_count=1 so the backend skips the semantic cache (is_retry=true).
    this.question = turn.question;
    this.conversation = this.conversation.filter(t => t !== turn);
    this.askQuestion(1);
  }

  get isLoading(): boolean {
    return this.conversation.some(t => t.safeUrl === 'loading');
  }

  toggleSidebar(): void {
    this.sidebarOpen = !this.sidebarOpen;
    
    // Refresh chat list when opening sidebar
    if (this.sidebarOpen && this.sidebar) {
      this.sidebar.loadChats();
    }
  }

  onChatSelected(chat: Chat): void {
    this.loadChat(chat.id);
  }

  onNewChat(): void {
    this.conversation = [];
    this.currentChatId = null;
    this.currentTurnIndex = 0;
    this.selectedVisualization = 'table'; // Reset to default
  }

  async loadChat(chatId: string): Promise<void> {
    try {
      if (!await this.authService.isAuthenticated()) {
        throw new Error('Not authenticated');
      }

      const chatData = await firstValueFrom(
        this.apiService.getChat<{conversation: Turn[]}>(chatId)
      );

      this.conversation = chatData.conversation.map(turn => ({
        ...turn,
        iframeLoaded: true, // No iframe anymore, always mark as loaded
        sql_explanation_visible: turn.sql_explanation_visible ?? false // Preserve visibility state or default to false
      }));
      
      this.currentChatId = chatId;
      this.currentTurnIndex = Math.max(0, this.conversation.length - 1);
      
      // Update dropdown selection based on the current turn
      // this.updateDropdownSelection(); // VISUALIZATION: commented out
      
      // Scroll to bottom instantly after loading chat
      this.scrollToBottomInstant();
    } catch (error) {
      // Log the error and provide non-intrusive user feedback
      if (this.logger && typeof this.logger.error === 'function') {
        this.logger.error('Failed to load chat', error);
      }
      if (this.toastService && typeof (this.toastService as any).showError === 'function') {
        (this.toastService as any).showError('Failed to load chat. Please try again.');
      }
    } finally {
      this.cdr.markForCheck();
    }
  }

  async saveChat(): Promise<void> {
    if (this.conversation.length === 0) return;

    try {
      if (!await this.authService.isAuthenticated()) {
        throw new Error('Not authenticated');
      }

      // Use the most recent embed's title, or fall back to the first question
      const mostRecentTurn = this.conversation[this.conversation.length - 1];
      const chatTitle = mostRecentTurn?.embed?.title || this.conversation[0]?.question || 'New Chat';

      // Ensure sql_explanation_visible is included in the saved conversation
      const conversationToSave = this.conversation.map(turn => ({
        ...turn,
        sql_explanation_visible: turn.sql_explanation_visible || false
      }));

      const response = await firstValueFrom(
        this.apiService.saveChat<{chat_id: string}>(this.currentChatId, conversationToSave, chatTitle)
      );

      this.currentChatId = response.chat_id;
      
      // Refresh the chat list in the sidebar
      if (this.sidebar) {
        this.sidebar.loadChats();
      }
    } catch (error) {
      // Log the error and provide user feedback instead of handling it silently
      this.logger.error('Failed to save chat', error);
      this.toastService.error('Failed to save chat. Please try again.');
    }
  }

  scrollToPreviousTurn(): void {
    if (this.currentTurnIndex > 0) {
      this.currentTurnIndex--;
      this.scrollToTurn(this.currentTurnIndex);
      // this.updateDropdownSelection(); // VISUALIZATION: commented out
    }
  }

  scrollToNextTurn(): void {
    if (this.currentTurnIndex < this.conversation.length - 1) {
      this.currentTurnIndex++;
      this.scrollToTurn(this.currentTurnIndex);
      // this.updateDropdownSelection(); // VISUALIZATION: commented out
    }
  }

  private scrollToTurn(index: number): void {
    if (this.turnsContainer) {
      const turnElements = this.turnsContainer.nativeElement.querySelectorAll('.turn');
      if (turnElements[index]) {
        const turnElement = turnElements[index] as HTMLElement;
        const containerElement = this.turnsContainer.nativeElement;
        
        // Get the turn's position relative to the container's scroll area
        const containerRect = containerElement.getBoundingClientRect();
        const turnRect = turnElement.getBoundingClientRect();
        
        // Calculate the scroll position needed
        const scrollTop = containerElement.scrollTop + (turnRect.top - containerRect.top);
        
        // Scroll the container to show the turn at the top
        containerElement.scrollTo({
          top: scrollTop,
          behavior: 'smooth'
        });
      }
    }
  }

  // VISUALIZATION: Commented out - not functional since switching to Metabase redirect.
  //               Restore when custom visualization is implemented.
  // toggleVisualizationDropdown(): void {
  //   // Only toggle if there are other options available
  //   if (this.hasOtherVisualizationOptions()) {
  //     this.visualizationDropdownOpen = !this.visualizationDropdownOpen;
  //   }
  // }

  // async selectVisualization(type: string): Promise<void> {
  //   this.selectedVisualization = type;
  //   this.visualizationDropdownOpen = false;
  //
  //   // If we have a current turn, apply the visualization change immediately
  //   if (this.conversation.length > 0 && this.currentTurnIndex >= 0 && this.currentTurnIndex < this.conversation.length) {
  //     const currentTurn = this.conversation[this.currentTurnIndex];
  //     if (currentTurn.embed?.card_id) {
  //       await this.changeDisplay(currentTurn, type);
  //       // Save the chat to persist the visualization change
  //       await this.saveChat();
  //     }
  //   }
  // }

  // getVisualizationDisplayName(type: string): string {
  //   const names: { [key: string]: string } = {
  //     'table': 'Table',
  //     'bar': 'Bar Chart',
  //     'line': 'Line Chart',
  //     'pie': 'Pie Chart',
  //     'map': 'Map'
  //   };
  //   return names[type] || type;
  // }

  // getAvailableVisualizationOptions(): string[] {
  //   // Always include table as it's the default
  //   const options = ['table'];
  //   const all_options = ['bar', 'line', 'pie', 'map'];
  //
  //   // If we have a conversation, get options from the current turn
  //   if (this.conversation.length > 0 && this.currentTurnIndex >= 0 && this.currentTurnIndex < this.conversation.length) {
  //     const currentTurn = this.conversation[this.currentTurnIndex];
  //
  //     if (currentTurn.embed?.visualization_options) {
  //       // Add available options from the current turn, avoiding duplicates
  //       currentTurn.embed.visualization_options.forEach(option => {
  //         if (!options.includes(option) && all_options.includes(option)) {
  //           options.push(option);
  //         }
  //       });
  //     }
  //   } else {
  //     // If no conversation yet, show all possible options
  //     options.push(...all_options);
  //   }
  //
  //   return options;
  // }

  // hasOtherVisualizationOptions(): boolean {
  //   const availableOptions = this.getAvailableVisualizationOptions();
  //   return availableOptions.some(option => option !== this.selectedVisualization);
  // }

  // updateDropdownSelection(): void {
  //   if (this.conversation.length > 0 && this.currentTurnIndex >= 0 && this.currentTurnIndex < this.conversation.length) {
  //     const currentTurn = this.conversation[this.currentTurnIndex];
  //     if (currentTurn.embed?.current_visualization) {
  //       this.selectedVisualization = currentTurn.embed.current_visualization;
  //     } else if (currentTurn.embed) {
  //       // For older chats without current_visualization, default to table and save it
  //       this.selectedVisualization = 'table';
  //       currentTurn.embed.current_visualization = 'table';
  //     } else {
  //       // Default to table if no embed data
  //       this.selectedVisualization = 'table';
  //     }
  //   } else {
  //     // No conversation or invalid turn index
  //     this.selectedVisualization = 'table';
  //   }
  // }

  highlightSql(sql: string): SafeHtml {
    if (!sql) return this.sanitizer.bypassSecurityTrustHtml('');

    const KEYWORDS = new Set([
      'SELECT','FROM','WHERE','JOIN','LEFT','RIGHT','INNER','OUTER','FULL','ON',
      'GROUP','BY','ORDER','HAVING','LIMIT','OFFSET','INSERT','UPDATE','DELETE',
      'CREATE','DROP','ALTER','WITH','AS','AND','OR','NOT','IN','LIKE','ILIKE',
      'BETWEEN','IS','NULL','DISTINCT','UNION','ALL','CASE','WHEN','THEN','ELSE',
      'END','EXISTS','SET','INTO','VALUES','TABLE','INDEX','VIEW','PRIMARY','FOREIGN',
      'KEY','REFERENCES','CONSTRAINT','UNIQUE','DEFAULT','RETURNING','OVER',
      'PARTITION','ROW','ROWS','RANGE','PRECEDING','FOLLOWING','UNBOUNDED','CURRENT',
      'CROSS','NATURAL','USING','EXCEPT','INTERSECT','RECURSIVE','LATERAL','FILTER',
      'WITHIN','FETCH','NEXT','FIRST','ONLY','TRUE','FALSE','NULLS','LAST','TIES'
    ]);
    const FUNCTIONS = new Set([
      'COUNT','SUM','AVG','MIN','MAX','COALESCE','NULLIF','CAST','CONVERT','CONCAT',
      'SUBSTRING','SUBSTR','UPPER','LOWER','TRIM','LTRIM','RTRIM','LENGTH',
      'CHAR_LENGTH','DATE','YEAR','MONTH','DAY','NOW','CURRENT_DATE',
      'CURRENT_TIMESTAMP','EXTRACT','TO_DATE','TO_CHAR','TO_NUMBER','ROUND','FLOOR',
      'CEIL','CEILING','ABS','MOD','POWER','SQRT','ISNULL','IFNULL','NVL','REPLACE',
      'SPLIT_PART','POSITION','STRPOS','ARRAY_AGG','STRING_AGG','LISTAGG','JSON_AGG',
      'RANK','DENSE_RANK','ROW_NUMBER','LEAD','LAG','FIRST_VALUE','LAST_VALUE',
      'NTILE','PERCENT_RANK','CUME_DIST','GENERATE_SERIES','UNNEST','DATE_TRUNC',
      'DATE_PART','AGE','DATEDIFF','DATEADD','GETDATE','GREATEST','LEAST',
      'REGEXP_REPLACE','REGEXP_MATCH','IIF','DECODE'
    ]);

    const esc = (s: string) =>
      s.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;');

    // Ordered alternation: comments → strings → identifiers → any char.
    // \w+ matches integers; classify() detects numbers via first-char check.
    const TOKEN_RE = /--.*|\/\*[^]*?\*\/|'[^']*'|"[^"]*"|\w+|[^]/g;

    const classify = (token: string): string => {
      const ch = token[0];
      if (ch === '-' || ch === '/') return `<span class="sql-comment">${esc(token)}</span>`;
      if (ch === "'") return `<span class="sql-string">${esc(token)}</span>`;
      if (ch === '"') return `<span class="sql-identifier">${esc(token)}</span>`;
      if (ch >= '0' && ch <= '9') return `<span class="sql-number">${esc(token)}</span>`;
      const upper = token.toUpperCase();
      if (KEYWORDS.has(upper)) return `<span class="sql-keyword">${esc(token)}</span>`;
      if (FUNCTIONS.has(upper)) return `<span class="sql-function">${esc(token)}</span>`;
      return esc(token);
    };

    // Wrap each line so CSS counters can add line numbers.
    // Join with no separator — display:block on .sql-line handles the line breaks,
    // so we avoid doubling the gap with a stray \n in the <pre>.
    const highlighted = sql.replaceAll(TOKEN_RE, classify);
    const numbered = highlighted.split('\n')
      .map(line => `<span class="sql-line">${line}</span>`)
      .join('');
    // Safe: all SQL content is HTML-escaped via esc(); only hardcoded <span> tags with
    // static class names are injected.
    return this.sanitizer.bypassSecurityTrustHtml(numbered);
  }

  ngOnDestroy(): void {
    // Clean up event listeners and timeouts
    window.removeEventListener('resize', this.resizeListener);
    document.removeEventListener('click', this.handleDocumentClick);
    clearTimeout(this.resizeTimeout);
  }

}
