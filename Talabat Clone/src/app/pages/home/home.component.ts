import { CommonModule } from '@angular/common';
import { Component, OnDestroy, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LucideDynamicIcon, LucideIcon, LucideMenu, LucideMapPin, LucideSearch, LucideShoppingBag, LucideUserRound, LucideArrowUpRight, LucidePlus, LucideHeart, LucideSparkles, LucideActivity, LucideCarFront, LucideSmartphone, LucideTentTree, LucideBicepsFlexed, LucideShirt, LucideGamepad2, LucideDumbbell, LucidePackage, LucideMilk, LucideCroissant, LucideCarrot, LucideWheat, LucideBeef, LucideCupSoda, LucideApple, LucideFish, LucideCakeSlice } from '@lucide/angular';
import { AuthService } from '../../core/auth.service';
import { CartService } from '../../core/cart.service';
import { CatalogService, CategoryDto, ProductDto } from '../../core/catalog.service';
import { ToastService } from '../../core/toast.service';
import { WishlistService } from '../../core/wishlist.service';
import { PasswordRequirementsComponent } from '../auth/password-requirements.component';
import { ProductCardComponent } from '../../shared/product-card.component';

interface CategoryCard extends CategoryDto { icon: LucideIcon; tone: string; }
interface HeroSlide { first: string; second: string; description: string; action: string; route: string; image: string; fragment?: string; }

