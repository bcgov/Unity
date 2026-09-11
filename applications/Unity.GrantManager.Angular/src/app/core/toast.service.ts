import { Injectable } from '@angular/core';

// AbpToastService is defined globally by /libs/abp/aspnetcore-mvc-ui-theme-shared/toast/abp-toast.js
// (loaded at runtime - see core/theme-assets.ts), the exact same toast the MVC app's
// ProgramDetails.js calls via abp.notify.success/error/info. Constructed fresh per
// call (matching abp-toast.js's own wiring: `new AbpToastService().success(...)`),
// not cached in the constructor, so an early call can't race the script's load.
declare const AbpToastService: new () => {
  success(message: string): number;
  error(message: string): number;
  info(message: string): number;
  warning(message: string): number;
};

@Injectable({ providedIn: 'root' })
export class ToastService {
  success(message: string): void {
    new AbpToastService().success(message);
  }

  error(message: string): void {
    new AbpToastService().error(message);
  }

  info(message: string): void {
    new AbpToastService().info(message);
  }
}
