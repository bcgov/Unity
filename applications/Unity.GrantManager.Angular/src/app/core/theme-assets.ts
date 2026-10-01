// Loads the MVC app's own theme stylesheets at runtime rather than linking them in
// index.html. Angular's build/dev-server treats every root-relative href in
// index.html as part of its own asset graph and rewrites it under the app's base
// path (e.g. /themes/ux2/fonts.css -> /app/themes/ux2/fonts.css), which 404s since
// those files are only ever served by the backend at the un-prefixed path. A link
// element created here via the DOM API is never touched by that build-time rewrite -
// its href is resolved by the browser against the current origin exactly as written.
//
// Order matters: this is the base-layer subset of the real list the MVC app itself
// serves (confirmed by curling the running app and reading its actual <link> tags,
// not guessed) - abp.css and Bootstrap's own CSS have to load before the UX2 theme
// files, since those only add overrides on top of Bootstrap's base classes. Table/
// picker/rich-text-editor widget CSS (datatables, select2, daterangepicker, tinymce,
// malihu-scrollbar) is intentionally left out - none of it affects the shell's
// header/nav, which is all that's rendered here today.
const THEME_STYLESHEETS = [
  '/libs/abp/core/abp.css',
  '/libs/bootstrap/css/bootstrap.css',
  '/libs/@fortawesome/fontawesome-free/css/all.css',
  '/libs/@fortawesome/fontawesome-free/css/v4-shims.css',
  '/themes/ux2/fonts.css',
  '/themes/ux2/fluentui-icons.css',
  '/themes/ux2/fluenticons.min.css',
  '/themes/ux2/layout.css',
  '/themes/ux2/unity-styles.css',
  '/libs/abp/aspnetcore-mvc-ui-theme-shared/toast/abp-toast.css'
];

export function loadStylesheet(href: string): void {
  const link = document.createElement('link');
  link.rel = 'stylesheet';
  link.href = href;
  document.head.appendChild(link);
}

export function loadThemeStylesheets(): void {
  for (const href of THEME_STYLESHEETS) {
    loadStylesheet(href);
  }
}

// Bootstrap's dropdown/collapse/etc. components only work with their own JS loaded -
// data-bs-toggle="dropdown" is otherwise inert, which is why the user-dropdown and
// menu-item submenus didn't respond to clicks. bootstrap.bundle.js is self-contained
// (includes Popper, no jQuery dependency), so it's the only script needed here - the
// rest of the MVC app's script list is jQuery/plugin wiring for widgets (datatables,
// tinymce, etc.) this shell doesn't use.
// abp-toast.js implements the actual bottom-right toast (position/timing/markup)
// that ProgramDetails.js's abp.notify.success/error/info calls render with in the
// MVC app - abp.js core only defines abp.notify.* as unimplemented stubs. This file
// happens to be plain vanilla JS with no dependency on jQuery or abp.js core (it
// defines its own AbpToastService constructor directly on window), so it's safe to
// load standalone - see core/toast.service.ts for how Angular calls into it.
const THEME_SCRIPTS = [
  '/libs/bootstrap/js/bootstrap.bundle.js',
  '/libs/abp/aspnetcore-mvc-ui-theme-shared/toast/abp-toast.js'
];

// abp-toast.js's own trailing wiring (`abp.notify.success = ...`) assumes abp.js
// core already ran and defined `window.abp` / `abp.notify` - which we deliberately
// don't load (see comment above). Without this stub that wiring throws an uncaught
// "Cannot set properties of undefined" on script load. We never call abp.notify.*
// ourselves (core/toast.service.ts calls AbpToastService directly), so this stub
// exists purely to keep that unrelated code path from crashing, not because we need it.
function stubAbpNotifyNamespace(): void {
  const globalWindow = window as unknown as { abp?: { notify?: unknown } };
  globalWindow.abp = globalWindow.abp || {};
  globalWindow.abp.notify = globalWindow.abp.notify || {};
}

export function loadThemeScripts(): void {
  stubAbpNotifyNamespace();
  for (const src of THEME_SCRIPTS) {
    const script = document.createElement('script');
    script.src = src;
    document.head.appendChild(script);
  }
}
