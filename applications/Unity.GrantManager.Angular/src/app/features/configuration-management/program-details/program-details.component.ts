import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { debounceTime } from 'rxjs';

import { loadStylesheet } from '../../../core/theme-assets';
import { ToastService } from '../../../core/toast.service';
import { CharacterCounterDirective } from '../../../shared/character-counter.directive';
import { PROGRAM_DETAILS_FIELD_LIMITS } from './program-details.model';
import { ProgramDetailsService } from './program-details.service';

@Component({
  selector: 'app-program-details',
  standalone: true,
  imports: [ReactiveFormsModule, CharacterCounterDirective],
  templateUrl: './program-details.component.html',
  styleUrl: './program-details.component.scss'
})
export class ProgramDetailsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly programDetailsService = inject(ProgramDetailsService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly limits = PROGRAM_DETAILS_FIELD_LIMITS;

  readonly form = this.fb.nonNullable.group({
    displayName: ['', Validators.maxLength(this.limits.displayName)],
    division: ['', Validators.maxLength(this.limits.division)],
    branch: ['', Validators.maxLength(this.limits.branch)],
    description: ['', Validators.maxLength(this.limits.description)]
  });

  saving = false;

  // Matches ProgramDetails.js exactly: both buttons start disabled and only enable
  // once the form actually differs from its last-loaded/saved values, recomputed on
  // a 150ms debounce (same value the real page's own debounce() helper uses) rather
  // than on every keystroke.
  readonly hasChanges = signal(false);

  private initialValue = this.form.getRawValue();

  constructor() {
    // Feature-specific (the char-counter CSS classes), not shell-global - matches
    // how the Razor page only pulls this in via its own @section styles block.
    loadStylesheet('/Views/Settings/ProgramDetails/ProgramDetails.css');
  }

  ngOnInit(): void {
    this.programDetailsService.get().subscribe((details) => {
      this.form.reset(details);
      this.initialValue = this.form.getRawValue();
      this.hasChanges.set(false);
    });

    this.form.valueChanges.pipe(debounceTime(150), takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.hasChanges.set(this.computeHasChanges());
    });
  }

  // Matches ProgramDetails.js's exact behavior/message text (localization keys
  // ProgramDetails:SaveSuccess/SaveError/ChangesReset/NoChanges in en.json).
  save(): void {
    if (this.form.invalid || !this.hasChanges()) {
      return;
    }

    this.saving = true;

    this.programDetailsService.update(this.form.getRawValue()).subscribe({
      next: () => {
        this.saving = false;
        this.initialValue = this.form.getRawValue();
        this.hasChanges.set(false);
        this.toast.success('Program details updated successfully.');
      },
      error: () => {
        this.saving = false;
        this.toast.error('An error occurred while saving program details.');
      }
    });
  }

  discard(): void {
    this.form.reset(this.initialValue);
    this.hasChanges.set(false);
    this.toast.info('Changes have been reset to original values.');
  }

  private computeHasChanges(): boolean {
    const current = this.form.getRawValue();
    return (Object.keys(current) as (keyof typeof current)[]).some((key) => current[key] !== this.initialValue[key]);
  }
}
