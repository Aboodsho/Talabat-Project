import { CommonModule } from '@angular/common';
import { Component, Input, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideDynamicIcon, LucideHeart, LucideMinus, LucidePlus } from '@lucide/angular';
import { CartService } from '../core/cart.service';
import { ProductDto } from '../core/catalog.service';
import { ToastService } from '../core/toast.service';
import { WishlistService } from '../core/wishlist.service';

@Component({
  selector: 'app-product-card', standalone: true,
  imports: [CommonModule, RouterLink, LucideDynamicIcon],
  template: `
    <article class="product-card">
      <a class="product-photo" [routerLink]="['/product', product.id]">
        <img [src]="image" [alt]="product.name" loading="lazy" decoding="async">
        <span class="stock-label" [class.unavailable]="!product.isAvailable">{{product.isAvailable ? 'IN STOCK' : 'OUT OF STOCK'}}</span>
      </a>
      <button class="wishlist-card-button" [class.saved]="wishlist.has(product.id)" (click)="toggleWishlist()" [attr.aria-label]="(wishlist.has(product.id) ? 'Remove ' : 'Add ') + product.name + ' ' + (wishlist.has(product.id) ? 'from' : 'to') + ' wishlist'" [attr.aria-pressed]="wishlist.has(product.id)"><svg [lucideIcon]="icons.Heart" [size]="19"></svg></button>
      <div class="product-info">
        <div class="product-category">{{product.categoryName || 'MARKET PICK'}}</div>
        <h3><a [routerLink]="['/product', product.id]">{{product.name}}</a></h3>
        <p>{{product.description || 'A practical pick for everyday life.'}}</p>
        <div class="product-purchase"><strong>EGP {{product.price | number:'1.2-2'}}</strong>
          <div class="quantity-control" aria-label="Choose quantity">
            <button type="button" (click)="decrease()" [disabled]="quantity() <= 1" [attr.aria-label]="'Decrease ' + product.name + ' quantity'"><svg [lucideIcon]="icons.Minus" [size]="15"></svg></button>
            <span aria-live="polite">{{quantity()}}</span>
            <button type="button" (click)="increase()" [disabled]="quantity() >= maximum" [attr.aria-label]="'Increase ' + product.name + ' quantity'"><svg [lucideIcon]="icons.Plus" [size]="15"></svg></button>
          </div>
        </div>
        <button class="card-add" type="button" (click)="addToCart()" [disabled]="!product.isAvailable || maximum < 1"><svg [lucideIcon]="icons.Plus" [size]="16"></svg> ADD TO CART</button>
      </div>
    </article>`,
  styles: [`
    :host{display:block;min-width:0;height:100%}.product-card{height:100%;min-height:100%;min-width:0;position:relative;overflow:hidden;display:flex;flex-direction:column;background:#fff;border:1px solid #e2e6e9;transition:transform .22s ease,box-shadow .22s ease,border-color .22s ease}.product-card:hover{transform:translateY(-4px);box-shadow:0 12px 25px #17212b1c;border-color:#f4a13e}.product-photo{width:100%;aspect-ratio:4/3;flex:none;position:relative;display:block;overflow:hidden;background:#f5f4ec}.product-photo img{width:100%;height:100%;object-fit:cover;transition:transform .35s ease}.product-card:hover .product-photo img{transform:scale(1.045)}.stock-label{position:absolute;left:10px;top:10px;padding:5px 7px;background:#fff;color:#557b4b;font-size:9px;letter-spacing:.08em;font-weight:700}.stock-label.unavailable{color:#9a4a41}.product-info{padding:13px 14px 15px;flex:1;display:flex;flex-direction:column;justify-content:space-between;gap:8px}.product-category{font-size:10px;letter-spacing:.12em;color:#828c94;text-transform:uppercase}.product-info h3{font-size:16px;line-height:1.35;font-weight:700;margin:0;min-height:43px;display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;overflow:hidden}.product-info h3 a{color:inherit;text-decoration:none}.product-info>p{font-size:13px;line-height:1.45;color:#727d86;margin:0;min-height:38px;display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;overflow:hidden}.product-purchase{display:flex;justify-content:space-between;align-items:center;gap:7px;margin-top:auto;padding-top:11px;border-top:1px solid #edf0f2}.product-purchase>strong{font-size:18px;white-space:nowrap}.quantity-control{height:38px;display:flex;align-items:center;gap:8px}.quantity-control button{width:36px;height:36px;border:0;border-radius:50%;display:grid;place-items:center;background:#f1f2f3;color:#17212b;cursor:pointer;transition:transform .18s ease,background-color .18s ease,color .18s ease}.quantity-control button:last-child{background:#fff0e2}.quantity-control button:hover:not(:disabled){transform:scale(1.1);background:#ffd9b2;color:#9b4a00}.quantity-control button:disabled{opacity:.4;cursor:not-allowed}.quantity-control span{min-width:16px;text-align:center;font-size:16px;font-weight:750}.card-add{width:100%;min-height:46px;margin-top:3px;border:0;border-radius:2px;background:#ff8a00;color:#17212b;display:flex;align-items:center;justify-content:center;gap:8px;font-weight:800;font-size:13px;letter-spacing:.04em;cursor:pointer;transition:transform .2s ease,background-color .2s ease,box-shadow .2s ease}.card-add:hover:not(:disabled){transform:translateY(-2px);background:#f17b00;box-shadow:0 7px 14px #ff8a0033}.card-add:active:not(:disabled){transform:scale(.98)}.card-add:disabled{opacity:.5;cursor:not-allowed}.wishlist-card-button{position:absolute;z-index:2;top:10px;right:10px;width:40px;height:40px;padding:0;border:0;background:#fff;color:#48545e;display:grid;place-items:center;cursor:pointer;box-shadow:0 2px 9px #17212b22;transition:transform .2s ease,color .2s ease,background-color .2s ease}.wishlist-card-button:hover{transform:scale(1.1);background:#fff0e2;color:#e84c3d}.wishlist-card-button.saved{color:#e84c3d}.wishlist-card-button.saved svg{fill:#e84c3d}@media(max-width:600px){.product-info{padding:10px}.product-info h3{font-size:14px}.product-info>p{font-size:12px;min-height:35px}.product-purchase>strong{font-size:15px}.quantity-control{gap:3px}.quantity-control button{width:31px;height:31px}.card-add{font-size:12px;min-height:42px}}
  `]
})
export class ProductCardComponent {
  @Input({ required: true }) product!: ProductDto;
  readonly cart = inject(CartService);
  readonly wishlist = inject(WishlistService);
  private readonly toast = inject(ToastService);
  readonly quantity = signal(1);
  readonly icons = { Heart: LucideHeart, Plus: LucidePlus, Minus: LucideMinus };
  get maximum(): number { return Math.min(this.cart.remainingFor(this.product.id) || (this.cart.items().some(item => item.id === this.product.id) ? 0 : 10), this.product.stockQuantity ?? 10); }
  get image(): string { return this.product.images?.[0] || 'https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=720&q=80'; }
  increase(): void { this.quantity.update(value => Math.min(this.maximum, value + 1)); }
  decrease(): void { this.quantity.update(value => Math.max(1, value - 1)); }
  addToCart(): void {
    this.cart.error.set('');
    this.cart.add(this.product, this.quantity());
    this.toast.show(this.cart.error() || `${this.product.name} added to your basket`);
  }
  toggleWishlist(): void { this.wishlist.toggle(this.product); this.toast.show(this.wishlist.has(this.product.id) ? 'Added to wishlist' : 'Removed from wishlist'); }
}
