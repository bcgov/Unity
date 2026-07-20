import { Component, Input } from '@angular/core';

import { ShellMenuItem } from '../shell.model';

// Mirrors Themes/UX2/Components/Menu/_MenuItem.cshtml's recursive partial: a leaf
// renders as a plain dropdown-item link, a non-leaf renders a nested dropdown-submenu
// and recurses into itself for its own children.
@Component({
  selector: 'app-menu-item',
  standalone: true,
  imports: [MenuItemComponent],
  templateUrl: './menu-item.component.html'
})
export class MenuItemComponent {
  @Input({ required: true }) item!: ShellMenuItem;

  get isLeaf(): boolean {
    return !this.item.items || this.item.items.length === 0;
  }

  // Matches Default.cshtml/_MenuItem.cshtml: icons only render when the icon name
  // starts with "fa" - this app's actual menu icons are "fl fl-*" (fluent icons), so
  // in practice no icon ever renders today. Replicated as-is for visual parity.
  get showIcon(): boolean {
    return !!this.item.icon && this.item.icon.startsWith('fa');
  }
}
