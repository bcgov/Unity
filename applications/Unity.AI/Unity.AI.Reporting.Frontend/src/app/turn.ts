import { Embed } from './embed';
import { SafeResourceUrl } from '@angular/platform-browser';

export interface Turn {
  question: string;
  embed: Embed;
  safeUrl: SafeResourceUrl | null; 
  iframeLoaded: boolean; 
  sqlPanelOpen?: boolean; // Optional property to track SQL panel state
  errorType?: 'rate_limit' | 'connection_error' | 'ai_failure' | 'server_error' | 'unknown';
  errorMessage?: string;
  errorDetail?: string | null;
  retryCount?: number;
  canRetry?: boolean;
}