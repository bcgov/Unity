import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AfterViewInit, DestroyRef, Directive, ElementRef, HostListener, Renderer2, inject } from '@angular/core';
import { NgControl } from '@angular/forms';

// Reusable Angular equivalent of Views/Settings/ProgramDetails/CharacterCounter.js's
// UnityCharacterCounter.init(...) - same markup/classes (char-counter-wrapper,
// char-counter, char-counter-warning/critical thresholds at 80%/95%), so it renders
// identically under the same theme CSS, just driven by Angular's renderer instead
// of jQuery DOM wrapping. Reads maxLength directly off the host element (the same
// `maxlength` attribute/property an [maxlength] binding already sets) rather than
// taking a separate input, so there's exactly one source of truth for the limit.
@Directive({
  selector: '[appCharacterCounter]',
  standalone: true
})
export class CharacterCounterDirective implements AfterViewInit {
  private readonly el = inject(ElementRef<HTMLInputElement | HTMLTextAreaElement>);
  private readonly renderer = inject(Renderer2);
  private readonly destroyRef = inject(DestroyRef);
  // optional: this directive also works on a plain (non-form-bound) input; self:
  // true so it only picks up an NgControl on this exact element, not an ancestor's.
  private readonly ngControl = inject(NgControl, { optional: true, self: true });

  private counterElement: HTMLElement | null = null;
  private maxLength = 0;

  // Deliberately NOT ngOnInit: when `maxlength` is a dynamic property binding
  // (e.g. [maxlength]="limits.displayName", exactly how program-details.component
  // uses it) rather than a static HTML attribute, it is not yet applied to the
  // native element by the time this directive's ngOnInit runs - reading
  // input.maxLength there returns -1, so the "already has a limit" check silently
  // failed and no wrapper/counter was ever created. Confirmed with a dedicated
  // reactive-forms + [maxlength] repro test; a static-attribute-only test had
  // missed this entirely, since a static attribute IS present before any Angular
  // JS runs. ngAfterViewInit runs after this view's bindings have been applied, so
  // the value is reliably correct by then.
  ngAfterViewInit(): void {
    const input = this.el.nativeElement;
    this.maxLength = input.maxLength;
    if (this.maxLength <= 0) {
      return;
    }

    const parent = this.renderer.parentNode(input);

    const wrapper = this.renderer.createElement('div');
    this.renderer.addClass(wrapper, 'char-counter-wrapper');
    this.renderer.insertBefore(parent, wrapper, input);
    this.renderer.removeChild(parent, input);
    this.renderer.appendChild(wrapper, input);

    this.counterElement = this.renderer.createElement('div');
    this.renderer.addClass(this.counterElement, 'char-counter');
    this.renderer.appendChild(wrapper, this.counterElement);

    this.updateCounter();

    // A bound form control's value can change programmatically (e.g. this app's
    // own program-details.component.ts calling form.reset(details) once its GET
    // resolves) without ever firing a native 'input' event - the (input)
    // HostListener below misses that entirely, which is why the count stayed at
    // 0 until the user started typing. valueChanges fires for both that and
    // normal typing, so it's the one thing that reliably catches every change.
    this.ngControl?.control?.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.updateCounter();
    });
  }

  @HostListener('input')
  onInput(): void {
    this.updateCounter();
  }

  private updateCounter(): void {
    if (!this.counterElement) {
      return;
    }

    const currentLength = this.el.nativeElement.value.length;
    this.renderer.setProperty(this.counterElement, 'textContent', `${currentLength}/${this.maxLength}`);

    const percentageUsed = (currentLength / this.maxLength) * 100;
    this.renderer.removeClass(this.counterElement, 'char-counter-critical');
    this.renderer.removeClass(this.counterElement, 'char-counter-warning');
    if (percentageUsed >= 95) {
      this.renderer.addClass(this.counterElement, 'char-counter-critical');
    } else if (percentageUsed >= 80) {
      this.renderer.addClass(this.counterElement, 'char-counter-warning');
    }
  }
}
