import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class IframeDetectorService {
  
  constructor() { }

  isInIframe(): boolean {
    return window.self !== window.top;
  }

  addIframeClass(): void {
    if (this.isInIframe()) {
      document.body.classList.add('iframe-embeddable');
    }
  }

  removeIframeClass(): void {
    document.body.classList.remove('iframe-embeddable');
  }
}