import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { ApiService } from '../services/api.service';

interface FeedbackItem {
  feedback_id: string;
  chat_id: string;
  user_id: string;
  tenant_id: string;
  feedback_type: string;
  message: string;
  user_agent: string;
  metadata: any;
  status: string;
  created_at: string;
  updated_at: string;
  chat_title: string;
  current_question: string;
  current_sql: string;
  current_sql_explanation: string;
  previous_question: string;
  previous_sql: string;
  previous_sql_explanation: string;
}

interface FeedbackResponse {
  feedback: FeedbackItem[];
  limit: number;
  offset: number;
  count: number;
}

interface FeedbackSummary {
  total: number;
  open: number;
  inProgress: number;
  resolved: number;
}

@Component({
  selector: 'app-admin',
  imports: [CommonModule, FormsModule],
  templateUrl: './admin.component.html',
  styleUrls: ['./admin.component.css']
})
export class AdminComponent implements OnInit {
  feedbackList: FeedbackItem[] = [];
  filteredFeedback: FeedbackItem[] = [];
  isLoading = false;
  errorMessage = '';
  selectedStatus = '';
  selectedType = '';
  feedbackSummary: FeedbackSummary | null = null;

  constructor(
    private readonly router: Router,
    private readonly authService: AuthService,
    private readonly apiService: ApiService
  ) {}

  async ngOnInit(): Promise<void> {
    await this.loadFeedback();
  }

  async loadFeedback(): Promise<void> {
    this.isLoading = true;
    this.errorMessage = '';

    try {
      const response = await firstValueFrom(
        this.apiService.getAllFeedback<FeedbackResponse>()
      );

      this.feedbackList = response.feedback;
      this.filteredFeedback = [...this.feedbackList];
      this.calculateSummary();
      this.filterFeedback();

    } catch (error) {
      console.error('Error loading feedback:', error);
      this.errorMessage = 'Failed to load feedback. Please try again.';
    } finally {
      this.isLoading = false;
    }
  }

  filterFeedback(): void {
    this.filteredFeedback = this.feedbackList.filter(feedback => {
      const statusMatch = !this.selectedStatus || feedback.status === this.selectedStatus;
      const typeMatch = !this.selectedType || feedback.feedback_type === this.selectedType;
      return statusMatch && typeMatch;
    });
  }

  calculateSummary(): void {
    this.feedbackSummary = {
      total: this.feedbackList.length,
      open: this.feedbackList.filter(f => f.status === 'open').length,
      inProgress: this.feedbackList.filter(f => f.status === 'in_progress').length,
      resolved: this.feedbackList.filter(f => f.status === 'resolved').length
    };
  }

  async updateFeedbackStatus(feedbackId: string, event: Event): Promise<void> {
    const target = event.target as HTMLSelectElement;
    const newStatus = target.value;

    // Define variables before try block so they're accessible in catch block
    const feedback = this.feedbackList.find(f => f.feedback_id === feedbackId);
    const originalStatus = feedback?.status;

    try {
      // Update in the local array immediately for better UX
      if (feedback) {
        feedback.status = newStatus;
        this.calculateSummary();
        this.filterFeedback();
      }

      // Call API to update status in backend
      await firstValueFrom(this.apiService.updateFeedbackStatus(feedbackId, newStatus));
      console.log(`Feedback ${feedbackId} status updated to ${newStatus}`);

    } catch (error) {
      console.error('Error updating feedback status:', error);
      // Revert the change on error
      if (feedback && originalStatus) {
        feedback.status = originalStatus;
        this.calculateSummary();
        this.filterFeedback();
      }
      this.errorMessage = 'Failed to update feedback status. Please try again.';
    }
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
  }

  formatMetadata(metadata: any): string {
    if (!metadata) return '';
    return JSON.stringify(metadata, null, 2);
  }

  /**
   * Extract token information from feedback
   * Returns token counts if available
   */
  getTokenInfo(feedback: FeedbackItem): { prompt: number; completion: number; total: number } | null {
    // Token information is extracted from the conversation by the backend
    // and included directly in the feedback object
    const feedbackWithTokens = feedback as any;
    if (feedbackWithTokens.tokens) {
      return {
        prompt: feedbackWithTokens.tokens.prompt_tokens || 0,
        completion: feedbackWithTokens.tokens.completion_tokens || 0,
        total: feedbackWithTokens.tokens.total_tokens || 0
      };
    }
    return null;
  }

  /**
   * Format token info for display
   */
  formatTokens(tokens: { prompt: number; completion: number; total: number } | null): string {
    if (!tokens) return 'N/A';
    return `${tokens.total} (${tokens.prompt} in / ${tokens.completion} out)`;
  }

  goToMainApp(): void {
    this.router.navigate(['/app']);
  }
}