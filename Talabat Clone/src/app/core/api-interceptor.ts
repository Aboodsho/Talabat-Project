import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../environments/environment';

export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  const apiUrl = environment.apiUrl.replace(/\/+$/, '');
  const localApiUrl = 'http://localhost:5124';

  // Production uses same-origin /api requests, which Vercel proxies to the API.
  // Development prefixes requests with the local API host.
  if (req.url.startsWith('/api')) {
    if (!apiUrl || apiUrl === '/api') {
      return next(req);
    }
    return next(req.clone({ url: `${apiUrl}${req.url}` }));
  }

  // Rewrite any legacy hard-coded local API URL to the configured API target.
  if (req.url.includes(localApiUrl)) {
    const replacement = apiUrl === '/api' ? '' : apiUrl;
    return next(req.clone({ url: req.url.replace(localApiUrl, replacement) }));
  }

  return next(req);
};
