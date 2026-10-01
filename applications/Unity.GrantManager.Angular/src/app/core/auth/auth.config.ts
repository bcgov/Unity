export const AUTH_CONFIG = {
  // Pages/Account/Login.cshtml.cs: [Authorize] page that challenges via OIDC if needed,
  // then honors ?returnUrl (added alongside this SPA - previously hardcoded to
  // /GrantApplications, which remains the fallback when returnUrl is absent/non-local).
  loginPath: '/Account/Login',
  returnUrlParam: 'returnUrl'
} as const;
