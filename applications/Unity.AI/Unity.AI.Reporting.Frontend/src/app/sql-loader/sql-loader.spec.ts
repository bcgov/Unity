import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { SqlLoaderComponent } from './sql-loader';

describe('SqlLoaderComponent', () => {
  let component: SqlLoaderComponent;
  let fixture: ComponentFixture<SqlLoaderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SqlLoaderComponent],
      providers: [provideZonelessChangeDetection()]
    }).compileComponents();
  });

  afterEach(() => {
    fixture.destroy();
  });

  function createComponent(loadingText?: string): void {
    fixture = TestBed.createComponent(SqlLoaderComponent);
    component = fixture.componentInstance;
    if (loadingText !== undefined) {
      component.loadingText = loadingText;
    }
    fixture.detectChanges();
  }

  it('should create', () => {
    createComponent();
    expect(component).toBeTruthy();
  });

  it('should display default loading text', () => {
    createComponent();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.loading-text')?.textContent).toBe('Generating Report...');
  });

  it('should display custom loading text via input', () => {
    createComponent('Crunching numbers...');
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.loading-text')?.textContent).toBe('Crunching numbers...');
  });

  it('should render a canvas element', () => {
    createComponent();
    const canvas = fixture.nativeElement.querySelector('canvas');
    expect(canvas).toBeTruthy();
  });

  it('should cancel animation on destroy', () => {
    createComponent();
    (component as any).animationId = 123;

    const spy = vi.spyOn(window, 'cancelAnimationFrame');
    component.ngOnDestroy();

    expect(spy).toHaveBeenCalledWith(123);
    spy.mockRestore();
  });

  it('should not cancel animation on destroy when no animation is running', () => {
    createComponent();
    (component as any).animationId = 0;

    const spy = vi.spyOn(window, 'cancelAnimationFrame');
    component.ngOnDestroy();

    expect(spy).not.toHaveBeenCalled();
    spy.mockRestore();
  });
});
