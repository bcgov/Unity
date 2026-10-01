import { Component } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CharacterCounterDirective } from './character-counter.directive';

@Component({
  standalone: true,
  imports: [CharacterCounterDirective],
  template: `<input #inputEl id="name" type="text" maxlength="10" appCharacterCounter value="hello" />`
})
class HostComponent {}

// Reproduces the real usage in program-details.component.html: a dynamic
// [maxlength] property binding on a reactive-form-bound control, not a static
// HTML attribute. This distinction mattered - the directive originally read
// maxLength in ngOnInit, which runs *before* dynamic property bindings (as
// opposed to static attributes, already present in markup before any Angular JS
// runs) are applied to the native element, so input.maxLength was still -1 and
// the wrapper/counter were silently never created. Caught only by this test, not
// the static-attribute one above - fixed by moving the logic to ngAfterViewInit.
@Component({
  standalone: true,
  imports: [ReactiveFormsModule, CharacterCounterDirective],
  template: `
    <form [formGroup]="form">
      <input id="displayName" type="text" formControlName="displayName" [maxlength]="30" appCharacterCounter />
    </form>
  `
})
class ReactiveFormHostComponent {
  private readonly fb = new FormBuilder();
  form = this.fb.nonNullable.group({ displayName: ['DDDDDAA'] });
}

describe('CharacterCounterDirective', () => {
  let fixture: ComponentFixture<HostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [HostComponent] }).compileComponents();
    fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
  });

  function getInput(): HTMLInputElement {
    return fixture.nativeElement.querySelector('#name');
  }

  function getCounter(): HTMLElement {
    return fixture.nativeElement.querySelector('.char-counter');
  }

  it('wraps the input and shows an initial count', () => {
    const input = getInput();
    expect(input.parentElement?.classList.contains('char-counter-wrapper')).toBeTrue();

    const counter = getCounter();
    expect(counter).withContext('counter element should exist').not.toBeNull();
    expect(counter.textContent).toBe('5/10');
    expect(counter.classList.contains('char-counter-warning')).toBeFalse();
    expect(counter.classList.contains('char-counter-critical')).toBeFalse();
  });

  it('updates the count and warning class on input (80%+)', () => {
    const input = getInput();
    input.value = 'helloworl'; // 9/10 = 90%
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const counter = getCounter();
    expect(counter.textContent).toBe('9/10');
    expect(counter.classList.contains('char-counter-warning')).toBeTrue();
    expect(counter.classList.contains('char-counter-critical')).toBeFalse();
  });

  it('switches to critical at 95%+', () => {
    const input = getInput();
    input.value = 'helloworld'; // 10/10 = 100%
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const counter = getCounter();
    expect(counter.textContent).toBe('10/10');
    expect(counter.classList.contains('char-counter-critical')).toBeTrue();
  });
});

describe('CharacterCounterDirective with a dynamic [maxlength] binding on a reactive-form control', () => {
  let fixture: ComponentFixture<ReactiveFormHostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [ReactiveFormHostComponent] }).compileComponents();
    fixture = TestBed.createComponent(ReactiveFormHostComponent);
    fixture.detectChanges();
  });

  it('still wraps the input and shows the correct count', () => {
    const input: HTMLInputElement = fixture.nativeElement.querySelector('#displayName');
    expect(input.parentElement?.classList.contains('char-counter-wrapper')).toBeTrue();

    const counter: HTMLElement = fixture.nativeElement.querySelector('.char-counter');
    expect(counter).withContext('counter element should exist').not.toBeNull();
    expect(counter.textContent).toBe('7/30');
  });

  // Reproduces the exact real-world sequence: program-details.component.ts starts
  // with an empty form, then calls form.reset(details) once its async GET request
  // resolves - a programmatic value change that fires no native 'input' DOM event.
  // The count stayed at "0/30" until the user typed, because the directive only
  // listened for that event. Fixed by also subscribing to the bound control's own
  // valueChanges, which fires for both this and normal typing.
  it('updates the count on a programmatic value change with no native input event', () => {
    fixture.componentInstance.form.reset({ displayName: 'A New Value' });
    fixture.detectChanges();

    const counter: HTMLElement = fixture.nativeElement.querySelector('.char-counter');
    expect(counter.textContent).toBe('11/30');
  });
});
