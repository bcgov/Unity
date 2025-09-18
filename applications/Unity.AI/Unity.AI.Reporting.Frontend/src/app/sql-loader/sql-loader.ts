import { Component, Input, OnInit, ViewChild, ElementRef, AfterViewInit, OnDestroy } from '@angular/core';

interface Dot {
  x: number;
  y: number;
  z: number;
  baseZ: number;
  gridX: number;
  gridY: number;
}

interface WaveSource {
  x: number;
  y: number;
  vx: number;
  vy: number;
  amplitude: number;
  frequency: number;
  phase: number;
}

@Component({
  selector: 'sql-loader',
  templateUrl: './sql-loader.html',
  styleUrls: ['./sql-loader.css']
})
export class SqlLoaderComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('canvas', { static: false }) canvasRef!: ElementRef<HTMLCanvasElement>;
  @Input() loadingText: string = 'Generating Report...';
  
  private ctx!: CanvasRenderingContext2D;
  private animationId: number = 0;
  private dots: Dot[][] = [];
  private waveSources: WaveSource[] = [];
  private time: number = 0;
  
  private gridCols: number = 0;
  private gridRows: number = 0;
  private dotSpacing: number = 8;  // Much smaller spacing for more dots
  private readonly NUM_WAVE_SOURCES = 3;
  private readonly WAVE_HEIGHT = 40;

  ngOnInit() {
    // Initialize will happen in setupCanvas
  }

  ngAfterViewInit() {
    this.setupCanvas();
    this.initializeGrid();
    this.initializeWaveSources();
    this.animate();
  }

  ngOnDestroy() {
    if (this.animationId) {
      cancelAnimationFrame(this.animationId);
    }
  }

  private setupCanvas() {
    const canvas = this.canvasRef.nativeElement;
    const parent = canvas.parentElement;
    
    if (!parent) return;
    
    canvas.width = parent.clientWidth;
    canvas.height = parent.clientHeight;
    
    this.ctx = canvas.getContext('2d')!;
    
    // Calculate grid dimensions to fill screen
    this.gridCols = Math.ceil(canvas.width / this.dotSpacing) + 2;
    this.gridRows = Math.ceil(canvas.height / this.dotSpacing) + 2;
  }

  private initializeGrid() {
    const canvas = this.canvasRef.nativeElement;
    
    for (let i = 0; i < this.gridCols; i++) {
      this.dots[i] = [];
      for (let j = 0; j < this.gridRows; j++) {
        this.dots[i][j] = {
          x: i * this.dotSpacing,
          y: j * this.dotSpacing,
          z: 0,
          baseZ: 0,
          gridX: i,
          gridY: j
        };
      }
    }
  }

  private initializeWaveSources() {
    const canvas = this.canvasRef.nativeElement;
    
    for (let i = 0; i < this.NUM_WAVE_SOURCES; i++) {
      // Start sources in top-left quadrant
      const startX = Math.random() * canvas.width * 0.3;  // Left 30% of screen
      const startY = Math.random() * canvas.height * 0.3; // Top 30% of screen
      
      this.waveSources.push({
        x: startX,
        y: startY,
        vx: 0.3 + Math.random() * 0.2,  // Positive X velocity (moving right)
        vy: 0.3 + Math.random() * 0.2,  // Positive Y velocity (moving down)
        amplitude: 20 + Math.random() * 20,
        frequency: 0.015 + Math.random() * 0.015,  // Slower frequency
        phase: Math.random() * Math.PI * 2
      });
    }
  }

  private updateWaveSources() {
    const canvas = this.canvasRef.nativeElement;
    
    this.waveSources.forEach(source => {
      // Maintain diagonal movement with slight variation
      source.vx += (Math.random() - 0.3) * 0.02;  // Bias toward positive (right)
      source.vy += (Math.random() - 0.3) * 0.02;  // Bias toward positive (down)
      
      // Keep velocity in diagonal direction
      const speed = Math.sqrt(source.vx * source.vx + source.vy * source.vy);
      if (speed > 0.5) {
        source.vx = (source.vx / speed) * 0.5;
        source.vy = (source.vy / speed) * 0.5;
      }
      
      // Ensure minimum diagonal movement
      if (source.vx < 0.1) source.vx = 0.1;
      if (source.vy < 0.1) source.vy = 0.1;
      
      // Update position
      source.x += source.vx;
      source.y += source.vy;
      
      // When reaching bottom-right, wrap back to top-left
      if (source.x > canvas.width - 50 || source.y > canvas.height - 50) {
        source.x = Math.random() * canvas.width * 0.2;  // Reset to top-left
        source.y = Math.random() * canvas.height * 0.2;
        source.vx = 0.3 + Math.random() * 0.2;
        source.vy = 0.3 + Math.random() * 0.2;
      }
      
      // Slowly change wave parameters
      source.amplitude += (Math.random() - 0.5) * 0.5;
      source.amplitude = Math.max(15, Math.min(35, source.amplitude));
      
      source.phase += source.frequency * 0.2; // Much slower wave propagation
    });
  }

  private calculateWaveHeight(x: number, y: number): number {
    let totalZ = 0;
    
    this.waveSources.forEach(source => {
      const distance = Math.sqrt(
        Math.pow(x - source.x, 2) + 
        Math.pow(y - source.y, 2)
      );
      
      // Wave equation with decay
      const decay = Math.exp(-distance * 0.003);
      const wave = Math.sin(distance * source.frequency - source.phase) * source.amplitude * decay;
      
      totalZ += wave;
    });
    
    // Add subtle ambient waves (much slower)
    const ambientWave1 = Math.sin(this.time * 0.0005 + x * 0.003) * 3;
    const ambientWave2 = Math.cos(this.time * 0.0003 + y * 0.003) * 3;
    
    return totalZ + ambientWave1 + ambientWave2;
  }

  private updateWaves() {
    for (let i = 0; i < this.gridCols; i++) {
      for (let j = 0; j < this.gridRows; j++) {
        const dot = this.dots[i][j];
        dot.z = this.calculateWaveHeight(dot.x, dot.y);
      }
    }
  }

  private drawDot(dot: Dot) {
    // Position with vertical displacement based on wave height
    const x = dot.x;
    const y = dot.y - (dot.z * 0.5); // Dots appear to rise with wave
    
    // Much smaller fixed size for all dots
    const size = 1.5;
    
    // Color changes from dark blue to light blue based on wave height
    const heightNorm = (dot.z + this.WAVE_HEIGHT) / (this.WAVE_HEIGHT * 2);
    const lightness = 35 + heightNorm * 40; // Darker to lighter blue
    const opacity = 0.8 + heightNorm * 0.2; // Slightly more opaque at peaks
    
    // Blue dot with fixed hue and saturation
    this.ctx.fillStyle = `hsla(210, 70%, ${lightness}%, ${opacity})`;
    this.ctx.beginPath();
    this.ctx.arc(x, y, size, 0, Math.PI * 2);
    this.ctx.fill();
  }

  private drawConnections() {
    // Draw subtle connections between dots based on height difference
    for (let i = 0; i < this.gridCols - 1; i++) {
      for (let j = 0; j < this.gridRows - 1; j++) {
        const dot1 = this.dots[i][j];
        const dot2 = this.dots[i + 1][j];
        const dot3 = this.dots[i][j + 1];
        
        // Calculate height difference for line intensity
        const heightDiff1 = Math.abs(dot1.z - dot2.z);
        const heightDiff2 = Math.abs(dot1.z - dot3.z);
        
        // Horizontal connection with vertical displacement
        if (heightDiff1 > 5) {
          const intensity = Math.min(heightDiff1 / 30, 0.15);
          this.ctx.strokeStyle = `hsla(210, 50%, 70%, ${intensity})`;
          this.ctx.lineWidth = 0.5;
          this.ctx.beginPath();
          this.ctx.moveTo(dot1.x, dot1.y - (dot1.z * 0.5));
          this.ctx.lineTo(dot2.x, dot2.y - (dot2.z * 0.5));
          this.ctx.stroke();
        }
        
        // Vertical connection with vertical displacement
        if (heightDiff2 > 5) {
          const intensity = Math.min(heightDiff2 / 30, 0.15);
          this.ctx.strokeStyle = `hsla(210, 50%, 70%, ${intensity})`;
          this.ctx.lineWidth = 0.5;
          this.ctx.beginPath();
          this.ctx.moveTo(dot1.x, dot1.y - (dot1.z * 0.5));
          this.ctx.lineTo(dot3.x, dot3.y - (dot3.z * 0.5));
          this.ctx.stroke();
        }
      }
    }
  }

  private animate() {
    const canvas = this.canvasRef.nativeElement;
    
    // Clear with white background
    this.ctx.fillStyle = 'rgb(255, 255, 255)';
    this.ctx.fillRect(0, 0, canvas.width, canvas.height);
    
    // Add very subtle gradient overlay
    const bgGradient = this.ctx.createRadialGradient(
      canvas.width / 2, canvas.height / 2, 0,
      canvas.width / 2, canvas.height / 2, Math.max(canvas.width, canvas.height) / 2
    );
    bgGradient.addColorStop(0, 'rgba(240, 245, 255, 0.5)');
    bgGradient.addColorStop(1, 'rgba(255, 255, 255, 0.8)');
    this.ctx.fillStyle = bgGradient;
    this.ctx.fillRect(0, 0, canvas.width, canvas.height);
    
    // Update wave sources with random walk
    this.updateWaveSources();
    
    // Update wave heights
    this.updateWaves();
    
    // Draw connections first (underneath dots)
    this.drawConnections();
    
    // Draw all dots
    for (let i = 0; i < this.gridCols; i++) {
      for (let j = 0; j < this.gridRows; j++) {
        this.drawDot(this.dots[i][j]);
      }
    }
    
    // Remove wave source indicators for cleaner look</    // (waves are now only visible through dot displacement)
    
    this.time++;
    this.animationId = requestAnimationFrame(() => this.animate());
  }
}