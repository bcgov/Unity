import { Embed } from './embed';
import { SafeResourceUrl } from '@angular/platform-browser';

export interface Turn {
  question: string;
  embed: Embed;
  safeUrl: SafeResourceUrl | null; 
  iframeLoaded: boolean; 
  sqlPanelOpen?: boolean; // Optional property to track SQL panel state
  sql_explanation?: string; // SQL explanation text
  sql_explanation_visible?: boolean; // Toggle visibility of SQL explanation
}