import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface Toast {
  id: string;
  message: string;
  type: 'success' | 'error' | 'info' | 'warning';
  duration?: number;
  show?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  private toastsSubject = new BehaviorSubject<Toast[]>([]);
  public toasts$ = this.toastsSubject.asObservable();

  private generateId(): string {
    return Math.random().toString(36).substr(2, 9);
  }

  show(message: string, type: Toast['type'] = 'info', duration: number = 2500): string {
    const id = this.generateId();
    const toast: Toast = {
      id,
      message,
      type,
      duration,
      show: true
    };

    const currentToasts = this.toastsSubject.value;
    this.toastsSubject.next([...currentToasts, toast]);

    // Auto-remove toast after duration
    if (duration > 0) {
      setTimeout(() => {
        this.remove(id);
      }, duration);
    }

    return id;
  }

  success(message: string, duration?: number): string {
    return this.show(message, 'success', duration);
  }

  error(message: string, duration?: number): string {
    return this.show(message, 'error', duration);
  }

  info(message: string, duration?: number): string {
    return this.show(message, 'info', duration);
  }

  warning(message: string, duration?: number): string {
    return this.show(message, 'warning', duration);
  }

  remove(id: string): void {
    const currentToasts = this.toastsSubject.value;
    const updatedToasts = currentToasts.filter(toast => toast.id !== id);
    this.toastsSubject.next(updatedToasts);
  }

  clear(): void {
    this.toastsSubject.next([]);
  }
}