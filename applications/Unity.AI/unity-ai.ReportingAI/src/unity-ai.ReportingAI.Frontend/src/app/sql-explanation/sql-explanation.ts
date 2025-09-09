import { Component, Input, OnChanges, SimpleChanges, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-sql-explanation',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="sql-explanation-bubble" *ngIf="displayedText || isWaiting">
      <div class="bubble-content">
        <span>{{ displayedText }}</span>
        <span class="cursor" *ngIf="showCursor">█</span>
      </div>
      <div class="bubble-tail"></div>
    </div>
  `,
  styles: [`
    .sql-explanation-bubble {
      position: relative;
      background: #f0f9ff;
      border: 1px solid #bae6fd;
      border-radius: 12px;
      padding: 0;
      margin: 12px 8px 8px 8px;
      font-size: 0.85em;
      color: #075985;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
      animation: slideIn 0.3s ease-out;
    }

    @keyframes slideIn {
      from {
        opacity: 0;
        transform: translateY(-10px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }

    .bubble-content {
      padding: 10px 14px;
      line-height: 1.4;
    }

    .bubble-tail {
      position: absolute;
      top: -6px;
      left: 30px;
      width: 12px;
      height: 12px;
      background: #f0f9ff;
      border-left: 1px solid #bae6fd;
      border-top: 1px solid #bae6fd;
      transform: rotate(45deg);
    }
    
    .cursor {
      animation: blink 1s infinite;
      font-weight: normal;
      opacity: 0.8;
      color: #075985;
    }
    
    @keyframes blink {
      0%, 50% { opacity: 0.8; }
      51%, 100% { opacity: 0; }
    }
  `]
})
export class SqlExplanationComponent implements OnChanges, OnDestroy {
  @Input() explanation: string | undefined;
  displayedText: string = '';
  showCursor: boolean = false;
  isWaiting: boolean = false;
  private streamingInterval: any;
  private delayTimeout: any;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['explanation']) {
      if (this.explanation) {
        this.startDelayedStream();
      } else {
        // Show just the cursor while waiting for explanation
        this.displayedText = '';
        this.showCursor = true;
        this.isWaiting = true;
      }
    }
  }

  private startDelayedStream(): void {
    // Clear any existing intervals/timeouts
    this.clearTimers();
    
    this.displayedText = '';
    this.showCursor = true;
    this.isWaiting = true;
    
    // Start streaming immediately - no delay
    this.streamText();
  }

  private streamText(): void {
    let currentIndex = 0;
    const fullText = this.explanation || '';
    
    // Stream letter by letter with a small delay
    this.streamingInterval = setInterval(() => {
      if (currentIndex < fullText.length) {
        this.displayedText += fullText[currentIndex];
        currentIndex++;
      } else {
        // Hide cursor when done streaming
        this.showCursor = false;
        clearInterval(this.streamingInterval);
      }
    }, 10); // 15ms delay between each letter
  }

  private clearTimers(): void {
    if (this.streamingInterval) {
      clearInterval(this.streamingInterval);
    }
    if (this.delayTimeout) {
      clearTimeout(this.delayTimeout);
    }
  }

  ngOnDestroy(): void {
    this.clearTimers();
  }
}