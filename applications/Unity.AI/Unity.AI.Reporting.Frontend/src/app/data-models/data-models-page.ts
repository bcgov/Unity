import { Component, ChangeDetectorRef, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../services/api.service';
import { ToastService } from '../services/toast.service';
import { LoggerService } from '../services/logger.service';
import {
  ViewInfo,
  CoreField,
  ModelProposal,
  CreatedModel,
  ModelError,
  ExistingModelSummary,
  ExistingModelDetail,
  ModelsModalStep,
} from '../sidebar/sidebar';
import { CardData } from '../embed';

type Stage = 'mode' | 'select' | 'review' | 'done';

@Component({
  selector: 'app-data-models-page',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './data-models-page.html',
  styleUrls: ['./data-models-page.css'],
})
export class DataModelsPageComponent implements OnInit {
  step: ModelsModalStep = 'pick-mode';

  private _availableViews: ViewInfo[] = [];
  // Buckets precomputed once per assignment (see availableViews setter) so the
  // template's rail counts / tab lists don't re-filter on every change-detection.
  formViews: ViewInfo[] = [];
  worksheetViews: ViewInfo[] = [];
  scoresheetViews: ViewInfo[] = [];
  otherViews: ViewInfo[] = [];

  get availableViews(): ViewInfo[] { return this._availableViews; }
  set availableViews(views: ViewInfo[]) {
    this._availableViews = views;
    this.formViews = views.filter(v => v.source_type === 'form_view');
    this.worksheetViews = views.filter(v => v.source_type === 'worksheet_view');
    this.scoresheetViews = views.filter(v => v.source_type === 'scoresheet_view');
    this.otherViews = views.filter(v => v.source_type === 'other_view');
  }

  selectedViews: ViewInfo[] = [];
  activeViewTab: 'form_view' | 'worksheet_view' | 'scoresheet_view' | 'other_view' = 'form_view';
  viewSearch = '';
  modelProposal: ModelProposal | null = null;
  createdModels: CreatedModel[] = [];
  modelErrors: ModelError[] = [];
  existingModels: ExistingModelSummary[] = [];
  selectedExistingModel: ExistingModelDetail | null = null;
  existingPreviewExpanded = false;
  reviewPreviewExpanded = false;
  editPrompt = '';
  editAdditionalViews: ViewInfo[] = [];
  availableCoreFields: CoreField[] = [];
  selectedCoreFields: string[] = [];
  initialCoreFields: string[] = [];
  expandedFormGroups = new Set<string>();
  selectedFormVersions = new Map<string, string[]>();
  showFullError = false;

  get friendlyError(): string {
    const raw = this.modelProposal?.error || '';
    if (!raw) return '';
    const m = raw.match(/"error"\s*:\s*"([^"]+)"/);
    if (m && m[1]) return m[1].replace(/\\n/g, ' ').trim();
    return raw.length > 240 ? raw.slice(0, 240) + '…' : raw;
  }

  toggleFullError(): void { this.showFullError = !this.showFullError; }

  get tabViews(): ViewInfo[] {
    switch (this.activeViewTab) {
      case 'form_view': return this.formViews;
      case 'worksheet_view': return this.worksheetViews;
      case 'scoresheet_view': return this.scoresheetViews;
      default: return this.otherViews;
    }
  }

  private readonly sourceTypeLabels: Record<string, string> = {
    form_view: 'Form',
    worksheet_view: 'Worksheet',
    scoresheet_view: 'Scoresheet',
    other_view: 'Other',
  };

  categoryLabel(v: ViewInfo): string {
    return this.sourceTypeLabels[v.source_type ?? 'other_view'] ?? 'Other';
  }

  get searchActive(): boolean { return this.viewSearch.trim().length > 0; }

  get filteredViews(): ViewInfo[] {
    const q = this.viewSearch.trim().toLowerCase();
    if (!q) return [];
    return this.availableViews.filter(v =>
      (v.display_name || '').toLowerCase().includes(q) ||
      (v.view_name || '').toLowerCase().includes(q)
    );
  }

  constructor(
    private readonly apiService: ApiService,
    private readonly toastService: ToastService,
    private readonly logger: LoggerService,
    private readonly cdr: ChangeDetectorRef,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.step = 'pick-mode';
  }

  // ----- Stepper visualization -----

  get stage(): Stage {
    switch (this.step) {
      case 'pick-mode':
        return 'mode';
      case 'loading-views':
      case 'pick-view':
      case 'loading-models':
      case 'pick-existing-model':
      case 'edit-existing':
        return 'select';
      case 'generating':
      case 'review':
      case 'creating':
        return 'review';
      case 'done':
        return 'done';
      default:
        return 'mode';
    }
  }

  isStageActive(s: Stage): boolean { return this.stage === s; }

  isStageComplete(s: Stage): boolean {
    const order: Stage[] = ['mode', 'select', 'review', 'done'];
    return order.indexOf(s) < order.indexOf(this.stage);
  }

  get pageSubtitle(): string {
    switch (this.step) {
      case 'pick-mode': return 'Choose how you want to start.';
      case 'loading-views': return 'Discovering available views…';
      case 'loading-models': return 'Loading existing models…';
      case 'pick-view': return 'Select one or more views to combine into a model.';
      case 'pick-existing-model': return 'Pick a model to use as the starting point.';
      case 'edit-existing': return 'Describe changes, add views, or adjust core fields.';
      case 'generating': return 'AI is assembling your data model…';
      case 'review': return 'Review the proposed model. You can edit the name and description before saving.';
      case 'creating': return 'Publishing to Metabase…';
      case 'done': return 'All done.';
      default: return '';
    }
  }

  // ----- Mode picker -----

  async chooseModeCreate(): Promise<void> {
    this.step = 'loading-views';
    this.cdr.markForCheck();

    try {
      const [viewsResponse, coreFieldsResponse] = await Promise.all([
        firstValueFrom(this.apiService.getDataModelViews<{ views: ViewInfo[] }>()),
        firstValueFrom(this.apiService.getDataModelCoreFields<{ core_fields: CoreField[] }>()),
      ]);
      this.availableViews = viewsResponse.views || [];
      this.viewSearch = '';
      this.availableCoreFields = coreFieldsResponse.core_fields || [];
      this.selectedCoreFields = this.availableCoreFields
        .filter(cf => cf.default_selected)
        .map(cf => cf.name);
      if (this.formViews.length > 0) this.activeViewTab = 'form_view';
      else if (this.worksheetViews.length > 0) this.activeViewTab = 'worksheet_view';
      else if (this.scoresheetViews.length > 0) this.activeViewTab = 'scoresheet_view';
      else this.activeViewTab = 'other_view';
      this.step = 'pick-view';
    } catch (error) {
      this.logger.error('Failed to load views:', error);
      this.toastService.error('Failed to load available views. Please try again.');
      this.step = 'pick-mode';
    } finally {
      this.cdr.markForCheck();
    }
  }

  async chooseModeModify(): Promise<void> {
    this.step = 'loading-models';
    this.cdr.markForCheck();

    try {
      const response = await firstValueFrom(
        this.apiService.listDataModels<{ models: ExistingModelSummary[] }>()
      );
      this.existingModels = response.models || [];
      this.step = 'pick-existing-model';
    } catch (error) {
      this.logger.error('Failed to load existing models:', error);
      this.toastService.error('Failed to load existing models. Please try again.');
      this.step = 'pick-mode';
    } finally {
      this.cdr.markForCheck();
    }
  }

  // ----- Core fields -----

  toggleCoreField(name: string): void {
    if (this.isCoreFieldLocked(name)) return;
    const idx = this.selectedCoreFields.indexOf(name);
    if (idx >= 0) this.selectedCoreFields.splice(idx, 1);
    else this.selectedCoreFields.push(name);
  }

  isCoreFieldSelected(name: string): boolean {
    return this.selectedCoreFields.includes(name) || this.isCoreFieldLocked(name);
  }

  isCoreFieldLocked(name: string): boolean {
    return name === 'ReferenceNo'
      && (this.selectedViews.length > 1 || this.editAdditionalViews.length > 0);
  }

  private effectiveCoreFields(): string[] {
    const out = [...this.selectedCoreFields];
    const combining = this.selectedViews.length > 1 || this.editAdditionalViews.length > 0;
    if (combining && !out.includes('ReferenceNo')) out.unshift('ReferenceNo');
    return out;
  }

  private coreFieldsDiffer(): boolean {
    const a = [...this.selectedCoreFields].sort();
    const b = [...this.initialCoreFields].sort();
    return a.length !== b.length || a.some((v, i) => v !== b[i]);
  }

  // ----- View selection (create flow) -----

  selectView(view: ViewInfo): void {
    const idx = this.selectedViews.findIndex(v => v.view_name === view.view_name);
    if (idx >= 0) {
      this.selectedViews.splice(idx, 1);
      this.expandedFormGroups.delete(view.view_name);
      this.selectedFormVersions.delete(view.view_name);
    } else {
      this.selectedViews.push(view);
      if (view.versions && view.versions.length > 0) {
        this.selectedFormVersions.set(view.view_name, view.versions.map(v => v.table_name));
      }
    }
  }

  isViewSelected(view: ViewInfo): boolean {
    return this.selectedViews.some(v => v.view_name === view.view_name);
  }

  toggleFormExpand(view: ViewInfo, event: Event): void {
    event.stopPropagation();
    if (this.expandedFormGroups.has(view.view_name)) this.expandedFormGroups.delete(view.view_name);
    else this.expandedFormGroups.add(view.view_name);
  }

  isFormExpanded(view: ViewInfo): boolean {
    return this.expandedFormGroups.has(view.view_name);
  }

  toggleFormVersion(view: ViewInfo, tableName: string, event: Event): void {
    event.stopPropagation();
    const selected = this.selectedFormVersions.get(view.view_name) || [];
    const idx = selected.indexOf(tableName);
    if (idx >= 0) selected.splice(idx, 1);
    else selected.push(tableName);
    this.selectedFormVersions.set(view.view_name, selected);
  }

  isFormVersionSelected(view: ViewInfo, tableName: string): boolean {
    const selected = this.selectedFormVersions.get(view.view_name);
    return selected ? selected.includes(tableName) : false;
  }

  private getSelectedVersionsForView(view: ViewInfo): string[] | undefined {
    if (!view.versions || view.versions.length === 0) return undefined;
    const selected = this.selectedFormVersions.get(view.view_name);
    if (!selected || selected.length === view.versions.length) return undefined;
    return selected;
  }

  get generateButtonLabel(): string {
    const n = this.selectedViews.length;
    if (n <= 1) return 'Generate Model →';
    return `Generate Combined Model (${n}) →`;
  }

  async generateModelForView(): Promise<void> {
    if (this.selectedViews.length === 0) return;
    this.step = 'generating';
    this.modelProposal = null;
    this.cdr.markForCheck();

    try {
      type PreviewResponse = { proposal: Omit<ModelProposal, 'sqlExpanded'> };
      const viewNames = this.selectedViews.map(v => v.view_name);
      const coreFields = this.effectiveCoreFields();
      const selectedVersions = viewNames.length === 1
        ? this.getSelectedVersionsForView(this.selectedViews[0])
        : undefined;
      const obs = viewNames.length === 1
        ? this.apiService.previewDataModel<PreviewResponse>(viewNames[0], coreFields, selectedVersions)
        : this.apiService.previewCombinedModel<PreviewResponse>(viewNames, coreFields);
      const response = await firstValueFrom(obs);
      this.modelProposal = { ...response.proposal, sqlExpanded: true };
      this.step = 'review';
    } catch (error) {
      this.logger.error('Failed to generate model:', error);
      this.toastService.error('Failed to generate model. Please try again.');
      this.step = 'pick-view';
    } finally {
      this.cdr.markForCheck();
    }
  }

  async createModel(): Promise<void> {
    if (!this.modelProposal || !this.modelProposal.valid) return;
    const { name, description, sql } = this.modelProposal;
    this.step = 'creating';
    this.cdr.markForCheck();

    try {
      const response = await firstValueFrom(
        this.apiService.createDataModels<{ models: CreatedModel[]; errors: ModelError[] }>(
          [{ name, description, sql }]
        )
      );
      this.createdModels = response.models || [];
      this.modelErrors = response.errors || [];
      this.step = 'done';

      if (this.createdModels.length > 0) {
        this.toastService.success(`Model "${name}" created successfully`);
      }
      if (this.modelErrors.length > 0) {
        this.toastService.error('Failed to create model');
      }
    } catch (error) {
      this.logger.error('Failed to create model:', error);
      this.toastService.error('Failed to create model. Please try again.');
      this.step = 'review';
    } finally {
      this.cdr.markForCheck();
    }
  }

  toggleProposalSql(): void { /* no-op: SQL pane removed */ }

  regenerateProposal(): void {
    if (this.selectedExistingModel) void this.generateModifiedModel();
    else void this.generateModelForView();
  }

  // ----- Modify flow -----

  async selectExistingModel(summary: ExistingModelSummary): Promise<void> {
    this.step = 'loading-views';
    this.cdr.markForCheck();

    try {
      const [detail, viewsResponse, coreFieldsResponse] = await Promise.all([
        firstValueFrom(this.apiService.getDataModelDetail<ExistingModelDetail>(summary.card_id)),
        this.availableViews.length > 0
          ? Promise.resolve({ views: this.availableViews })
          : firstValueFrom(this.apiService.getDataModelViews<{ views: ViewInfo[] }>()),
        this.availableCoreFields.length > 0
          ? Promise.resolve({ core_fields: this.availableCoreFields })
          : firstValueFrom(this.apiService.getDataModelCoreFields<{ core_fields: CoreField[] }>()),
      ]);

      this.selectedExistingModel = detail;
      this.existingPreviewExpanded = false;
      this.availableViews = viewsResponse.views || [];
      this.availableCoreFields = coreFieldsResponse.core_fields || [];

      const sql = detail.sql || '';
      const detected = this.availableCoreFields
        .filter(cf => new RegExp(`a\\."${cf.name}"`).test(sql))
        .map(cf => cf.name);
      this.selectedCoreFields = [...detected];
      this.initialCoreFields = [...detected];

      if (this.formViews.length > 0) this.activeViewTab = 'form_view';
      else if (this.worksheetViews.length > 0) this.activeViewTab = 'worksheet_view';
      else if (this.scoresheetViews.length > 0) this.activeViewTab = 'scoresheet_view';
      else this.activeViewTab = 'other_view';
      this.editPrompt = '';
      this.editAdditionalViews = [];
      this.viewSearch = '';
      this.step = 'edit-existing';
    } catch (error) {
      this.logger.error('Failed to load model detail:', error);
      this.toastService.error('Failed to load model detail. Please try again.');
      this.step = 'pick-existing-model';
    } finally {
      this.cdr.markForCheck();
    }
  }

  isEditAdditionalViewSelected(view: ViewInfo): boolean {
    return this.editAdditionalViews.some(v => v.view_name === view.view_name);
  }

  toggleEditAdditionalView(view: ViewInfo): void {
    const idx = this.editAdditionalViews.findIndex(v => v.view_name === view.view_name);
    if (idx >= 0) this.editAdditionalViews.splice(idx, 1);
    else this.editAdditionalViews.push(view);
  }

  get canGenerateModified(): boolean {
    return !!(this.editPrompt.trim() || this.editAdditionalViews.length > 0 || this.coreFieldsDiffer());
  }

  async togglePreviewTable(): Promise<void> {
    this.existingPreviewExpanded = !this.existingPreviewExpanded;
    if (!this.existingPreviewExpanded || !this.selectedExistingModel) return;
    if (this.selectedExistingModel.previewData !== undefined) return;

    this.selectedExistingModel.previewLoading = true;
    this.cdr.markForCheck();
    try {
      const data = await firstValueFrom(
        this.apiService.getDataModelPreviewData<CardData>(this.selectedExistingModel.card_id)
      );
      this.selectedExistingModel.previewData = data;
    } catch {
      this.selectedExistingModel.previewData = null;
    } finally {
      this.selectedExistingModel.previewLoading = false;
      this.cdr.markForCheck();
    }
  }

  formatCell(value: unknown): string {
    if (value === null || value === undefined) return '—';
    if (typeof value === 'string') return value;
    if (typeof value === 'number' || typeof value === 'boolean') return String(value);
    return JSON.stringify(value);
  }

  toggleReviewPreview(): void { this.reviewPreviewExpanded = !this.reviewPreviewExpanded; }

  async generateModifiedModel(): Promise<void> {
    if (!this.selectedExistingModel || !this.canGenerateModified) return;
    this.step = 'generating';
    this.modelProposal = null;
    this.cdr.markForCheck();

    try {
      type PreviewResponse = { proposal: Omit<ModelProposal, 'sqlExpanded'> };
      const response = await firstValueFrom(
        this.apiService.modifyDataModelPreview<PreviewResponse>(
          this.selectedExistingModel.card_id,
          this.editPrompt.trim(),
          this.editAdditionalViews.map(v => v.view_name),
          this.effectiveCoreFields(),
        )
      );
      this.modelProposal = { ...response.proposal, sqlExpanded: true };
      this.step = 'review';
    } catch (error) {
      this.logger.error('Failed to generate modified model:', error);
      this.toastService.error('Failed to generate modified model. Please try again.');
      this.step = 'edit-existing';
    } finally {
      this.cdr.markForCheck();
    }
  }

  // ----- Navigation -----

  backFromReview(): void {
    this.reviewPreviewExpanded = false;
    if (this.selectedExistingModel) this.step = 'edit-existing';
    else this.step = 'pick-view';
  }

  exitToApp(): void {
    void this.router.navigate(['/app']);
  }

  startAnother(): void {
    this.availableViews = [];
    this.selectedViews = [];
    this.modelProposal = null;
    this.createdModels = [];
    this.modelErrors = [];
    this.existingModels = [];
    this.selectedExistingModel = null;
    this.existingPreviewExpanded = false;
    this.reviewPreviewExpanded = false;
    this.editPrompt = '';
    this.editAdditionalViews = [];
    this.availableCoreFields = [];
    this.selectedCoreFields = [];
    this.initialCoreFields = [];
    this.expandedFormGroups.clear();
    this.selectedFormVersions.clear();
    this.step = 'pick-mode';
  }
}
