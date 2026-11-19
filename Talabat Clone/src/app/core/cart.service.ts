import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { concatMap, from, switchMap, toArray } from 'rxjs';
import { AuthService } from './auth.service';
import { ProductDto } from './catalog.service';

export interface CartItem {
  id: string;
  name: string;
  detail: string;
  price: number;
  image: string;
  quantity: number;
  isAvailable: boolean;
  availableStock?: number;
}

interface CartDto {
  items: Array<{ productId: string; productName: string; imageUrl: string | null; unitPrice: number; quantity: number; lineTotal: number; availableStock: number; isAvailable: boolean }>;
  totalPrice: number;
}

@Injectable({ providedIn: 'root' })
export class CartService {
  readonly maxQuantityPerProduct = 10;
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  readonly items = signal<CartItem[]>([]);
  readonly error = signal('');
  readonly sessionExpired = signal(false);
  readonly syncing = signal(false);
  readonly count = computed(() => this.items().reduce((sum, item) => sum + item.quantity, 0));
  readonly subtotal = computed(() => this.items().reduce((sum, item) => sum + (item.isAvailable ? item.price * item.quantity : 0), 0));
  readonly hasUnavailable = computed(() => this.items().some(item => !item.isAvailable));
  maximumFor(id: string): number {
    const item = this.items().find(current => current.id === id);
    return Math.max(0, Math.min(this.maxQuantityPerProduct, item?.availableStock ?? this.maxQuantityPerProduct));
  }
  remainingFor(id: string): number {
    const item = this.items().find(current => current.id === id);
    return Math.max(0, this.maximumFor(id) - (item?.quantity ?? 0));
  }
  readonly tax = computed(() => Math.round(this.subtotal() * 0.14 * 100) / 100);
  readonly total = computed(() => Math.round((this.subtotal() + this.tax()) * 100) / 100);

  constructor() {
    this.items.set(this.readSavedItems());
    effect(() => { try { localStorage.setItem('near-market-cart', JSON.stringify(this.items())); } catch { /* storage may be unavailable */ } });
    if (this.auth.isAuthenticated()) this.load();
  }

  load(): void {
    if (!this.auth.isAuthenticated()) return;
    this.http.get<CartDto>('/api/cart').subscribe({
      next: cart => {
        this.sessionExpired.set(false);
        this.items.set(cart.items.map(item => ({ id: item.productId, name: item.productName, detail: item.isAvailable ? `${item.availableStock} in stock` : 'Currently unavailable', price: item.unitPrice, image: item.imageUrl || this.placeholderImage, quantity: item.quantity, availableStock: item.availableStock, isAvailable: item.isAvailable })));
        this.error.set('');
      },
      error: error => this.handleRequestError(error, 'We could not load your cart. Please retry.')
    });
  }

  add(product: ProductDto, quantity = 1): void {
    const currentItem = this.items().find(item => item.id === product.id);
    const current = currentItem?.quantity ?? 0;
    const limit = Math.min(this.maxQuantityPerProduct, currentItem?.availableStock ?? product.stockQuantity ?? this.maxQuantityPerProduct);
    if (!Number.isInteger(quantity) || quantity < 1 || current + quantity > limit) {
      this.error.set(currentItem?.availableStock !== undefined || product.stockQuantity !== undefined ? `Only ${limit} unit(s) are available for this product.` : `You can add up to ${limit} units of one product.`);
      return;
    }
    if (!product.isAvailable) { this.error.set('This product is currently unavailable.'); return; }
    this.error.set('');
    if (!this.auth.isAuthenticated()) {
      this.items.update(items => {
        const existing = items.find(item => item.id === product.id);
        return existing ? items.map(item => item.id === product.id ? { ...item, quantity: item.quantity + quantity } : item) : [...items, { ...this.fromProduct(product), quantity }];
      });
      return;
    }
    this.http.post('/api/cart/items', { productId: product.id, quantity }).subscribe({ next: () => this.load(), error: error => this.handleRequestError(error, this.readApiError(error, 'Could not add this item. Check your connection and try again.')) });
  }

