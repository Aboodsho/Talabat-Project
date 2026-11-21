import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';

export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  const apiUrl = environment.apiUrl.replace(/\/+$/, '');
  const localApiUrl = 'http://localhost:5124';
  const token = inject(AuthService).token();
  const apiRequest = req.url.startsWith('/api') || req.url.includes(localApiUrl);
  const authorizedReq = apiRequest && token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  // Production uses same-origin /api requests, which Vercel proxies to the API.
  // Development prefixes requests with the local API host.
  if (authorizedReq.url.startsWith('/api')) {
    if (!apiUrl || apiUrl === '/api') {
      return next(authorizedReq);
    }
    return next(authorizedReq.clone({ url: `${apiUrl}${authorizedReq.url}` }));
  }

  // Rewrite any legacy hard-coded local API URL to the configured API target.
  if (authorizedReq.url.includes(localApiUrl)) {
    const replacement = apiUrl === '/api' ? '' : apiUrl;
    return next(authorizedReq.clone({ url: authorizedReq.url.replace(localApiUrl, replacement) }));
  }

  return next(authorizedReq);
};
