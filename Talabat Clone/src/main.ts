import { bootstrapApplication } from '@angular/platform-browser';
import { provideRouter, withInMemoryScrolling } from '@angular/router';
import { HttpInterceptorFn, provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideLucideIcons, LucideArrowLeft, LucideArrowUpRight } from '@lucide/angular';
import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';
import { apiInterceptor } from './app/core/api-interceptor';

const authInterceptor: HttpInterceptorFn = (request, next) => {
  let token: string | null = null;
  try { token = localStorage.getItem('near-market-access-token'); } catch { /* Non-browser rendering */ }
  return next(token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request);
};
bootstrapApplication(AppComponent, {
  providers: [
    provideRouter(routes, withInMemoryScrolling({ anchorScrolling: 'enabled', scrollPositionRestoration: 'enabled' })),
    provideHttpClient(withInterceptors([apiInterceptor, authInterceptor])),
    provideLucideIcons(LucideArrowLeft, LucideArrowUpRight)
  ]
})
.catch((error: unknown) => console.error(error));
