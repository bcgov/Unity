import { Component, ViewChild, ElementRef, NgZone, OnInit, OnDestroy } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { CommonModule } from '@angular/common';
import { Embed } from './embed';
import { Turn } from './turn';
import { SqlExplanationComponent } from './sql-explanation/sql-explanation';
import { ToastComponent } from './toast/toast.component';
import { AuthService } from './services/auth.service';
import { ApiService } from './services/api.service';
import { ToastService } from './services/toast.service';
import { IframeDetectorService } from './iframe-detector.service';
import { SidebarComponent, Chat } from './sidebar/sidebar';
import { environment } from '../environments/environment';

@Component({
  selector: 'app-root',
  imports: [CommonModule, FormsModule, SqlExplanationComponent, SidebarComponent, ToastComponent],
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class App implements OnInit, OnDestroy {
  protected title = 'recap';
  protected api_url = environment.apiUrl;
  question: string = "";
  conversation: Turn[] = [];
  sidebarOpen: boolean = true;
  currentChatId: string | null = null;
  currentTurnIndex: number = 0;
  visualizationDropdownOpen: boolean = false;
  selectedVisualization: string = 'table';

  constructor(
    private readonly sanitizer: DomSanitizer,
    private readonly authService: AuthService,
    private readonly apiService: ApiService,
    private readonly toastService: ToastService,
    private readonly iframeDetector: IframeDetectorService
  ) {}

  @ViewChild('turnsContainer') private turnsContainer!: ElementRef<HTMLDivElement>;
  @ViewChild('sqlAnimationContainer') private sqlAnimationContainer!: ElementRef<HTMLDivElement>;
  @ViewChild('sidebar') private sidebar!: SidebarComponent;

  async ngOnInit(): Promise<void> {
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
  private resizeListener = () => this.onWindowResize();
  private handleDocumentClick = (event: Event) => this.onDocumentClick(event);

  private onDocumentClick(event: Event): void {
    const target = event.target as HTMLElement;
    const dropdownElement = target.closest('.visualization-dropdown');
    
    // If click is outside the dropdown, close it
    if (!dropdownElement && this.visualizationDropdownOpen) {
      this.visualizationDropdownOpen = false;
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
        console.error('Failed to generate SQL explanation:', error);

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
  }

  private observerMap = new Map<any, IntersectionObserver>();

  // In your component class, add a method:
  onIframeLoad(turn: any) {
    // Start observing the iframe for content stability
    this.observeIframeStability(turn);
  }

  private observeIframeStability(turn: any): void {
    // Clean up any existing observer for this turn
    if (this.observerMap.has(turn)) {
      this.observerMap.get(turn)?.disconnect();
      this.observerMap.delete(turn);
    }

    // Find the iframe element for this turn
    const iframeElement = document.querySelector(`iframe[src*="${turn.embed?.card_id}"]`) as HTMLIFrameElement;
    if (!iframeElement) {
      // Fallback to timeout if iframe not found
      setTimeout(() => turn.iframeLoaded = true, 2000);
      return;
    }

    let stableCount = 0;
    let lastHeight = 0;
    let lastWidth = 0;
    const requiredStableChecks = 3;
    const checkInterval = 500;

    const checkStability = () => {
      try {
        // Check if iframe dimensions have stabilized
        const currentHeight = iframeElement.offsetHeight;
        const currentWidth = iframeElement.offsetWidth;

        // Also check if the iframe has actual content by looking at its document
        let hasContent = false;
        try {
          const iframeDoc = iframeElement.contentDocument || iframeElement.contentWindow?.document;
          if (iframeDoc && iframeDoc.body) {
            // Check if there's meaningful content (not just loading states)
            const bodyContent = iframeDoc.body.innerText || '';
            const hasElements = iframeDoc.body.children.length > 0;
            hasContent = bodyContent.length > 10 || hasElements;
          }
        } catch (crossOriginError) {
          // Cross-origin iframe, can't access content - rely on dimensions only
          hasContent = true;
        }

        if (currentHeight === lastHeight && currentWidth === lastWidth && currentHeight > 0 && hasContent) {
          stableCount++;
          if (stableCount >= requiredStableChecks) {
            // Content appears stable and has actual content
            turn.iframeLoaded = true;
            clearInterval(stabilityChecker);
            return;
          }
        } else {
          stableCount = 0;
        }

        lastHeight = currentHeight;
        lastWidth = currentWidth;
      } catch (error) {
        // If we can't access iframe properties, fall back to timeout
        clearInterval(stabilityChecker);
        setTimeout(() => turn.iframeLoaded = true, 1000);
      }
    };

    // Check stability every 500ms
    const stabilityChecker = setInterval(checkStability, checkInterval);

    // Maximum timeout as fallback (10 seconds)
    setTimeout(() => {
      clearInterval(stabilityChecker);
      if (!turn.iframeLoaded) {
        turn.iframeLoaded = true;
      }
    }, 10000);
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
        console.error('Invalid or missing Metabase URL');
        this.toastService.error('Unable to open Metabase - invalid configuration');
        return null;
      }

      // Validate card ID
      if (!cardId || !this.isValidCardId(cardId)) {
        console.error('Invalid card ID');
        this.toastService.error('Unable to open Metabase - invalid card ID');
        return null;
      }

      // Construct the URL
      const fullUrl = `${metabaseUrl}/question/${cardId}`;

      // Additional validation to ensure the constructed URL is still safe
      if (!this.isValidRedirectUrl(fullUrl, metabaseUrl)) {
        console.error('Invalid redirect URL constructed');
        this.toastService.error('Unable to open Metabase - security validation failed');
        return null;
      }

      return window.open(fullUrl, '_blank');
    } catch (error) {
      console.error('Error redirecting to Metabase:', error);
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

  async deleteQuestion(turn: Turn) {
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
          console.error('Error deleting empty chat:', deleteError);
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
      this.updateDropdownSelection();
      
      // Save the updated conversation to the database
      await this.saveChat();
      
      // Show success toast for individual question deletion
      this.toastService.success('Question deleted successfully');
      
    } catch (error) {
      // Show error toast
      console.error('Error deleting question:', error);
      this.toastService.error('Failed to delete question. Please try again.');
    }
  }

  async changeDisplay(turn: Turn, mode: string) {
    try {
      const res = await firstValueFrom(
        this.apiService.changeDisplay<Embed>(turn.embed.card_id, mode, turn.embed.x_field, turn.embed.y_field)
      );
      turn.safeUrl = this.sanitizer.bypassSecurityTrustResourceUrl(res.url + '&cb=' + Date.now());
      // Update the current visualization in the embed
      turn.embed.current_visualization = mode;
      // turn.embed = res;
    } catch (error) {
      // Handle error silently
    }
  }

  async resetConversation() {
    this.conversation = [];
    this.currentTurnIndex = 0;
  }

  async askQuestion() {
    if (this.question.trim() === "") {
      alert("Please enter a question.");
      return;
    }
    
    // Always reset to table visualization for new questions
    this.selectedVisualization = 'table';
    
    const turn = {question: this.question.trim(), embed: {"url": "", "card_id": 0, "x_field": "", "y_field": "", "title": "", "visualization_options": [], "SQL": ""}, safeUrl: 'loading' as 'loading' | 'failure' | SafeResourceUrl, iframeLoaded: false, sqlPanelOpen: false, sql_explanation: "", sql_explanation_visible: false} as Turn;
    this.conversation.push(turn);
    
    // Set the new turn as the current turn for navigation
    this.currentTurnIndex = this.conversation.length - 1;
    
    this.scrollToBottom();   
    this.question = "";
    try {
      if (! await this.authService.isAuthenticated()) throw new Error('Not authenticated');
      
      turn.embed = await firstValueFrom(
        this.apiService.askQuestion<Embed>(turn.question, this.conversation)
      );

      if (turn.embed.url == "fail") {
        throw new Error;
      }
      
      // New questions always start as table view
      turn.safeUrl = this.sanitizer.bypassSecurityTrustResourceUrl(turn.embed.url);
      turn.embed.current_visualization = 'table';
      
      await this.saveChat();
    } catch (error) {
      turn.iframeLoaded = true;
      turn.safeUrl = "failure";
      // Error is handled by setting failure state
    }
  }

  retryQuestion(turn: Turn): void {
    // Reset the turn state and retry the question
    turn.safeUrl = "loading";
    turn.iframeLoaded = false;
    turn.sqlPanelOpen = false;
    
    // Retry the question with the existing turn
    this.question = turn.question;
    this.conversation = this.conversation.filter(t => t !== turn);
    this.askQuestion();
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
        safeUrl: turn.embed?.url ? this.sanitizer.bypassSecurityTrustResourceUrl(turn.embed.url) : 'failure',
        iframeLoaded: !turn.embed?.url, // If no URL (failure), mark as loaded
        sql_explanation_visible: turn.sql_explanation_visible || false // Preserve visibility state or default to false
      }));
      
      this.currentChatId = chatId;
      this.currentTurnIndex = Math.max(0, this.conversation.length - 1);
      
      // Update dropdown selection based on the current turn
      this.updateDropdownSelection();
      
      // Scroll to bottom instantly after loading chat
      this.scrollToBottomInstant();
    } catch (error) {
      // Handle error silently
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
      // Handle error silently
    }
  }

  scrollToPreviousTurn(): void {
    if (this.currentTurnIndex > 0) {
      this.currentTurnIndex--;
      this.scrollToTurn(this.currentTurnIndex);
      this.updateDropdownSelection();
    }
  }

  scrollToNextTurn(): void {
    if (this.currentTurnIndex < this.conversation.length - 1) {
      this.currentTurnIndex++;
      this.scrollToTurn(this.currentTurnIndex);
      this.updateDropdownSelection();
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

  toggleVisualizationDropdown(): void {
    // Only toggle if there are other options available
    if (this.hasOtherVisualizationOptions()) {
      this.visualizationDropdownOpen = !this.visualizationDropdownOpen;
    }
  }

  async selectVisualization(type: string): Promise<void> {
    this.selectedVisualization = type;
    this.visualizationDropdownOpen = false;
    
    // If we have a current turn, apply the visualization change immediately
    if (this.conversation.length > 0 && this.currentTurnIndex >= 0 && this.currentTurnIndex < this.conversation.length) {
      const currentTurn = this.conversation[this.currentTurnIndex];
      if (currentTurn.embed && currentTurn.embed.card_id) {
        await this.changeDisplay(currentTurn, type);
        // Save the chat to persist the visualization change
        await this.saveChat();
      }
    }
  }

  getVisualizationDisplayName(type: string): string {
    const names: { [key: string]: string } = {
      'table': 'Table',
      'bar': 'Bar Chart',
      'line': 'Line Chart',
      'pie': 'Pie Chart',
      'map': 'Map'
    };
    return names[type] || type;
  }

  getAvailableVisualizationOptions(): string[] {
    // Always include table as it's the default
    const options = ['table'];
    const all_options = ['bar', 'line', 'pie', 'map'];
    
    // If we have a conversation, get options from the current turn
    if (this.conversation.length > 0 && this.currentTurnIndex >= 0 && this.currentTurnIndex < this.conversation.length) {
      const currentTurn = this.conversation[this.currentTurnIndex];
      
      if (currentTurn.embed?.visualization_options) {
        // Add available options from the current turn, avoiding duplicates
        currentTurn.embed.visualization_options.forEach(option => {
          if (!options.includes(option) && all_options.includes(option)) {
            options.push(option);
          }
        });
      }
    } else {
      // If no conversation yet, show all possible options
      options.push(...all_options);
    }
    
    return options;
  }

  hasOtherVisualizationOptions(): boolean {
    const availableOptions = this.getAvailableVisualizationOptions();
    return availableOptions.some(option => option !== this.selectedVisualization);
  }

  updateDropdownSelection(): void {
    if (this.conversation.length > 0 && this.currentTurnIndex >= 0 && this.currentTurnIndex < this.conversation.length) {
      const currentTurn = this.conversation[this.currentTurnIndex];
      if (currentTurn.embed?.current_visualization) {
        this.selectedVisualization = currentTurn.embed.current_visualization;
      } else if (currentTurn.embed) {
        // For older chats without current_visualization, default to table and save it
        this.selectedVisualization = 'table';
        currentTurn.embed.current_visualization = 'table';
      } else {
        // Default to table if no embed data
        this.selectedVisualization = 'table';
      }
    } else {
      // No conversation or invalid turn index
      this.selectedVisualization = 'table';
    }
  }

  ngOnDestroy(): void {
    // Clean up event listeners and timeouts
    window.removeEventListener('resize', this.resizeListener);
    document.removeEventListener('click', this.handleDocumentClick);
    clearTimeout(this.resizeTimeout);
  }

}
