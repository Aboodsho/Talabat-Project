import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { CatalogService, CategoryDto, ProductDto } from '../../core/catalog.service';
import { ProductCardComponent } from '../../shared/product-card.component';

@Component({
  selector: 'app-explore', standalone: true,
  imports: [CommonModule, FormsModule, ProductCardComponent],
  template: `
    <main class="page">
      <small class="eyebrow">THE LIVE MARKET CATALOG</small>
      <h1>Explore the market<span>.</span></h1>
      <p class="intro">Search the catalog and browse results page by page.</p>

      <form class="search" (ngSubmit)="search()">
        <input name="term" [(ngModel)]="term" placeholder="Search products" aria-label="Search catalog">
        <button type="submit" [disabled]="loading()">SEARCH</button>
      </form>
      <nav class="categories" aria-label="Filter by category">
        <button [class.active]="!categoryId()" (click)="selectCategory('')">ALL PRODUCTS</button>
        <button *ngFor="let c of categories()" [class.active]="categoryId()===c.id" (click)="selectCategory(c.id)">{{c.name}}</button>
      </nav>

      <div class="state" *ngIf="loading()">Loading market products…</div>
      <div class="state error" *ngIf="error()">{{error()}} <button (click)="load(pageNumber())">RETRY</button></div>
      <div class="empty" *ngIf="!loading()&&!error()&&!products().length">No products matched. Try another search.</div>
      <div class="result-count" *ngIf="!loading()&&!error()">{{totalCount()}} PRODUCTS · PAGE {{pageNumber()}} OF {{totalPages() || 1}}</div>
      <div class="grid" *ngIf="!loading()&&!error()"><app-product-card *ngFor="let p of products()" [product]="p"></app-product-card></div>

      <nav class="pagination" *ngIf="totalPages()>1" aria-label="Product pages">
        <button (click)="load(pageNumber()-1)" [disabled]="pageNumber()<=1 || loading()">← PREVIOUS</button>
        <span>PAGE {{pageNumber()}} / {{totalPages()}}</span>
        <button (click)="load(pageNumber()+1)" [disabled]="pageNumber()>=totalPages() || loading()">NEXT →</button>
      </nav>
    </main>`,
  styles: [`
    :host{display:block}.page{max-width:1240px;min-height:65vh;margin:auto;padding:34px 24px 65px;color:#17212b}.eyebrow{font-size:10px;letter-spacing:.15em;color:#818991;font-weight:700}h1{font-size:clamp(32px,4.3vw,45px);letter-spacing:-.065em;margin:7px 0}h1 span{color:#ed7900}.intro{color:#77828a;font-size:15px}.search{display:flex;height:46px;max-width:600px;margin:18px 0}.search input{flex:1;min-width:0;border:1px solid #dce1e5;padding:0 13px;font:inherit;font-size:15px}.search button{border:0;background:#ff8a00;color:#17212b;padding:0 20px;font-size:13px;font-weight:800;letter-spacing:.08em;cursor:pointer}.categories{display:flex;gap:7px;overflow:auto;padding:4px 0 17px}.categories button{flex:none;background:white;border:1px solid #dfe3e6;padding:10px 14px;font-size:12px;letter-spacing:.06em;cursor:pointer;transition:transform .18s,border-color .18s,background-color .18s}.categories button.active,.categories button:hover{border-color:#f19a33;background:#fff8f0;transform:translateY(-2px)}.result-count{font-size:11px;letter-spacing:.12em;color:#818991;margin:0 0 11px}.grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));align-items:stretch;gap:14px}.state,.empty{padding:25px;text-align:center;background:white;border:1px solid #e2e6e9;color:#7a858d;font-size:14px}.error{color:#a43b24}.error button{margin-left:10px;padding:9px 12px;border:1px solid #ddd;background:white;cursor:pointer}.pagination{display:flex;justify-content:center;align-items:center;gap:20px;margin-top:23px}.pagination button{min-height:42px;padding:0 15px;border:1px solid #d4d9dd;background:#fff;color:#26323b;cursor:pointer;transition:transform .18s,border-color .18s,background-color .18s}.pagination button:hover:not(:disabled){transform:translateY(-2px);border-color:#ff8a00;background:#fff8f0}.pagination button:disabled{opacity:.45;cursor:not-allowed}.pagination span{font-size:12px;letter-spacing:.1em;color:#7c868e}@media(max-width:900px){.grid{grid-template-columns:repeat(3,minmax(0,1fr))}}@media(max-width:650px){.page{padding:22px 12px 45px}.grid{grid-template-columns:repeat(2,minmax(0,1fr));gap:9px}.pagination{gap:9px}.pagination button{padding-inline:10px}}
  `]
})
export class ExploreComponent {
  private readonly catalog = inject(CatalogService);
  private readonly route = inject(ActivatedRoute);
  readonly products = signal<ProductDto[]>([]);
  readonly categories = signal<CategoryDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly pageNumber = signal(1);
  readonly totalPages = signal(0);
  readonly totalCount = signal(0);
  readonly categoryId = signal('');
  readonly pageSize = 12;
  term = '';

  constructor() {
    this.catalog.getCategories(50).subscribe({ next: result => this.categories.set(result.items), error: () => {} });
    this.route.queryParamMap.subscribe(params => {
      this.term = params.get('q') || '';
      this.load(1);
    });
  }

  search(): void { this.load(1); }
  selectCategory(id: string): void { this.categoryId.set(id); this.load(1); }

  load(page: number): void {
    if (page < 1 || (this.totalPages() && page > this.totalPages())) return;
    this.loading.set(true);
    this.error.set('');
    this.catalog.getProducts(this.pageSize, this.term, this.categoryId() || undefined, page).subscribe({
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
      error: () => { this.error.set('We could not load catalog results. Please retry.'); this.loading.set(false); }
    });
  }
}
