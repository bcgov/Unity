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
  display_name: string;
  column_count: number;
  has_labels: boolean;
  source_type?: 'form_view' | 'worksheet_view' | 'scoresheet_view' | 'other_view';
  form_group?: string;
  version?: string;
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

export interface ExistingModelSummary {
  card_id: number;
  name: string;
  description: string;
}

export interface ExistingModelDetail extends ExistingModelSummary {
  sql: string;
  columns: string[];
}

export type ModelsModalStep = 'idle' | 'pick-mode' | 'loading-views' | 'pick-view'
  | 'loading-models' | 'pick-existing-model' | 'edit-existing'
  | 'generating' | 'review' | 'creating' | 'done';

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
  selectedViews: ViewInfo[] = [];
  activeViewTab: 'form_view' | 'worksheet_view' | 'scoresheet_view' | 'other_view' = 'form_view';
  modelProposal: ModelProposal | null = null;
  createdModels: CreatedModel[] = [];
  modelErrors: ModelError[] = [];
  existingModels: ExistingModelSummary[] = [];
  selectedExistingModel: ExistingModelDetail | null = null;
  existingSqlExpanded: boolean = false;
  editPrompt: string = '';
  editAdditionalViews: ViewInfo[] = [];

  get formViews(): ViewInfo[] { return this.availableViews.filter(v => v.source_type === 'form_view'); }
  get worksheetViews(): ViewInfo[] { return this.availableViews.filter(v => v.source_type === 'worksheet_view'); }
  get scoresheetViews(): ViewInfo[] { return this.availableViews.filter(v => v.source_type === 'scoresheet_view'); }
  get otherViews(): ViewInfo[] { return this.availableViews.filter(v => v.source_type === 'other_view'); }
  get tabViews(): ViewInfo[] { return this.availableViews.filter(v => v.source_type === this.activeViewTab); }

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
    this.modelsModalStep = 'pick-mode';
    this.availableViews = [];
    this.selectedViews = [];
    this.modelProposal = null;
    this.createdModels = [];
    this.modelErrors = [];
    this.existingModels = [];
    this.selectedExistingModel = null;
    this.editPrompt = '';
    this.editAdditionalViews = [];
    this.cdr.markForCheck();
  }

  async chooseModeCreate(): Promise<void> {
    this.modelsModalStep = 'loading-views';
    this.cdr.markForCheck();

    try {
      const response = await firstValueFrom(
        this.apiService.getDataModelViews<{ views: ViewInfo[] }>()
      );
      this.availableViews = response.views || [];
      if (this.formViews.length > 0) this.activeViewTab = 'form_view';
      else if (this.worksheetViews.length > 0) this.activeViewTab = 'worksheet_view';
      else if (this.scoresheetViews.length > 0) this.activeViewTab = 'scoresheet_view';
      else this.activeViewTab = 'other_view';
      this.modelsModalStep = 'pick-view';
    } catch (error) {
      this.logger.error('Failed to load views:', error);
      this.toastService.error('Failed to load available views. Please try again.');
      this.modelsModalStep = 'idle';
    } finally {
      this.cdr.markForCheck();
    }
  }

  async chooseModeModify(): Promise<void> {
    this.modelsModalStep = 'loading-models';
    this.cdr.markForCheck();

    try {
      const response = await firstValueFrom(
        this.apiService.listDataModels<{ models: ExistingModelSummary[] }>()
      );
      this.existingModels = response.models || [];
      this.modelsModalStep = 'pick-existing-model';
    } catch (error) {
      this.logger.error('Failed to load existing models:', error);
      this.toastService.error('Failed to load existing models. Please try again.');
      this.modelsModalStep = 'idle';
    } finally {
      this.cdr.markForCheck();
    }
  }

  selectView(view: ViewInfo): void {
    const idx = this.selectedViews.findIndex(v => v.view_name === view.view_name);
    if (idx >= 0) {
      this.selectedViews.splice(idx, 1);
    } else {
      this.selectedViews.push(view);
    }
  }

  isViewSelected(view: ViewInfo): boolean {
    return this.selectedViews.some(v => v.view_name === view.view_name);
  }

  get generateButtonLabel(): string {
    const n = this.selectedViews.length;
    if (n === 0) return 'Generate Model →';
    if (n === 1) return 'Generate Model →';
    return `Generate Combined Model (${n}) →`;
  }

  async generateModelForView(): Promise<void> {
    if (this.selectedViews.length === 0) return;

    this.modelsModalStep = 'generating';
    this.modelProposal = null;
    this.cdr.markForCheck();

    try {
      type PreviewResponse = { proposal: Omit<ModelProposal, 'sqlExpanded'> };
      const viewNames = this.selectedViews.map(v => v.view_name);
      const obs = viewNames.length === 1
        ? this.apiService.previewDataModel<PreviewResponse>(viewNames[0])
        : this.apiService.previewCombinedModel<PreviewResponse>(viewNames);
      const response = await firstValueFrom(obs);
      this.modelProposal = { ...response.proposal, sqlExpanded: false };
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

  /** Re-run the right preview path for whichever flow produced the current proposal. */
  regenerateProposal(): void {
    if (this.selectedExistingModel) {
      void this.generateModifiedModel();
    } else {
      void this.generateModelForView();
    }
  }

  async selectExistingModel(summary: ExistingModelSummary): Promise<void> {
    this.modelsModalStep = 'loading-views';
    this.cdr.markForCheck();

    try {
      // Fetch detail and views in parallel
      const [detail, viewsResponse] = await Promise.all([
        firstValueFrom(
          this.apiService.getDataModelDetail<ExistingModelDetail>(summary.card_id)
        ),
        this.availableViews.length > 0
          ? Promise.resolve({ views: this.availableViews })
          : firstValueFrom(
              this.apiService.getDataModelViews<{ views: ViewInfo[] }>()
            ),
      ]);

      this.selectedExistingModel = detail;
      this.existingSqlExpanded = false;
      this.availableViews = viewsResponse.views || [];
      if (this.formViews.length > 0) this.activeViewTab = 'form_view';
      else if (this.worksheetViews.length > 0) this.activeViewTab = 'worksheet_view';
      else if (this.scoresheetViews.length > 0) this.activeViewTab = 'scoresheet_view';
      else this.activeViewTab = 'other_view';
      this.editPrompt = '';
      this.editAdditionalViews = [];
      this.modelsModalStep = 'edit-existing';
    } catch (error) {
      this.logger.error('Failed to load model detail:', error);
      this.toastService.error('Failed to load model detail. Please try again.');
      this.modelsModalStep = 'pick-existing-model';
    } finally {
      this.cdr.markForCheck();
    }
  }

  isEditAdditionalViewSelected(view: ViewInfo): boolean {
    return this.editAdditionalViews.some(v => v.view_name === view.view_name);
  }

  toggleEditAdditionalView(view: ViewInfo): void {
    const idx = this.editAdditionalViews.findIndex(v => v.view_name === view.view_name);
    if (idx >= 0) {
      this.editAdditionalViews.splice(idx, 1);
    } else {
      this.editAdditionalViews.push(view);
    }
  }

  get canGenerateModified(): boolean {
    return !!(this.editPrompt.trim() || this.editAdditionalViews.length > 0);
  }

  async generateModifiedModel(): Promise<void> {
    if (!this.selectedExistingModel || !this.canGenerateModified) return;

    this.modelsModalStep = 'generating';
    this.modelProposal = null;
    this.cdr.markForCheck();

    try {
      type PreviewResponse = { proposal: Omit<ModelProposal, 'sqlExpanded'> };
      const response = await firstValueFrom(
        this.apiService.modifyDataModelPreview<PreviewResponse>(
          this.selectedExistingModel.card_id,
          this.editPrompt.trim(),
          this.editAdditionalViews.map(v => v.view_name)
        )
      );
      this.modelProposal = { ...response.proposal, sqlExpanded: false };
      this.modelsModalStep = 'review';
    } catch (error) {
      this.logger.error('Failed to generate modified model:', error);
      this.toastService.error('Failed to generate modified model. Please try again.');
      this.modelsModalStep = 'edit-existing';
    } finally {
      this.cdr.markForCheck();
    }
  }

  closeModelsModal(): void {
    this.modelsModalStep = 'idle';
    this.availableViews = [];
    this.selectedViews = [];
    this.activeViewTab = 'form_view';
    this.modelProposal = null;
    this.createdModels = [];
    this.modelErrors = [];
    this.existingModels = [];
    this.selectedExistingModel = null;
    this.existingSqlExpanded = false;
    this.editPrompt = '';
    this.editAdditionalViews = [];
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