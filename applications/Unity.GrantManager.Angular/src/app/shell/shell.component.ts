import { Component, ElementRef, HostListener, computed, inject, signal } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { RouterOutlet } from '@angular/router';

import { MenuItemComponent } from './menu-item/menu-item.component';
import { ShellService } from './shell.service';

// Mirrors Themes/UX2/Layouts/Application.cshtml + Components/Topbar/Default.cshtml:
// brand + main menu + user-initials + user-dropdown, then a content area (here,
// the router-outlet). No footer - the MVC app doesn't have one either.
@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, MenuItemComponent],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss'
})
export class ShellComponent {
  private readonly shellService = inject(ShellService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly elementRef = inject(ElementRef<HTMLElement>);

  readonly bootstrap = this.shellService.bootstrap;
  readonly mainMenu = computed(() => this.bootstrap()?.mainMenu ?? []);
  readonly userDropdown = computed(() => this.bootstrap()?.userDropdown ?? null);

  // This is NOT a Bootstrap data-bs-toggle dropdown, even though it looks like one -
  // themes/ux2/unity-styles.css:109 sets `#user-dropdown { display: none; }` by
  // default (hiding the whole panel, tenant-name button included) and
  // themes/ux2/layout.js:117-151 wires a hand-written click listener on
  // `.unity-user-initials` (the avatar badge) that toggles `.show` on the outer
  // `#user-dropdown` box, not on the button. That's what "the badge does nothing
  // when clicked" actually meant - clicking the tenant-name text was never the
  // real interaction; the badge is. Matching that exactly, including
  // stopPropagation on the badge click (layout.js does this too, to stop
  // Bootstrap's own delegated dropdown-toggle listener from also reacting).
  readonly userDropdownOpen = signal(false);

  toggleUserDropdown(event: MouseEvent): void {
    event.stopPropagation();
    this.userDropdownOpen.update((open) => !open);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.userDropdownOpen()) {
      return;
    }
    const target = event.target as Node;
    const badge = this.elementRef.nativeElement.querySelector('.unity-user-initials');
    const dropdown = this.elementRef.nativeElement.querySelector('#user-dropdown');
    const clickedBadge = !!badge?.contains(target);
    const clickedDropdown = !!dropdown?.contains(target);
    if (!clickedBadge && !clickedDropdown) {
      this.userDropdownOpen.set(false);
    }
  }

  // Badge is trusted, server-issued markup (same claim the Razor Topbar renders via
  // Html.Raw) - not user input, so bypassing sanitization here matches that same
  // trust boundary rather than introducing a new one.
  readonly badgeHtml = computed<SafeHtml | null>(() => {
    const badge = this.bootstrap()?.user?.badge;
    return badge ? this.sanitizer.bypassSecurityTrustHtml(badge) : null;
  });

  // Top-level main-menu items are either a plain leaf link or a dropdown of leaf
  // children (this app's menu contributor never nests more than one level deep).
  isLeaf(item: { items: unknown[] }): boolean {
    return !item.items || item.items.length === 0;
  }

  showIcon(icon: string | null): boolean {
    return !!icon && icon.startsWith('fa');
  }
}
