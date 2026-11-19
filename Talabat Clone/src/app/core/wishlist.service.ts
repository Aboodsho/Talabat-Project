import { Injectable, computed, signal } from '@angular/core';
import { ProductDto } from './catalog.service';

export interface WishlistItem { id: string; name: string; detail: string; price: number; image: string; isAvailable: boolean; }

@Injectable({ providedIn: 'root' })
export class WishlistService {
  readonly items = signal<WishlistItem[]>(this.read());
  readonly count = computed(() => this.items().length);

  add(product: ProductDto): void {
    if (this.items().some(item => item.id === product.id)) return;
    this.items.update(items => [...items, { id: product.id, name: product.name, detail: product.categoryName || 'Market pick', price: product.price, image: product.images?.[0] || '', isAvailable: product.isAvailable }]);
    this.persist();
  }
  remove(id: string): void { this.items.update(items => items.filter(item => item.id !== id)); this.persist(); }
  toggle(product: ProductDto): void { this.items().some(item => item.id === product.id) ? this.remove(product.id) : this.add(product); }
  has(id: string): boolean { return this.items().some(item => item.id === id); }
  private persist(): void { try { localStorage.setItem('near-market-wishlist', JSON.stringify(this.items())); } catch { /* storage may be unavailable */ } }
  private read(): WishlistItem[] { try { const value = JSON.parse(localStorage.getItem('near-market-wishlist') || '[]'); return Array.isArray(value) ? value : []; } catch { return []; } }
}
