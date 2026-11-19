import { Routes } from '@angular/router';
import { HomeComponent } from './pages/home/home.component';
import { SiteShellComponent } from './pages/site-shell.component';

export const routes: Routes = [
  { path: '', component: SiteShellComponent, children: [
    { path: '', component: HomeComponent, title: 'Near Market — Everyday goods' },
    { path: 'explore', loadComponent: () => import('./pages/explore/explore.component').then(m => m.ExploreComponent), title: 'Explore the market — Near Market' },
    { path: 'categories', loadComponent: () => import('./pages/category-list/categories-list.component').then(m => m.CategoriesListComponent), title: 'All categories — Near Market' },
    { path: 'category/:id', loadComponent: () => import('./pages/category/category.component').then(m => m.CategoryComponent), title: 'Shop category — Near Market' },
    { path: 'product/:id', loadComponent: () => import('./pages/product/product-detail.component').then(m => m.ProductDetailComponent), title: 'Product — Near Market' },
    { path: 'cart', loadComponent: () => import('./pages/cart/cart.component').then(m => m.CartComponent), title: 'Your cart — Near Market' },
    { path: 'wishlist', loadComponent: () => import('./pages/wishlist/wishlist.component').then(m => m.WishlistComponent), title: 'Wishlist — Near Market' },
    { path: 'orders', loadComponent: () => import('./pages/orders/orders.component').then(m => m.OrdersComponent), title: 'My orders — Near Market' },
    { path: 'admin', loadComponent: () => import('./pages/admin/admin-dashboard.component').then(m => m.AdminDashboardComponent), title: 'Admin dashboard — Near Market' },
    { path: 'profile', loadComponent: () => import('./pages/profile/profile.component').then(m => m.ProfileComponent), title: 'My profile — Near Market' },
    { path: 'checkout', loadComponent: () => import('./pages/checkout/checkout.component').then(m => m.CheckoutComponent), title: 'Checkout — Near Market' },
    { path: 'confirm-email', loadComponent: () => import('./pages/auth/email-confirmation.component').then(m => m.EmailConfirmationComponent), title: 'Confirm email — Near Market' },
    { path: 'reset-password', loadComponent: () => import('./pages/auth/password-reset.component').then(m => m.PasswordResetComponent), title: 'Reset password — Near Market' }
  ] },
  { path: '**', redirectTo: '' }
];
