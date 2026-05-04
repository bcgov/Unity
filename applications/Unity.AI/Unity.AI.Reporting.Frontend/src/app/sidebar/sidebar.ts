import { Component, Input, Output, EventEmitter, ChangeDetectorRef } from '@angular/core';

import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../services/api.service';
import { ConfigService } from '../services/config.service';
import { ToastService } from '../services/toast.service';
import { LoggerService } from '../services/logger.service';
import { AlertComponent } from '../alert/alert';
import { Turn } from '../turn';

export interface Chat {
  id: string;
  title: string;
  created_at: Date;
  updated_at: Date;
}

export interface ViewInfo {
  view_name: string;
  column_count: number;
  has_labels: boolean;
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

export type ModelsModalStep = 'idle' | 'loading-views' | 'pick-view' | 'generating' | 'review' | 'creating' | 'done';

@Component({
  selector: 'app-sidebar',
  imports: [FormsModule, AlertComponent],
  templateUrl: './sidebar.html',
  styleUrls: ['./sidebar.css']
})
export class SidebarComponent {
  @Input() isOpen: boolean = false;
  @Input() currentChatId: string | null = null;
  @Input() conversation: Turn[] = [];
  @Input() currentTurnIndex: number = 0;
  @Output() toggleSidebar = new EventEmitter<void>();
  @Output() chatSelected = new EventEmitter<Chat>();
  @Output() newChat = new EventEmitter<void>();

  chats: Chat[] = [];
  loading: boolean = false;
  
  // Alert state
  showDeleteAlert: boolean = false;
  chatToDelete: Chat | null = null;

  // Feedback modal state
  showFeedbackModal: boolean = false;
  feedbackMessage: string = '';

  // Information modal state
  showInfoModal: boolean = false;

  // Data model generation modal state
  modelsModalStep: ModelsModalStep = 'idle';
  availableViews: ViewInfo[] = [];
  selectedView: ViewInfo | null = null;
  modelProposal: ModelProposal | null = null;
  createdModels: CreatedModel[] = [];
  modelErrors: ModelError[] = [];

  constructor(
    private readonly apiService: ApiService,
    private readonly configService: ConfigService,
    private readonly toastService: ToastService,
    private readonly logger: LoggerService,
    private readonly cdr: ChangeDetectorRef
  ) {}

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

  // ----- Data model generation -----

  async openDataModelsModal(): Promise<void> {
    this.modelsModalStep = 'loading-views';
    this.availableViews = [];
    this.selectedView = null;
    this.modelProposal = null;
    this.createdModels = [];
    this.modelErrors = [];
    this.cdr.markForCheck();

    try {
      const response = await firstValueFrom(
        this.apiService.getDataModelViews<{ views: ViewInfo[] }>()
      );
      this.availableViews = response.views || [];
      this.modelsModalStep = 'pick-view';
    } catch (error) {
      this.logger.error('Failed to load views:', error);
      this.toastService.error('Failed to load available views. Please try again.');
      this.modelsModalStep = 'idle';
    } finally {
      this.cdr.markForCheck();
    }
  }

  selectView(view: ViewInfo): void {
    this.selectedView = view;
  }

  async generateModelForView(): Promise<void> {
    if (!this.selectedView) return;

    this.modelsModalStep = 'generating';
    this.modelProposal = null;
    this.cdr.markForCheck();

    try {
      const response = await firstValueFrom(
        this.apiService.previewDataModel<{
          proposal: Omit<ModelProposal, 'sqlExpanded'>;
        }>(this.selectedView.view_name)
      );
      this.modelProposal = {
        ...response.proposal,
        sqlExpanded: false,
      };
      this.modelsModalStep = 'review';
    } catch (error) {
      this.logger.error('Failed to generate model:', error);
      this.toastService.error('Failed to generate model. Please try again.');
      this.modelsModalStep = 'pick-view';
    } finally {
      this.cdr.markForCheck();
    }
  }

  async createModel(): Promise<void> {
    if (!this.modelProposal || !this.modelProposal.valid) return;

    const { name, description, sql } = this.modelProposal;
    this.modelsModalStep = 'creating';
    this.cdr.markForCheck();

    try {
      const response = await firstValueFrom(
        this.apiService.createDataModels<{
          models: CreatedModel[];
          errors: ModelError[];
        }>([{ name, description, sql }])
      );
      this.createdModels = response.models || [];
      this.modelErrors = response.errors || [];
      this.modelsModalStep = 'done';

      if (this.createdModels.length > 0) {
        this.toastService.success(`Model "${name}" created successfully`);
      }
      if (this.modelErrors.length > 0) {
        this.toastService.error(`Failed to create model`);
      }
    } catch (error) {
      this.logger.error('Failed to create model:', error);
      this.toastService.error('Failed to create model. Please try again.');
      this.modelsModalStep = 'review';
    } finally {
      this.cdr.markForCheck();
    }
  }

  toggleProposalSql(): void {
    if (this.modelProposal) {
      this.modelProposal.sqlExpanded = !this.modelProposal.sqlExpanded;
    }
  }

  closeModelsModal(): void {
    this.modelsModalStep = 'idle';
    this.availableViews = [];
    this.selectedView = null;
    this.modelProposal = null;
    this.createdModels = [];
    this.modelErrors = [];
  }

  private extractConversationContext(): any {
    const context: any = {};

    // Get current turn data
    if (this.conversation.length > 0 && this.currentTurnIndex >= 0 && this.currentTurnIndex < this.conversation.length) {
      const currentTurn = this.conversation[this.currentTurnIndex];
      context.currentQuestion = currentTurn.question;
      context.currentSql = currentTurn.embed?.SQL;
      context.currentSqlExplanation = currentTurn.sql_explanation ?? currentTurn.embed?.sql_explanation;
    }

    // Get previous turn data (if exists)
    if (this.currentTurnIndex > 0) {
      const previousTurn = this.conversation[this.currentTurnIndex - 1];
      context.previousQuestion = previousTurn.question;
      context.previousSql = previousTurn.embed?.SQL;
      context.previousSqlExplanation = previousTurn.sql_explanation ?? previousTurn.embed?.sql_explanation;
    }

    return context;
  }
}