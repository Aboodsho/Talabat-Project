import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../environments/environment';

export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  // لو المسار بيبدأ بـ /api، نضيف رابط السيرفر
  if (req.url.startsWith('/api')) {
    const apiReq = req.clone({
      url: `${environment.apiUrl}${req.url}`
    });
    return next(apiReq);
  }
  return next(req);
};