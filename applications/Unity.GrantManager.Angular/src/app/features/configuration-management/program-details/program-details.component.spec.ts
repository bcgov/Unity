import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CONFIGURATION_MANAGEMENT_API_BASE } from '../../../core/http/api-paths';
import { ProgramDetailsComponent } from './program-details.component';
import { ProgramDetails } from './program-details.model';

const FAKE_DETAILS: ProgramDetails = {
  displayName: 'Original Name',
  division: 'Original Division',
  branch: 'Original Branch',
  description: 'Original Description'
};

// Guards against NG0203-style regressions ("X can only be used within an injection
// context") that a directive/service-level test alone wouldn't catch, since those
// don't exercise this component's own ngOnInit at all - only actually creating the
// component and running change detection proves this class of bug doesn't happen.
describe('ProgramDetailsComponent', () => {
  let fixture: ComponentFixture<ProgramDetailsComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProgramDetailsComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();

    fixture = TestBed.createComponent(ProgramDetailsComponent);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  function flushGet(): void {
    const req = httpMock.expectOne(`${CONFIGURATION_MANAGEMENT_API_BASE}/program-details`);
    req.flush(FAKE_DETAILS);
  }

  it('creates without throwing and loads existing values', () => {
    expect(() => fixture.detectChanges()).not.toThrow();
    flushGet();
    fixture.detectChanges();

    expect(fixture.componentInstance.form.getRawValue()).toEqual(FAKE_DETAILS);
    expect(fixture.componentInstance.hasChanges()).toBeFalse();
  });

  it('enables Save/Discard only after the form actually changes', (done) => {
    fixture.detectChanges();
    flushGet();
    fixture.detectChanges();

    expect(fixture.componentInstance.hasChanges()).toBeFalse();

    fixture.componentInstance.form.controls.displayName.setValue('Changed Name');

    // The dirty check is debounced (150ms), matching ProgramDetails.js's own debounce.
    setTimeout(() => {
      expect(fixture.componentInstance.hasChanges()).toBeTrue();
      done();
    }, 200);
  });
});
