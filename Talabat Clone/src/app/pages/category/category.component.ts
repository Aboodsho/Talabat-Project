import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { CatalogService, ProductDto } from '../../core/catalog.service';
import { ProductCardComponent } from '../../shared/product-card.component';

@Component({
  selector: 'app-category', standalone: true,
  imports: [CommonModule, FormsModule, ProductCardComponent],
  template: `
    <main class="page">
      <small class="eyebrow">THE MARKET CATALOG</small>
      <h1>{{name()}}<span>.</span></h1>
      <div class="layout">
        <aside>
          <b>FILTER PRODUCTS</b>
          <form class="search" (ngSubmit)="search()"><label for="category-search">SEARCH THIS CATEGORY</label><input id="category-search" name="term" [(ngModel)]="searchTerm" placeholder="Product name"><button type="submit">SEARCH</button></form>
          <label class="price-filter">MAX PRICE <strong>EGP {{maxPrice}}</strong><input type="range" min="0" [max]="ceiling" step="10" [(ngModel)]="maxPrice" (change)="applyFilters()"></label>
          <button class="reset" (click)="resetFilters()">RESET FILTERS</button>
        </aside>
        <section>
          <div class="count" *ngIf="!loading()">{{totalCount()}} PRODUCTS · PAGE {{pageNumber()}} OF {{totalPages() || 1}}</div>
          <div class="state" *ngIf="loading()">Loading products…</div>
          <div class="state error" *ngIf="error()">{{error()}} <button (click)="load(pageNumber())">RETRY</button></div>
          <div class="empty" *ngIf="!loading()&&!error()&&!products().length">No products match these filters.</div>
          <div class="grid" *ngIf="!loading()&&!error()&&products().length"><app-product-card *ngFor="let p of products()" [product]="p"></app-product-card></div>
          <nav class="pagination" *ngIf="totalPages()>1" aria-label="Category product pages"><button (click)="load(pageNumber()-1)" [disabled]="pageNumber()<=1 || loading()">← PREVIOUS</button><span>PAGE {{pageNumber()}} / {{totalPages()}}</span><button (click)="load(pageNumber()+1)" [disabled]="pageNumber()>=totalPages() || loading()">NEXT →</button></nav>
        </section>
      </div>
    </main>`,
  styles: [`
    :host{display:block}.page{max-width:1240px;margin:auto;min-height:65vh;padding:35px 24px 65px;color:#17212b}.eyebrow{font-size:10px;letter-spacing:.15em;color:#818991;font-weight:700}h1{font-size:clamp(32px,4.3vw,45px);letter-spacing:-.06em;margin:7px 0 22px}h1 span{color:#ed7900}.layout{display:grid;grid-template-columns:235px 1fr;gap:20px;align-items:start}.layout aside{background:#fff;border:1px solid #e2e6e9;padding:17px;height:max-content;position:sticky;top:15px}.layout aside>b{font-size:12px;letter-spacing:.12em}.search{display:grid;gap:8px;margin:20px 0}.search label,.price-filter{font-size:11px;letter-spacing:.08em;color:#6b747b}.search input{min-width:0;height:42px;padding:0 10px;border:1px solid #dce1e5;font:inherit;font-size:14px}.search button,.reset{border:0;background:#ff8a00;color:#17212b;padding:11px;width:100%;font-size:12px;font-weight:750;letter-spacing:.08em;cursor:pointer;transition:transform .18s,background-color .18s}.search button:hover,.reset:hover{transform:translateY(-2px);background:#ffa126}.price-filter{display:grid;gap:12px;margin:20px 0}.price-filter strong{font-size:15px;letter-spacing:0;color:#17212b}.price-filter input{accent-color:#f18a16;width:100%}.reset{background:white;border:1px solid #dce1e5;color:#34404a}.count{font-size:11px;letter-spacing:.12em;color:#818991;margin-bottom:12px}.grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));align-items:stretch;gap:13px}.state,.empty{padding:26px;background:white;border:1px solid #e2e6e9;text-align:center;color:#7a858e;font-size:14px}.error{color:#a43b24}.error button{margin-left:9px;border:1px solid #ddd;background:white;padding:8px 12px;cursor:pointer}.pagination{display:flex;justify-content:center;align-items:center;gap:18px;margin-top:22px}.pagination button{min-height:42px;padding:0 14px;border:1px solid #dce1e5;background:#fff;cursor:pointer;transition:transform .18s,border-color .18s}.pagination button:hover:not(:disabled){transform:translateY(-2px);border-color:#ff8a00}.pagination button:disabled{opacity:.45;cursor:not-allowed}.pagination span{font-size:12px;letter-spacing:.1em;color:#7c868e}@media(max-width:950px){.grid{grid-template-columns:repeat(2,minmax(0,1fr))}}@media(max-width:680px){.page{padding:23px 12px 45px}.layout{grid-template-columns:1fr}.layout aside{position:static}.grid{grid-template-columns:repeat(2,minmax(0,1fr));gap:9px}}
  `]
})
export class CategoryComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly catalog = inject(CatalogService);
  readonly products = signal<ProductDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly name = signal('Category');
  readonly pageNumber = signal(1);
  readonly totalPages = signal(0);
  readonly totalCount = signal(0);
  readonly pageSize = 12;
  readonly ceiling = 10000;
  maxPrice = this.ceiling;
  searchTerm = '';
  private categoryId = '';

  constructor() {
    this.route.paramMap.subscribe(params => {
      this.categoryId = params.get('id') || '';
      this.searchTerm = '';
      this.maxPrice = this.ceiling;
      this.catalog.getCategories(50).subscribe(result => this.name.set(result.items.find(category => category.id === this.categoryId)?.name || 'Category'));
      this.load(1);
    });
  }

  search(): void { this.load(1); }
  applyFilters(): void { this.load(1); }
  resetFilters(): void { this.searchTerm = ''; this.maxPrice = this.ceiling; this.load(1); }
  load(page: number): void {
    if (page < 1 || (this.totalPages() && page > this.totalPages())) return;
    this.loading.set(true);
    this.error.set('');
    this.catalog.getProducts(this.pageSize, this.searchTerm, this.categoryId, page, undefined, this.maxPrice).subscribe({
      next: result => {
        const pages = Math.ceil(result.totalCount / this.pageSize);
        if (!result.items.length && result.totalCount > 0 && page > 1) {
          this.totalCount.set(result.totalCount);
          this.totalPages.set(pages);
          this.load(Math.min(page - 1, pages));
          return;
        }
        this.products.set(result.items);
        this.pageNumber.set(Math.max(1, Math.min(page, Math.max(pages, 1))));
        this.totalPages.set(pages);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: () => { this.error.set('We could not load this category. Please retry.'); this.loading.set(false); }
    });
  }
}
