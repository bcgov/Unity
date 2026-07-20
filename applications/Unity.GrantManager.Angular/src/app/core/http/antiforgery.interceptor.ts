import { HttpInterceptorFn } from '@angular/common/http';

// Mirrors abp.security.antiForgery.tokenCookieName/tokenHeaderName in wwwroot/libs/abp/core/abp.js
// (AbpAntiForgeryOptions defaults, unchanged by GrantManagerWebModule.cs's Configure<AbpAntiForgeryOptions>).
const XSRF_COOKIE_NAME = 'XSRF-TOKEN';
const XSRF_HEADER_NAME = 'RequestVerificationToken';

function readCookie(name: string): string | null {
  const escaped = name.replace(/[.$?*|{}()[\]\\/+^]/g, '\\$&');
  const match = document.cookie.match(new RegExp('(^|; )' + escaped + '=([^;]*)'));
  return match ? decodeURIComponent(match[2]) : null;
}

export const antiforgeryInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith('/api/angular-app/')) {
    return next(req);
  }

  const mutating = req.method === 'POST' || req.method === 'PUT' || req.method === 'DELETE' || req.method === 'PATCH';
  const token = mutating ? readCookie(XSRF_COOKIE_NAME) : null;

  return next(
    req.clone({
      withCredentials: true,
      setHeaders: token ? { [XSRF_HEADER_NAME]: token } : {}
    })
  );
};
