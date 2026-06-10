import { Component, Input, Output, EventEmitter, ChangeDetectorRef, OnDestroy } from '@angular/core';

import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../services/api.service';
import { ConfigService } from '../services/config.service';
import { ToastService } from '../services/toast.service';
import { LoggerService } from '../services/logger.service';
import { AuthService } from '../services/auth.service';
import { AlertComponent } from '../alert/alert';
import { CardData } from '../embed';
import { Turn } from '../turn';

export interface Chat {
  id: string;
  title: string;
  created_at: Date;
  updated_at: Date;
}

export interface ViewInfo {
  view_name: string;
  display_name: string;
  column_count: number;
  has_labels: boolean;
  source_type?: 'form_view' | 'worksheet_view' | 'scoresheet_view' | 'other_view';
  form_group?: string;
  version?: string;
  versions?: { table_name: string; version: number }[];
  is_empty?: boolean;
}

export interface CoreField {
  name: string;
  label: string;
  type: 'text' | 'number' | 'date';
  default_selected: boolean;
}

export interface ModelProposal {
  name: string;
  description: string;
  sql: string;
  valid: boolean;
  error?: string | null;
  source_view: string;
  columns: string[];
  excluded_columns: string[];
  preview_data?: CardData | null;  // Real columns + 1 sample row from Metabase (null when SQL invalid)
  sqlExpanded: boolean;  // local UI state
}

export interface CreatedModel {
  name: string;
  description: string;
  card_id: number;
  metabase_url: string;
}

export interface ModelError {
  name: string;
  error: string;
}

export interface ExistingModelSummary {
  card_id: number;
  name: string;
  description: string;
}

export interface ExistingModelDetail extends ExistingModelSummary {
  sql: string;
  columns: string[];
  previewData?: CardData | null;
  previewLoading?: boolean;
}

export type ModelsModalStep = 'idle' | 'pick-mode' | 'loading-views' | 'pick-view'
  | 'loading-models' | 'pick-existing-model' | 'edit-existing'
  | 'generating' | 'review' | 'creating' | 'done';

@Component({
  selector: 'app-sidebar',
  imports: [FormsModule, RouterLink, AlertComponent],
  templateUrl: './sidebar.html',
  styleUrls: ['./sidebar.css']
})
export class SidebarComponent implements OnDestroy {
  @Input() isOpen: boolean = false;
  @Input() currentChatId: string | null = null;
  @Input() conversation: Turn[] = [];
  @Input() currentTurnIndex: number = 0;
  @Output() toggleSidebar = new EventEmitter<void>();
  @Output() chatSelected = new EventEmitter<Chat>();
  @Output() newChat = new EventEmitter<void>();

  chats: Chat[] = [];
  loading: boolean = false;
  sidebarWidth = 220;

  private readonly MIN_WIDTH = 150;
  private readonly MAX_WIDTH = 600;
  private startX = 0;
  private startWidth = 0;

  private readonly onMouseMove = (e: MouseEvent) => {
    const delta = e.clientX - this.startX;
    this.sidebarWidth = Math.min(this.MAX_WIDTH, Math.max(this.MIN_WIDTH, this.startWidth + delta));
    this.cdr.markForCheck();
  };

  private readonly onMouseUp = () => {
    document.removeEventListener('mousemove', this.onMouseMove);
    document.removeEventListener('mouseup', this.onMouseUp);
    document.body.style.userSelect = '';
  };

  onResizeStart(e: MouseEvent): void {
    this.startX = e.clientX;
    this.startWidth = this.sidebarWidth;
    document.body.style.userSelect = 'none';
    document.addEventListener('mousemove', this.onMouseMove);
    document.addEventListener('mouseup', this.onMouseUp);
    e.preventDefault();
  }

  ngOnDestroy(): void {
    document.removeEventListener('mousemove', this.onMouseMove);
    document.removeEventListener('mouseup', this.onMouseUp);
  }

  // Alert state
  showDeleteAlert: boolean = false;
  chatToDelete: Chat | null = null;

  // Feedback modal state
  showFeedbackModal: boolean = false;
  feedbackMessage: string = '';

  // Information modal state
  showInfoModal: boolean = false;

