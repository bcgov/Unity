import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterOutlet } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../services/api.service';
import { AuthService } from '../services/auth.service';
import { LoggerService } from '../services/logger.service';

@Component({
  selector: 'app-root',
  imports: [CommonModule, RouterOutlet],
  template: `
    <div class="loading-container" *ngIf="isLoading">
      <div class="loading-spinner"></div>
      <div class="loading-text">Checking authentication...</div>
    </div>
    <router-outlet *ngIf="!isLoading"></router-outlet>
  `,
  styles: [`
    .loading-container {
      display: flex;
      flex-direction: column;
      justify-content: center;
      align-items: center;
      height: 100vh;
      background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
      color: white;
    }

    .loading-spinner {
      width: 40px;
      height: 40px;
      border: 4px solid rgba(255, 255, 255, 0.3);
      border-top: 4px solid white;
      border-radius: 50%;
      animation: spin 1s linear infinite;
      margin-bottom: 20px;
    }

    .loading-text {
      font-size: 16px;
      font-weight: 500;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }
  `]
})
export class RootComponent implements OnInit {
  isLoading = true;

  constructor(
    private readonly router: Router,
    private readonly apiService: ApiService,
    private readonly authService: AuthService,
    private readonly logger: LoggerService
  ) {}

  async ngOnInit(): Promise<void> {
    try {
      // Check if user is authenticated
      if (!await this.authService.isAuthenticated()) {
        this.logger.info('User not authenticated, redirecting to main app');
        this.isLoading = false;
        this.router.navigate(['/app']);
        return;
      }

      // Check JWT token for admin status instead of backend API
      const payload = this.authService.decodeToken();
      const isAdmin = payload?.['is_it_admin'] === true || payload?.['is_it_admin'] === 'true';
      
      this.logger.info('JWT Token admin claim (is_it_admin):', payload?.['is_it_admin']);
      this.logger.info('Final admin decision:', isAdmin);
      
      if (isAdmin) {
        this.logger.info('Admin user detected from JWT token, redirecting to admin page');
        this.router.navigate(['/admin']);
      } else {
        this.logger.info('Regular user from JWT token, redirecting to main app');
        this.router.navigate(['/app']);
      }

    } catch (error) {
      this.logger.error('Error checking admin status:', error);
      // On error, default to main app
      this.router.navigate(['/app']);
    } finally {
      this.isLoading = false;
    }
  }
}