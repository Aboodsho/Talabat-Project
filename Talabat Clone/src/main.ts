import { bootstrapApplication } from '@angular/platform-browser';
import { provideRouter, withInMemoryScrolling } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpInterceptorFn } from '@angular/common/http';
import { provideLucideIcons, LucideArrowLeft, LucideArrowUpRight, LucideMapPin, LucidePlus, LucideMinus, LucideX, LucideShieldCheck, LucideCreditCard, LucideBanknote, LucideCheck, LucideChevronRight, LucideShoppingBag, LucideLockKeyhole, LucideMenu, LucideSearch, LucideUserRound, LucideSparkles, LucideActivity, LucideCarFront, LucideSmartphone, LucideTentTree, LucideBicepsFlexed, LucideShirt, LucideGamepad2, LucideDumbbell, LucidePackage } from '@lucide/angular';
import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';

const authInterceptor: HttpInterceptorFn = (request, next) => {
  let token: string | null = null;
  try { token = localStorage.getItem('near-market-access-token'); } catch { /* Non-browser rendering */ }
  return next(token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request);
};

bootstrapApplication(AppComponent, { providers: [provideRouter(routes, withInMemoryScrolling({ anchorScrolling: 'enabled', scrollPositionRestoration: 'enabled' })), provideHttpClient(withInterceptors([authInterceptor])), provideLucideIcons(LucideArrowLeft, LucideArrowUpRight, LucideMapPin, LucidePlus, LucideMinus, LucideX, LucideShieldCheck, LucideCreditCard, LucideBanknote, LucideCheck, LucideChevronRight, LucideShoppingBag, LucideLockKeyhole, LucideMenu, LucideSearch, LucideUserRound, LucideSparkles, LucideActivity, LucideCarFront, LucideSmartphone, LucideTentTree, LucideBicepsFlexed, LucideShirt, LucideGamepad2, LucideDumbbell, LucidePackage)] })
  .catch((error: unknown) => console.error(error));