  constructor(
    private readonly apiService: ApiService,
    private readonly configService: ConfigService,
    private readonly toastService: ToastService,
    private readonly logger: LoggerService,
    private readonly authService: AuthService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  /** Whether to show the "Generate Data Models" entry point (Create/Edit Data Model permission). */
  get canEditDataModel(): boolean {
    return this.authService.canEditDataModel();
  }

  async ngOnInit(): Promise<void> {
    await this.loadChats();
  }

  public async loadChats(): Promise<void> {
    try {
      // Only show loading if we don't have any chats yet
      if (this.chats.length === 0) {
        this.loading = true;
      }
      
      this.chats = await firstValueFrom(
        this.apiService.getChats<Chat[]>()
      );
    } catch (error) {
      console.error('Failed to load chats:', error);
      this.chats = [];
    } finally {
      this.loading = false;
      this.cdr.markForCheck();
    }
  }

  onToggle(): void {
    this.toggleSidebar.emit();
  }

  onChatSelect(chat: Chat): void {
    this.chatSelected.emit(chat);
  }

  onNewChat(): void {
    this.newChat.emit();
  }

  formatToPST(dateString: Date): string {
    try {
      const date = new Date(dateString);
      
      // Format to PST (Pacific Standard Time)
      const options: Intl.DateTimeFormatOptions = {
        timeZone: 'America/Los_Angeles',
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: 'numeric',
        minute: '2-digit',
        hour12: true
      };
      
      return date.toLocaleString('en-US', options);
    } catch (error) {
      console.warn('Failed to format date:', error);
      return dateString.toString();
    }
  }

  deleteChat(chat: Chat, event: Event): void {
    event.stopPropagation();
    this.chatToDelete = chat;
    this.showDeleteAlert = true;
  }

  async confirmDelete(): Promise<void> {
    if (!this.chatToDelete) return;

    const deletedChatId = this.chatToDelete.id;
    const chatTitle = this.chatToDelete.title;
    
    try {
      await firstValueFrom(
        this.apiService.deleteChat(deletedChatId)
      );

      this.chats = this.chats.filter(c => c.id !== deletedChatId);
      
      // If we deleted the current chat, trigger a new chat
      if (this.currentChatId === deletedChatId) {
        this.newChat.emit();
      }

      // Show success toast
      this.toastService.success(`Report "${chatTitle}" deleted successfully`);
      
    } catch (error) {
      console.error('Failed to delete report:', error);
      this.toastService.error('Failed to delete report. Please try again.');
    }

    this.cancelDelete();
    this.cdr.markForCheck();
  }

  cancelDelete(): void {
    this.showDeleteAlert = false;
    this.chatToDelete = null;
  }

  openFeedbackModal(): void {
    this.showFeedbackModal = true;
    this.feedbackMessage = '';
  }

  closeFeedbackModal(): void {
    this.showFeedbackModal = false;
    this.feedbackMessage = '';
  }

  async submitFeedback(): Promise<void> {
    if (!this.currentChatId) {
      this.toastService.error('No current report to flag');
      return;
    }

    try {
      // Extract conversation context
      const context = this.extractConversationContext();

      const response = await firstValueFrom(
        this.apiService.submitFeedback<any>(
          this.currentChatId,
          'bug_report',
          this.feedbackMessage.trim(),
          context
        )
      );

      this.logger.info('Feedback submitted successfully:', response.feedback_id);
      this.closeFeedbackModal();

      // Show success toast
      const messageText = this.feedbackMessage.trim()
        ? 'Bug report submitted with your feedback, thank you'
        : 'Bug report submitted successfully, thank you';
      this.toastService.success(messageText);

    } catch (error: any) {
      this.logger.error('Failed to submit feedback:', error);
      
      // Show error toast with specific message
      let errorMessage = 'Failed to submit bug report. Please try again.';
      if (error?.error?.message) {
        errorMessage = error.error.message;
      } else if (error?.message) {
        errorMessage = error.message;
      }
      
      this.toastService.error(errorMessage);
      // Keep the modal open so user can retry
    } finally {
      this.cdr.markForCheck();
    }
  }

  openInfoModal(): void {
    this.showInfoModal = true;
  }

  closeInfoModal(): void {
    this.showInfoModal = false;
  }

  get version(): string {
    return this.configService.version;
  }

  get environment(): string {
    return this.configService.environment;
  }

  private extractConversationContext(): any {
    const context: any = {};

    // Get current turn data
    if (this.conversation.length > 0 && this.currentTurnIndex >= 0 && this.currentTurnIndex < this.conversation.length) {
      const currentTurn = this.conversation[this.currentTurnIndex];
      context.currentQuestion = currentTurn.question;
      context.currentSql = currentTurn.embed?.SQL;
      context.currentSqlExplanation = currentTurn.embed?.sql_explanation;
    }

    // Get previous turn data (if exists)
    if (this.currentTurnIndex > 0) {
      const previousTurn = this.conversation[this.currentTurnIndex - 1];
      context.previousQuestion = previousTurn.question;
      context.previousSql = previousTurn.embed?.SQL;
      context.previousSqlExplanation = previousTurn.embed?.sql_explanation;
    }

    return context;
  }
}