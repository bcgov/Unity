import { Injectable, signal } from '@angular/core';

import { ShellBootstrap } from './shell.model';

// Populated once by authGuard (core/auth/auth.guard.ts) right after the shell
// bootstrap call succeeds, so ShellComponent never needs to fetch it again itself.
@Injectable({ providedIn: 'root' })
export class ShellService {
  private readonly bootstrapState = signal<ShellBootstrap | null>(null);

  readonly bootstrap = this.bootstrapState.asReadonly();

  setBootstrap(bootstrap: ShellBootstrap): void {
    this.bootstrapState.set(bootstrap);
  }
}
