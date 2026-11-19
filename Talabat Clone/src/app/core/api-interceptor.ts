import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../environments/environment';

export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  const apiUrl = environment.apiUrl.replace(/\/+$/, '');
  const localApiUrl = 'http://localhost:5124';

  // Prefix relative API paths with the environment-specific API host.
  if (req.url.startsWith('/api')) {
    return next(req.clone({ url: `${apiUrl}${req.url}` }));
  }

  // Rewrite legacy hard-coded local API URLs to the configured API host.
  if (req.url.includes(localApiUrl)) {
    return next(req.clone({ url: req.url.replace(localApiUrl, apiUrl) }));
  }

  return next(req);
};
