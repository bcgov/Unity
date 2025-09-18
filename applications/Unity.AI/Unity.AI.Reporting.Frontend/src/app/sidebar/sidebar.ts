import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../services/api.service';
import { ToastService } from '../services/toast.service';
import { AlertComponent } from '../alert/alert';

export interface Chat {
  id: string;
  title: string;
  created_at: Date;
  updated_at: Date;
}

@Component({
  selector: 'app-sidebar',
  imports: [CommonModule, FormsModule, AlertComponent],
  templateUrl: './sidebar.html',
  styleUrls: ['./sidebar.css']
})
export class SidebarComponent {
  @Input() isOpen: boolean = false;
  @Input() currentChatId: string | null = null;
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

  constructor(
    private apiService: ApiService,
    private toastService: ToastService
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
      this.chats = [];
    } finally {
      this.loading = false;
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
      return dateString.toString(); // Fallback to original string
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
      // Show error toast
      this.toastService.error('Failed to delete report. Please try again.');
    }

    this.cancelDelete();
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
      const response = await firstValueFrom(
        this.apiService.submitFeedback<any>(
          this.currentChatId,
          'bug_report',
          this.feedbackMessage.trim()
        )
      );

      console.log('Feedback submitted successfully:', response.feedback_id);
      this.closeFeedbackModal();
      
      // Show success toast
      const messageText = this.feedbackMessage.trim() 
        ? 'Bug report submitted with your feedback, thank you'
        : 'Bug report submitted successfully, thank you';
      this.toastService.success(messageText);
      
    } catch (error: any) {
      console.error('Failed to submit feedback:', error);
      
      // Show error toast with specific message
      let errorMessage = 'Failed to submit bug report. Please try again.';
      if (error?.error?.message) {
        errorMessage = error.error.message;
      } else if (error?.message) {
        errorMessage = error.message;
      }
      
      this.toastService.error(errorMessage);
      // Keep the modal open so user can retry
    }
  }

  openInfoModal(): void {
    this.showInfoModal = true;
  }

  closeInfoModal(): void {
    this.showInfoModal = false;
  }
}