@Component({
  selector: 'app-home', standalone: true, imports: [CommonModule, FormsModule, RouterLink, LucideDynamicIcon, PasswordRequirementsComponent, ProductCardComponent],
  templateUrl: './home.component.html', styleUrl: './home.component.css'
})
export class HomeComponent implements OnDestroy {
  readonly cart = inject(CartService);
  readonly wishlist = inject(WishlistService);
  readonly auth = inject(AuthService);
  private readonly catalog = inject(CatalogService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  readonly icons = { Menu: LucideMenu, MapPin: LucideMapPin, Search: LucideSearch, ShoppingBag: LucideShoppingBag, UserRound: LucideUserRound, ArrowUpRight: LucideArrowUpRight, Plus: LucidePlus, Heart: LucideHeart, Package: LucidePackage };
  readonly categories = signal<CategoryCard[]>([]);
  readonly products = signal<ProductDto[]>([]);
  readonly animatedUserTotal = signal(0);
  readonly animatedProductTotal = signal(0);
  readonly animatedOrderTotal = signal(0);
  readonly loading = signal(true);
  readonly loadingMore = signal(false);
  readonly hasMore = signal(false);
  readonly categoryError = signal('');
  readonly productError = signal('');
  readonly loginOpen = signal(false);
  readonly loginError = signal('');
  readonly authMode = signal<'login' | 'register' | 'registered'>('login');
  readonly registrationError = signal('');
  searchTerm = '';
  loginEmail = '';
  loginPassword = '';
  registerName = '';
  readonly passwordPattern = '(?=.*[A-Z])(?=.*[a-z])(?=.*[0-9]).{8,20}';
  readonly selectedCategoryId = signal<string | null>(null);
  readonly activeSlide = signal(0);
  readonly heroSlides: HeroSlide[] = [
    { first: 'Find your next', second: 'favourite thing.', description: 'Explore a thoughtfully chosen mix of everyday goods, from useful essentials to new discoveries.', action: 'EXPLORE THE MARKET', route: '/explore', image: 'https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=2200&q=85' },
    { first: 'Good finds,', second: 'just a click away.', description: 'Browse the catalog, compare your picks, and build a basket that feels like yours.', action: 'BROWSE PRODUCTS', route: '/explore', image: 'https://images.unsplash.com/photo-1543168256-418811576931?auto=format&fit=crop&w=2200&q=85' },
    { first: 'Everyday goods,', second: 'well chosen.', description: 'Discover what is available in the market and keep your favourite finds close.', action: 'OUR STORY', route: '/', fragment: 'about-us', image: 'https://images.unsplash.com/photo-1509440159596-0249088772ff?auto=format&fit=crop&w=2200&q=85' }
  ];
  private pageNumber = 1;
  private readonly sliderTimer = setInterval(() => this.nextSlide(), 5500);
  private readonly countTimers: ReturnType<typeof setInterval>[] = [];

  constructor() {
    this.route.queryParamMap.subscribe(params => {
      const account = params.get('account');
      if (account) { this.authMode.set(account === 'register' ? 'register' : 'login'); this.loginOpen.set(true); }
      const query = params.get('q');
      if (query !== null) { this.searchTerm = query; this.loadProducts(); }
    });
    this.loadCategories();
    this.loadProducts();
    this.animateCount(40_000, value => this.animatedUserTotal.set(value));
    this.animateCount(70_000, value => this.animatedProductTotal.set(value));
    this.animateCount(149, value => this.animatedOrderTotal.set(value));
  }

  ngOnDestroy(): void { clearInterval(this.sliderTimer); this.countTimers.forEach(timer => clearInterval(timer)); }
  nextSlide(): void { this.activeSlide.update(index => (index + 1) % this.heroSlides.length); }

  loadCategories(): void {
    this.categoryError.set('');
    this.catalog.getCategories(3).subscribe({
      next: result => this.categories.set(result.items.map(category => ({ ...category, icon: this.categoryIcon(category.name), tone: this.categoryTone(category.name) }))),
      error: () => this.categoryError.set('Category request failed.')
    });
  }

  loadProducts(categoryId?: string, event?: Event): void {
    event?.preventDefault();
    this.selectedCategoryId.set(categoryId ?? null);
    this.pageNumber = 1;
    this.loading.set(true);
    this.loadingMore.set(false);
    this.productError.set('');
    this.catalog.getProducts(3, this.searchTerm, categoryId, this.pageNumber).subscribe({
      next: result => { this.products.set(result.items); this.hasMore.set(result.pageNumber < result.totalPages); this.loading.set(false); },
      error: () => { this.products.set([]); this.hasMore.set(false); this.productError.set('Product request failed.'); this.loading.set(false); }
    });
  }

  searchProducts(): void { this.loadProducts(this.selectedCategoryId() || undefined); }

  selectedCategoryName(): string | null {
    return this.categories().find(category => category.id === this.selectedCategoryId())?.name || null;
  }

  retryCatalog(): void { this.loadCategories(); this.loadProducts(this.selectedCategoryId() || undefined); }

  clearFilters(event: Event): void {
    this.searchTerm = '';
    this.loadProducts(undefined, event);
  }

  loadMore(): void {
    if (!this.hasMore() || this.loadingMore()) return;
    const nextPage = this.pageNumber + 1;
    this.loadingMore.set(true);
    this.catalog.getProducts(3, this.searchTerm, this.selectedCategoryId() || undefined, nextPage).subscribe({
      next: result => {
        this.pageNumber = result.pageNumber;
        this.products.update(items => [...items, ...result.items]);
        this.hasMore.set(result.pageNumber < result.totalPages);
        this.loadingMore.set(false);
      },
      error: () => { this.productError.set('More products could not be loaded. Please retry.'); this.loadingMore.set(false); }
    });
  }

  add(product: ProductDto): void { this.cart.add(product); this.toast.show(`${product.name} added to your basket`); }
  toggleWishlist(product: ProductDto): void { this.wishlist.toggle(product); this.toast.show(this.wishlist.has(product.id) ? 'Added to wishlist' : 'Removed from wishlist'); }

  private animateCount(target: number, update: (value: number) => void): void {
    const started = Date.now();
    const duration = 1300;
    const timer = setInterval(() => {
      const progress = Math.min((Date.now() - started) / duration, 1);
      update(Math.round(target * (1 - Math.pow(1 - progress, 3))));
      if (progress >= 1) clearInterval(timer);
    }, 24);
    this.countTimers.push(timer);
  }

  accountAction(): void {
    if (this.auth.isAuthenticated()) { this.auth.logout(); this.cart.clear(); return; }
    this.openLogin();
  }

  signIn(): void {
    this.loginError.set('');
    this.auth.login(this.loginEmail, this.loginPassword).subscribe({
      next: response => {
        this.loginOpen.set(false);
        this.loginPassword = '';
        const roles = response.user?.roles ?? [];
        if (roles.some(role => role.trim().toLowerCase() === 'admin')) {
          this.router.navigate(['/admin']);
          return;
        }
        this.cart.syncGuestCart();
      },
      error: error => this.loginError.set(error.status === 403 ? 'Confirm your email before signing in.' : 'Sign in failed. Check your email and password.')
    });
  }

  register(): void {
    this.registrationError.set('');
    if (!this.passwordMeetsPolicy(this.loginPassword)) { this.registrationError.set('Use 8–20 characters with at least one uppercase letter, one lowercase letter, and one number.'); return; }
    this.auth.register(this.registerName, this.loginEmail, this.loginPassword).subscribe({
      next: () => { this.loginPassword = ''; this.authMode.set('registered'); },
      error: error => this.registrationError.set(error.error?.error || 'We could not create your account. Check the details and try again.')
    });
  }

  private passwordMeetsPolicy(password: string): boolean { return password.length >= 8 && password.length <= 20 && /[A-Z]/.test(password) && /[a-z]/.test(password) && /[0-9]/.test(password); }

  openLogin(): void { this.authMode.set('login'); this.loginError.set(''); this.loginOpen.set(true); }

  productImage(product: ProductDto): string {
    return product.images?.[0] || 'https://images.unsplash.com/photo-1441986300917-64674bd600d8?auto=format&fit=crop&w=560&q=80';
  }

  private categoryIcon(name: string): LucideIcon {
    const value = name.toLowerCase();
    if (value.includes('dairy') || value.includes('milk') || value.includes('لبن')) return LucideMilk;
    if (value.includes('bakery') || value.includes('bread') || value.includes('مخبوز')) return LucideCroissant;
    if (value.includes('produce') || value.includes('fruit') || value.includes('vegetable') || value.includes('خضار')) return LucideCarrot;
    if (value.includes('pantry') || value.includes('grain') || value.includes('مطبخ')) return LucideWheat;
    if (value.includes('meat') || value.includes('beef') || value.includes('لحم')) return LucideBeef;
    if (value.includes('beverage') || value.includes('drink') || value.includes('مشروب')) return LucideCupSoda;
    if (value.includes('seafood') || value.includes('fish') || value.includes('سمك')) return LucideFish;
    if (value.includes('snack') || value.includes('dessert') || value.includes('sweet')) return LucideCakeSlice;
    if (value.includes('fruit') || value.includes('apple') || value.includes('فاكه')) return LucideApple;
    if (value.includes('health') || value.includes('beauty')) return LucideSparkles;
    if (value.includes('supplement') || value.includes('مكمل')) return LucideActivity;
    if (value.includes('automotive')) return LucideCarFront;
    if (value.includes('electronics')) return LucideSmartphone;
    if (value.includes('outdoor')) return LucideTentTree;
    if (value.includes('accessor') || value.includes('إكسسوار')) return LucideBicepsFlexed;
    if (value.includes('clothing') || value.includes('apparel') || value.includes('ملابس')) return LucideShirt;
    if (value.includes('toy') || value.includes('game')) return LucideGamepad2;
    if (value.includes('exercise') || value.includes('equipment') || value.includes('تمرين')) return LucideDumbbell;
    const uniqueFallbacks = [LucidePackage, LucideActivity, LucideCarFront, LucideSmartphone, LucideTentTree, LucideBicepsFlexed, LucideShirt, LucideGamepad2, LucideDumbbell, LucideSparkles];
    const hash = [...name].reduce((sum, char) => sum + char.charCodeAt(0), 0);
    return uniqueFallbacks[hash % uniqueFallbacks.length];
  }

  private categoryTone(name: string): string {
    const tones = ['green', 'cream', 'blue', 'yellow', 'pink', 'lavender'];
    return tones[Math.abs([...name].reduce((sum, char) => sum + char.charCodeAt(0), 0)) % tones.length];
  }
}
