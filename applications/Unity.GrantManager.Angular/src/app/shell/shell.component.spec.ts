import { provideRouter } from '@angular/router';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ShellComponent } from './shell.component';
import { ShellService } from './shell.service';
import { ShellBootstrap } from './shell.model';

const FAKE_BOOTSTRAP: ShellBootstrap = {
  branding: { appName: 'Test App', logoUrl: '/logo.svg' },
  user: { badge: '<span>AG</span>', currentTenantName: 'Test Tenant' },
  mainMenu: [],
  userDropdown: {
    showSwitchGrantPrograms: true,
    showApplicantPortalConfiguration: true,
    showConfigurationManagement: true,
    showUnityAdmin: true
  }
};

// The real click target is the avatar badge (.unity-user-initials), not the
// tenant-name button - see shell.component.ts's comment on userDropdownOpen for
// why (themes/ux2/layout.js's hand-written toggle, not Bootstrap's data-bs-toggle).
describe('ShellComponent user-dropdown', () => {
  let fixture: ComponentFixture<ShellComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShellComponent],
      providers: [provideRouter([])]
    }).compileComponents();

    fixture = TestBed.createComponent(ShellComponent);
    const shellService = TestBed.inject(ShellService);
    shellService.setBootstrap(FAKE_BOOTSTRAP);
    fixture.detectChanges();
  });

  function getBadge(): HTMLElement {
    return fixture.nativeElement.querySelector('.unity-user-initials');
  }

  function getPanel(): HTMLElement {
    return fixture.nativeElement.querySelector('#user-dropdown');
  }

  function getMenu(): HTMLElement {
    return fixture.nativeElement.querySelector('#user-dropdown .dropdown-menu');
  }

  it('opens the panel and menu on badge click', () => {
    expect(getPanel().classList.contains('show')).toBeFalse();
    expect(getMenu().classList.contains('show')).toBeFalse();

    getBadge().dispatchEvent(new MouseEvent('click', { bubbles: true }));
    fixture.detectChanges();

    expect(getPanel().classList.contains('show')).toBeTrue();
    expect(getMenu().classList.contains('show')).toBeTrue();
  });

  it('closes on a second badge click', () => {
    getBadge().dispatchEvent(new MouseEvent('click', { bubbles: true }));
    fixture.detectChanges();
    expect(getPanel().classList.contains('show')).toBeTrue();

    getBadge().dispatchEvent(new MouseEvent('click', { bubbles: true }));
    fixture.detectChanges();
    expect(getPanel().classList.contains('show')).toBeFalse();
  });

  it('closes on an outside click', () => {
    getBadge().dispatchEvent(new MouseEvent('click', { bubbles: true }));
    fixture.detectChanges();
    expect(getPanel().classList.contains('show')).toBeTrue();

    document.body.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    fixture.detectChanges();

    expect(getPanel().classList.contains('show')).toBeFalse();
  });

  it('stays open when clicking inside the panel itself', () => {
    getBadge().dispatchEvent(new MouseEvent('click', { bubbles: true }));
    fixture.detectChanges();

    getPanel().dispatchEvent(new MouseEvent('click', { bubbles: true }));
    fixture.detectChanges();

    expect(getPanel().classList.contains('show')).toBeTrue();
  });
});