  change(id: string, amount: number): void {
    const item = this.items().find(current => current.id === id);
    if (!item) return;
    if (amount > 0 && item.quantity + amount > this.maximumFor(id)) {
      this.error.set(`You can add up to ${this.maximumFor(id)} units of this product.`);
      return;
    }
    this.error.set('');
    if (!this.auth.isAuthenticated()) {
      this.items.update(items => items.map(current => current.id === id ? { ...current, quantity: current.quantity + amount } : current).filter(current => current.quantity > 0));
      return;
    }
    const request = amount > 0
      ? this.http.post('/api/cart/items', { productId: id, quantity: amount })
      : item.quantity <= Math.abs(amount)
        ? this.http.delete(`/api/cart/items/${id}`)
        : this.http.patch(`/api/cart/items/${id}/decrease`, { amount: Math.abs(amount) });
    request.subscribe({ next: () => this.load(), error: error => this.handleRequestError(error, this.readApiError(error, 'Could not update the cart. Please retry.')) });
  }

  remove(id: string): void {
    if (!this.auth.isAuthenticated()) {
      this.items.update(items => items.filter(item => item.id !== id));
      return;
    }
    this.http.delete(`/api/cart/items/${id}`).subscribe({ next: () => this.load(), error: error => this.handleRequestError(error, 'Could not remove this item. Please retry.') });
  }

  syncGuestCart(): void {
    if (!this.auth.isAuthenticated()) return;
    if (this.sessionExpired()) {
      this.sessionExpired.set(false);
      this.load();
      return;
    }
    const guestItems = this.items();
    if (!guestItems.length) { this.load(); return; }
    this.syncing.set(true);
    from(guestItems).pipe(
      concatMap(item => this.http.post('/api/cart/items', { productId: item.id, quantity: item.quantity })),
      toArray(),
      switchMap(() => this.http.get<CartDto>('/api/cart'))
    ).subscribe({
      next: cart => {
        this.sessionExpired.set(false);
        this.items.set(cart.items.map(item => ({ id: item.productId, name: item.productName, detail: item.isAvailable ? `${item.availableStock} in stock` : 'Currently unavailable', price: item.unitPrice, image: item.imageUrl || this.placeholderImage, quantity: item.quantity, availableStock: item.availableStock, isAvailable: item.isAvailable })));
        this.syncing.set(false);
      },
      error: error => { this.handleRequestError(error, 'Signed in, but we could not sync your saved bag. Please retry.'); this.syncing.set(false); }
    });
  }

  clear(): void { this.items.set([]); }

  private handleRequestError(error: { status?: number }, fallback: string): void {
    if (error.status === 401) {
      this.auth.logout();
      this.sessionExpired.set(true);
      this.error.set('');
      return;
    }
    this.error.set(fallback);
  }

  private fromProduct(product: ProductDto): CartItem {
    return { id: product.id, name: product.name, detail: product.categoryName || 'Market pick', price: product.price, image: product.images[0] || this.placeholderImage, quantity: 1, availableStock: product.stockQuantity, isAvailable: product.isAvailable };
  }

  private readApiError(error: { error?: unknown }, fallback: string): string {
    const body = error.error as { error?: string; detail?: string; message?: string } | undefined;
    return body?.detail || body?.error || body?.message || fallback;
  }

  private get placeholderImage(): string {
    return 'https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=560&q=80';
  }

  private readSavedItems(): CartItem[] {
    try {
      const value = JSON.parse(localStorage.getItem('near-market-cart') || '[]') as CartItem[];
      return Array.isArray(value) ? value.filter(item => item && typeof item.id === 'string' && Number(item.quantity) > 0) : [];
    } catch { return []; }
  }
}
