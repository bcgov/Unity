import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../services/api.service';
import { AlertComponent } from '../alert/alert';

export interface Chat {
  id: string;
  title: string;
  created_at: Date;
  updated_at: Date;
}

@Component({
  selector: 'app-sidebar',
  imports: [CommonModule, AlertComponent],
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

  constructor(private apiService: ApiService) {}

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
    
    try {
      await firstValueFrom(
        this.apiService.deleteChat(deletedChatId)
      );

      this.chats = this.chats.filter(c => c.id !== deletedChatId);
      
      // If we deleted the current chat, trigger a new chat
      if (this.currentChatId === deletedChatId) {
        this.newChat.emit();
      }
    } catch (error) {
      // Handle error silently
    }

    this.cancelDelete();
  }

  cancelDelete(): void {
    this.showDeleteAlert = false;
    this.chatToDelete = null;
  }
}