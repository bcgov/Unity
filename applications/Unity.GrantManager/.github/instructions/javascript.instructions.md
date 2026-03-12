---
applyTo: "**/*.js"
description: "JavaScript development standards for ABP Framework frontend patterns"
---

# JavaScript Development Standards

- Variables should be declared with "let" or "const" instead of "var"

## General Patterns

- Wrap all page scripts in IIFE: `(function ($) { ... })(jQuery);`
- Never create global JavaScript variables
- Use `var l = abp.localization.getResource('GrantManager');` for all user-facing text
- Use ABP's dynamic JavaScript API client proxies instead of manual AJAX

## ABP JavaScript Utilities

- Notifications: `abp.notify.success()`, `.error()`, `.warn()`, `.info()`
- Confirmation: `abp.message.confirm()` for destructive actions
- Authorization: `abp.auth.isGranted()` for permission checks
- Busy indicators: `abp.ui.setBusy()` / `abp.ui.clearBusy()`
- Localization: `l('LocalizationKey')` — never hardcode user-facing strings

## DataTables Integration

- Use DataTables.net 2.x with Bootstrap 5 integration (`datatables.net-bs5`)
- Always wrap configuration with `abp.libs.datatables.normalizeConfiguration()`
- Use `abp.libs.datatables.createAjax()` for server-side pagination
- Use `rowAction` for action buttons with `abp.auth.isGranted()` visibility checks
- Use `dataFormat` property for automatic date/boolean formatting
- Always call `dataTable.ajax.reload()` after CRUD operations

## Modal Manager

- Use `abp.ModalManager` for all modal dialogs
- Configure with `viewUrl`, `scriptUrl`, and `modalClass`
- Implement `onResult()` callback to reload DataTable after save
- Modal script classes: register in `abp.modals.*` namespace
- Return `NoContent()` from Razor Page handler to close modal

## DOM Auto-Initialization

- ABP auto-initializes: tooltips, popovers, datepickers, AJAX forms, autocomplete selects
- Use `data-bs-toggle="tooltip"` for tooltips
- Use `class="auto-complete-select"` with `data-autocomplete-*` attributes for lookups
- Use `data-ajaxForm="true"` for AJAX form submission

## Client-Side Package Management

- Add NPM packages to `package.json`, prefer `@abp/*` packages
- Configure `abp.resourcemapping.js` to map from `node_modules` to `wwwroot/libs`
- Run `abp install-libs` to copy resources
- Add to bundle contributor in `Unity.Theme.UX2` module