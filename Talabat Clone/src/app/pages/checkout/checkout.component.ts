import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { LucideDynamicIcon, LucideArrowLeft, LucideArrowUpRight, LucideMapPin, LucidePlus, LucideMinus, LucideX, LucideShieldCheck, LucideCreditCard, LucideBanknote, LucideCheck, LucideChevronRight, LucideShoppingBag, LucideLockKeyhole } from '@lucide/angular';
import { AuthService } from '../../core/auth.service';
import { CartService } from '../../core/cart.service';

@Component({
  selector: 'app-checkout', standalone: true, imports: [CommonModule, FormsModule, RouterLink, LucideDynamicIcon],
  templateUrl: './checkout.component.html', styleUrl: './checkout.component.css'
})
export class CheckoutComponent {
  readonly cart = inject(CartService);
  readonly auth = inject(AuthService);
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  readonly icons = { ArrowLeft: LucideArrowLeft, ArrowUpRight: LucideArrowUpRight, MapPin: LucideMapPin, Plus: LucidePlus, Minus: LucideMinus, X: LucideX, ShieldCheck: LucideShieldCheck, CreditCard: LucideCreditCard, Banknote: LucideBanknote, Check: LucideCheck, ChevronRight: LucideChevronRight, ShoppingBag: LucideShoppingBag, LockKeyhole: LucideLockKeyhole };
  readonly busy = signal(false);
  readonly message = signal('');
  readonly orderId = signal('');
  readonly selectedPayment = signal('cash');
  loginEmail = '';
  loginPassword = '';
  street = '';
  city = '';
  state = '';
  postalCode = '';
  country = 'Egypt';

  addressValid(): boolean {
    return !!this.street.trim() && !!this.city.trim() && !!this.country.trim();
  }

  login(): void {
    this.message.set('');
    this.busy.set(true);
    this.auth.login(this.loginEmail, this.loginPassword).subscribe({
      next: response => {
        this.loginPassword = '';
        if ((response.user?.roles ?? []).some(role => role.trim().toLowerCase() === 'admin')) {
          this.busy.set(false);
          this.router.navigate(['/admin']);
          return;
        }
        this.cart.syncGuestCart();
        this.busy.set(false);
      },
      error: error => {
        this.message.set(error.status === 403 ? 'Confirm your email before signing in.' : 'Sign in failed. Check your email and password.');
        this.busy.set(false);
      }
    });
  }

  placeOrder(): void {
    if (!this.auth.isAuthenticated()) { this.message.set('Sign in to place your order.'); return; }
    if (!this.cart.items().length) { this.message.set('Your cart is empty. Add products before checkout.'); return; }
    if (!this.addressValid()) { this.message.set('Enter a street address, city, and country to continue.'); return; }
    this.message.set('');
    this.busy.set(true);
    this.http.post<{ orderId: string }>('/api/orders/checkout', {
      street: this.street,
      city: this.city,
      state: this.state || null,
      postalCode: this.postalCode || null,
      country: this.country
    }).subscribe({
      next: response => {
        this.orderId.set(response.orderId);
        this.cart.clear();
        this.busy.set(false);
      },
      error: error => {
        this.message.set(error.error?.error || 'Checkout failed. Please review your cart and try again.');
        this.busy.set(false);
      }
    });
  }
}
