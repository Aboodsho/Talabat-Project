import { CommonModule, DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, forkJoin } from 'rxjs';
import { LucideActivity, LucideArrowUpRight, LucideBoxes, LucideDynamicIcon, LucideRefreshCw, LucideShoppingBag, LucideTags, LucideTruck } from '@lucide/angular';
import { AdminCategorySummary, AdminOrderDetail, AdminOrderSummary, AdminProductSummary, AdminService } from '../../core/admin.service';
import { AuthService } from '../../core/auth.service';
import { ProductReview, ReviewService } from '../../core/review.service';

@Component({
  selector: 'app-admin-dashboard', standalone: true,
  imports: [CommonModule, DatePipe, FormsModule, RouterLink, LucideDynamicIcon],
  template: `
    <main class="admin-page">
      <header class="page-head">
        <div><small class="eyebrow">NEAR MARKET · ADMINISTRATION</small><h1>Market overview<span>.</span></h1><p>Monitor your catalog and keep orders moving.</p></div>
        <button class="refresh" type="button" (click)="load()" [disabled]="loading()"><svg [class.spinning]="loading()" [lucideIcon]="icons.Refresh" [size]="16"></svg> REFRESH</button>
      </header>

      <section class="access-card" *ngIf="!auth.isAuthenticated()"><span class="access-mark">!</span><div><h2>Admin sign-in required</h2><p>Sign in with an account that has the Admin role to view dashboard data.</p></div><a routerLink="/" [queryParams]="{account:'login'}">SIGN IN <svg [lucideIcon]="icons.Arrow" [size]="15"></svg></a></section>
      <section class="access-card denied" *ngIf="auth.isAuthenticated() && !hasAdminAccess()"><span class="access-mark">!</span><div><h2>Admin access required</h2><p>This account does not have the Admin role. Dashboard data is restricted by the API.</p></div><a routerLink="/">BACK TO MARKET <svg [lucideIcon]="icons.Arrow" [size]="15"></svg></a></section>

      <ng-container *ngIf="hasAdminAccess()">
        <div class="error-banner" *ngIf="error()"><span>{{error()}}</span><button type="button" (click)="load()">TRY AGAIN</button></div>
        <section class="metrics" aria-label="Store metrics">
          <article class="metric"><div class="metric-icon"><svg [lucideIcon]="icons.Boxes" [size]="19"></svg></div><small>PRODUCTS</small><strong>{{productCount() | number}}</strong><span>Catalog inventory</span></article>
          <article class="metric"><div class="metric-icon green"><svg [lucideIcon]="icons.Tags" [size]="19"></svg></div><small>CATEGORIES</small><strong>{{categoryCount() | number}}</strong><span>Category records</span></article>
          <article class="metric"><div class="metric-icon blue"><svg [lucideIcon]="icons.Bag" [size]="19"></svg></div><small>ALL ORDERS</small><strong>{{orderCount() | number}}</strong><span>Orders in the system</span></article>
          <article class="metric"><div class="metric-icon orange"><svg [lucideIcon]="icons.Activity" [size]="19"></svg></div><small>NEED ATTENTION</small><strong>{{pendingCount() | number}}</strong><span>Pending orders</span></article>
        </section>

        <section class="dashboard-grid">
          <article class="panel orders-panel">
            <div class="panel-head"><div><small>ORDER OPERATIONS</small><h2>Recent orders</h2></div><a routerLink="/orders">CUSTOMER ORDERS <svg [lucideIcon]="icons.Arrow" [size]="14"></svg></a></div>
            <div class="loading-state" *ngIf="loading()"><i></i> Loading dashboard data…</div>
            <div class="empty-state" *ngIf="!loading() && !error() && !orders().length"><svg [lucideIcon]="icons.Truck" [size]="23"></svg><strong>No orders yet</strong><span>Orders will appear here when customers check out.</span></div>
            <div class="orders-table-wrap" *ngIf="!loading() && orders().length">
              <table><thead><tr><th>ORDER</th><th>PLACED</th><th>STATUS</th><th>TOTAL</th><th>ACTION</th></tr></thead>
                <tbody><tr *ngFor="let order of orders()"><td><strong>#{{order.id | slice:0:8 | uppercase}}</strong><small>{{order.itemsCount}} item(s)</small></td><td>{{order.createdAt | date:'mediumDate'}}</td><td><span class="status" [class.pending]="order.status.toLowerCase()==='pending'" [class.cancelled]="order.status.toLowerCase()==='cancelled'">{{order.status}}</span></td><td class="money">EGP {{order.totalAmount | number:'1.2-2'}}</td><td><div class="actions"><button class="details-action" type="button" (click)="loadOrderDetail(order.id)">{{selectedOrder()?.id===order.id?'HIDE':'DETAILS'}}</button><ng-container *ngIf="nextAction(order) as action"><button type="button" (click)="updateOrder(order,action)" [disabled]="busyOrder()===order.id">{{busyOrder()===order.id?'SAVING…':action.label}}</button><button class="cancel" type="button" *ngIf="order.status.toLowerCase()!=='cancelled' && order.status.toLowerCase()!=='delivered'" (click)="updateOrder(order,{key:'cancel',label:'CANCEL'})" [disabled]="busyOrder()===order.id" aria-label="Cancel order">×</button></ng-container></div></td></tr></tbody>
              </table>
            </div>
          </article>
          <aside class="panel side-panel"><small>QUICK ACCESS</small><h2>Manage the market</h2><a routerLink="/explore"><span class="quick-icon"><svg [lucideIcon]="icons.Boxes" [size]="17"></svg></span><span><strong>Product catalog</strong><small>Browse products currently on sale</small></span><svg class="quick-arrow" [lucideIcon]="icons.Arrow" [size]="14"></svg></a><a routerLink="/categories"><span class="quick-icon green"><svg [lucideIcon]="icons.Tags" [size]="17"></svg></span><span><strong>Categories</strong><small>Open the public category catalog</small></span><svg class="quick-arrow" [lucideIcon]="icons.Arrow" [size]="14"></svg></a><div class="api-note"><svg [lucideIcon]="icons.Activity" [size]="16"></svg><span><strong>Connected to admin API</strong><small>Orders, products, and categories are protected by the Admin role.</small></span></div></aside>
        </section>
        <section class="management-grid">
          <article class="panel inventory-panel"><div class="panel-head"><div><small>STOCK & AVAILABILITY</small><h2>Product inventory <span class="inventory-total">{{productCount() | number}} products</span></h2></div><a routerLink="/explore">VIEW MARKET <svg [lucideIcon]="icons.Arrow" [size]="14"></svg></a></div><div class="create-launch product-launch"><div><span class="launch-icon">＋</span><span><strong>Add a product</strong><small>Create a catalog item with its price, stock and category.</small></span></div><button type="button" (click)="openProductCreate()">＋ NEW PRODUCT</button></div><form class="product-search" (ngSubmit)="searchProducts()"><span class="search-mark">⌕</span><input name="productSearchTerm" [(ngModel)]="productSearchTerm" (ngModelChange)="onProductSearchChange($event)" placeholder="Search products by name…" aria-label="Search products"><span class="search-live" *ngIf="productsLoading()">SEARCHING…</span><button class="clear-search" *ngIf="productSearchTerm" type="button" (click)="clearProductSearch()">CLEAR</button><span class="search-spacer"></span><small>SHOWING {{products().length}} OF {{productCount()}}</small></form><div class="inventory-list"><article class="inventory-row" *ngFor="let product of products()" [class.low-stock]="product.stockQuantity <= 5" [class.category-open]="openCategoryMenu()===product.id"><span class="product-mark"><img *ngIf="product.images.length" [src]="product.images[0]" [alt]="" loading="lazy"><svg *ngIf="!product.images.length" [lucideIcon]="icons.Boxes" [size]="17"></svg></span><span class="inventory-name"><strong>{{product.name}}</strong><small>{{product.categoryName || 'Uncategorized'}} · EGP {{product.price | number:'1.2-2'}}</small></span><span class="stock-control"><button type="button" (click)="changeStock(product,-1)" [disabled]="busyProduct()===product.id || product.stockQuantity<=0" aria-label="Decrease stock">−</button><b>{{product.stockQuantity}}</b><small class="stock-alert" *ngIf="product.stockQuantity<=5">LOW</small><button type="button" (click)="changeStock(product,1)" [disabled]="busyProduct()===product.id" aria-label="Increase stock">+</button></span><div class="category-picker row-category-picker"><button type="button" class="category-picker-trigger" [class.chosen]="product.categoryId" (click)="toggleCategoryMenu(product.id)"><span><small>CATEGORY</small><strong>{{selectedCategoryName(product.categoryId || '')}}</strong></span><b>⌄</b></button><div class="category-picker-menu" *ngIf="openCategoryMenu()===product.id"><label class="category-picker-search"><span>⌕</span><input [name]="'category-search-'+product.id" [(ngModel)]="categorySearch" placeholder="Find a category…" autocomplete="off"></label><button type="button" class="category-option" (click)="chooseCategory(product.id,'')"><span>No category</span><b *ngIf="!product.categoryId">✓</b></button><button type="button" class="category-option" *ngFor="let category of filteredCategoryOptions()" (click)="chooseCategory(product.id,category.id)"><span>{{category.name}}</span><b *ngIf="product.categoryId===category.id">✓</b></button><p class="category-no-results" *ngIf="!filteredCategoryOptions().length">No categories found</p></div></div><button class="row-action" type="button" (click)="editProduct(product)">EDIT</button><button class="row-action delete" type="button" (click)="deleteProduct(product)">×</button><button class="active-toggle" [class.inactive]="!product.isActive" type="button" (click)="toggleProduct(product)" [disabled]="busyProduct()===product.id">{{product.isActive?'ACTIVE':'INACTIVE'}}</button></article><div class="empty-small" *ngIf="!loading() && !productsLoading() && !products().length">{{productSearchTerm ? 'No products match your search.' : 'No products were returned by the admin catalog.'}}</div></div><nav class="list-pagination" *ngIf="productPages()>1"><button type="button" (click)="goProducts(productPage()-1)" [disabled]="productPage()<=1 || loading() || productsLoading()">← PREVIOUS</button><span>PAGE {{productPage()}} OF {{productPages()}}</span><button type="button" (click)="goProducts(productPage()+1)" [disabled]="productPage()>=productPages() || loading() || productsLoading()">NEXT →</button></nav></article>
          <article class="panel category-panel"><div class="panel-head"><div><small>CATALOG STRUCTURE</small><h2>Categories <span class="category-total">{{categoryFilterTerm.trim() ? visibleCategories().length : categoryCount()}} {{categoryFilterTerm.trim() ? 'matches' : 'total'}}</span></h2></div></div><form class="category-search" (ngSubmit)="$event.preventDefault()"><span class="search-mark">⌕</span><input name="categoryFilterTerm" [(ngModel)]="categoryFilterTerm" placeholder="Search all categories…" aria-label="Search categories"><button *ngIf="categoryFilterTerm" type="button" (click)="categoryFilterTerm=''">CLEAR</button></form><div class="create-launch category-launch"><div><span class="launch-icon">＋</span><span><strong>Create a category</strong><small>Organize related products into a clear catalog group.</small></span></div><button type="button" (click)="openCategoryCreate()">＋ NEW CATEGORY</button></div><div class="category-list"><div *ngFor="let category of visibleCategories()"><span class="quick-icon green"><svg [lucideIcon]="icons.Tags" [size]="15"></svg></span><span><strong>{{category.name}}</strong><small>{{category.isDeleted?'Archived':'Available in catalog'}}</small></span><b [class.archived]="category.isDeleted">{{category.isDeleted?'ARCHIVED':'ACTIVE'}}</b><button class="row-action" type="button" (click)="editCategory(category)">EDIT</button><button class="row-action delete" type="button" (click)="deleteCategory(category)">×</button></div><div class="empty-small" *ngIf="categoryFilterTerm.trim() && !visibleCategories().length">No categories match this search.</div></div><nav class="list-pagination category-pagination" *ngIf="categoryPages()>1 && !categoryFilterTerm.trim()"><button type="button" (click)="goCategories(categoryPage()-1)" [disabled]="categoryPage()<=1 || loading()">← PREVIOUS</button><span>PAGE {{categoryPage()}} OF {{categoryPages()}}</span><button type="button" (click)="goCategories(categoryPage()+1)" [disabled]="categoryPage()>=categoryPages() || loading()">NEXT →</button></nav></article>
        </section>
        <section class="panel review-moderation"><div class="panel-head"><div><small>COMMUNITY MODERATION</small><h2>Product reviews</h2></div><label class="review-product-select">PRODUCT<select name="reviewProduct" [(ngModel)]="reviewProductId" (ngModelChange)="loadProductReviews()"><option value="">Choose a product</option><option *ngFor="let product of products()" [value]="product.id">{{product.name}}</option></select></label></div><div class="loading-state" *ngIf="reviewLoading()">Loading reviews…</div><div class="review-admin-list" *ngIf="!reviewLoading() && reviewProductId"><article *ngFor="let review of productReviews()"><div class="review-admin-main"><strong>{{review.authorName}}</strong><span class="stars">{{'★'.repeat(review.rating)}}<i>{{'★'.repeat(5-review.rating)}}</i></span><small>{{review.createdAt | date:'mediumDate'}}</small><p>{{review.comment || 'No written comment.'}}</p></div><button type="button" (click)="removeReview(review)" [disabled]="busyReview()===review.id">{{busyReview()===review.id?'REMOVING…':'REMOVE REVIEW'}}</button></article><div class="empty-small" *ngIf="!productReviews().length">No reviews for this product yet.</div></div><div class="empty-small" *ngIf="!reviewLoading() && !reviewProductId">Choose a product to review its customer feedback.</div></section>
        <footer class="admin-foot">ADMIN DASHBOARD · LIVE DATA FROM YOUR MARKET API</footer>
      </ng-container>
      <div class="workspace-backdrop" *ngIf="editOpen()" (click)="closeEdit()"><section class="create-workspace edit-workspace admin-form-modal" role="dialog" aria-modal="true" aria-labelledby="edit-workspace-title" (click)="$event.stopPropagation()"><form class="workspace-content admin-form-content" (ngSubmit)="saveEdit()"><header class="workspace-top"><div><small>CATALOG MANAGEMENT</small><h2 id="edit-workspace-title">Edit product details</h2></div><button type="button" class="workspace-close" (click)="closeEdit()" aria-label="Close edit form">×</button></header><div class="workspace-body admin-form-body"><section class="workspace-card"><small class="workspace-card-kicker">PRODUCT DETAILS</small><h3>General information</h3><label>Product name<input name="editName" [(ngModel)]="editName" required maxlength="120" autofocus></label><label>Description<textarea name="editDescription" [(ngModel)]="editDescription" maxlength="500" rows="5"></textarea></label><label *ngIf="editType()==='product'">Price · EGP<input name="editPrice" [(ngModel)]="editPrice" type="number" min="0.01" step="0.01" required></label></section><aside class="workspace-aside"><section class="workspace-preview"><small>PRODUCT PREVIEW</small><div class="preview-icon">▧</div><strong>{{editName || 'Product name'}}</strong><span>{{editDescription || 'Product description'}}</span><b>EGP {{editPrice | number:'1.2-2'}}</b></section><section class="workspace-card"><small class="workspace-card-kicker">IMAGE MANAGEMENT</small><h3>Product images</h3><p>Image editing will be available after the admin image API is connected.</p></section></aside></div><p class="workspace-error" *ngIf="editError()">{{editError()}}</p><footer class="workspace-actions admin-form-actions"><button type="button" class="cancel-workspace" (click)="closeEdit()">CANCEL</button><span class="action-spacer"></span><button type="submit" class="publish-workspace" [disabled]="savingEdit() || !editName.trim()">{{savingEdit()?'SAVING…':'SAVE CHANGES'}} <span>↗</span></button></footer></form></section></div><div class="modal-backdrop" *ngIf="selectedOrder() as detail" (click)="closeOrderDetail()"><section class="edit-modal order-modal" role="dialog" aria-modal="true" aria-labelledby="order-modal-title" (click)="$event.stopPropagation()"><button class="modal-close" type="button" (click)="closeOrderDetail()" aria-label="Close order details">×</button><small>ORDER MANAGEMENT · {{detail.status || 'LOADING'}}</small><h2 id="order-modal-title">Order details<span>.</span></h2><div class="order-modal-loading" *ngIf="orderDetailLoading()"><i></i> Loading order details…</div><ng-container *ngIf="!orderDetailLoading()"><div class="order-modal-meta"><span><small>ORDER NUMBER</small><strong>#{{detail.id | slice:0:8 | uppercase}}</strong></span><span><small>PLACED</small><strong>{{detail.createdAt | date:'mediumDate'}}</strong></span><span><small>TOTAL</small><strong>EGP {{detail.totalAmount | number:'1.2-2'}}</strong></span></div><div class="order-modal-address" *ngIf="detail.shippingAddress"><small>DELIVERY ADDRESS</small><span>{{detail.shippingAddress.street}}, {{detail.shippingAddress.city}}<span *ngIf="detail.shippingAddress.state">, {{detail.shippingAddress.state}}</span><span *ngIf="detail.shippingAddress.postalCode"> {{detail.shippingAddress.postalCode}}</span>, {{detail.shippingAddress.country}}</span></div><div class="order-modal-items"><small>ITEMS IN THIS ORDER</small><div *ngFor="let line of detail.items"><span><strong>{{line.prodcutName || line.productName || 'Product'}}</strong><small>Qty {{line.quantity}} × EGP {{line.unitPrice | number:'1.2-2'}}</small></span><b>EGP {{line.lineTotal | number:'1.2-2'}}</b></div><p *ngIf="!detail.items?.length">No item details were returned.</p></div></ng-container></section></div>
    <div class="workspace-backdrop" *ngIf="productCreateOpen()" (click)="closeProductCreate()"><section class="create-workspace admin-form-modal" role="dialog" aria-modal="true" aria-labelledby="product-create-title" (click)="$event.stopPropagation()"><form class="workspace-content admin-form-content" (ngSubmit)="createProduct()"><header class="workspace-top"><div><small>CATALOG MANAGEMENT</small><h2 id="product-create-title">Create a new product</h2></div><button type="button" class="workspace-close" (click)="closeProductCreate()" aria-label="Close product form">×</button></header><div class="workspace-body admin-form-body"><section class="workspace-card product-general" id="product-general"><small class="workspace-card-kicker">01 · PRODUCT DETAILS</small><h3>General information</h3><label>Product name<input name="newProductName" [(ngModel)]="newProductName" placeholder="e.g. Fresh strawberries" required maxlength="120"></label><label>Description<textarea name="newProductDescription" [(ngModel)]="newProductDescription" placeholder="Describe this product for your customers" rows="5" maxlength="500"></textarea><span class="field-hint">A short, clear description helps customers choose.</span></label><div class="workspace-two-fields"><label>Price · EGP<input name="newProductPrice" [(ngModel)]="newProductPrice" type="number" min="0.01" step="0.01" placeholder="0.00" required></label><label>Stock quantity<input name="newProductStock" [(ngModel)]="newProductStock" type="number" min="0" step="1" placeholder="0" required></label></div></section><aside class="workspace-aside"><section class="workspace-card" id="product-inventory"><small class="workspace-card-kicker">02 · ORGANIZATION</small><h3>Product category</h3><p>Choose the category customers will browse this product in.</p><div class="category-picker workspace-category-picker"><button type="button" class="category-picker-trigger" [class.chosen]="newProductCategoryId" (click)="toggleCategoryMenu('new')"><span><small>CATEGORY</small><strong>{{selectedCategoryName(newProductCategoryId)}}</strong></span><b>⌄</b></button><div class="category-picker-menu" *ngIf="openCategoryMenu()==='new'"><label class="category-picker-search"><span>⌕</span><input name="workspaceCategorySearch" [(ngModel)]="categorySearch" placeholder="Search categories…" autocomplete="off"></label><button type="button" class="category-option" (click)="chooseCategory('new','')"><span>No category</span><b *ngIf="!newProductCategoryId">✓</b></button><button type="button" class="category-option" *ngFor="let category of filteredCategoryOptions()" (click)="chooseCategory('new',category.id)"><span>{{category.name}}</span><b *ngIf="newProductCategoryId===category.id">✓</b></button><p class="category-no-results" *ngIf="!filteredCategoryOptions().length">No categories found</p></div></div><div class="workspace-note"><span>i</span><p>New products are created as active and will appear in the market catalog when the API accepts them.</p></div></section><section class="workspace-preview"><small>PRODUCT PREVIEW</small><div class="preview-icon">▧</div><strong>{{newProductName || 'Your product name'}}</strong><span>{{selectedCategoryName(newProductCategoryId)}}</span><b>EGP {{newProductPrice || '0.00'}}</b></section></aside></div><p class="workspace-error" *ngIf="error()">{{error()}}</p><footer class="workspace-actions admin-form-actions"><button type="button" class="cancel-workspace" (click)="closeProductCreate()">CANCEL</button><span class="action-spacer"></span><button type="submit" class="publish-workspace" [disabled]="savingProduct() || !newProductName.trim() || newProductPrice===null">{{savingProduct()?'CREATING…':'CREATE PRODUCT'}} <span>↗</span></button></footer></form></section></div>
<div class="workspace-backdrop" *ngIf="categoryCreateOpen()" (click)="closeCategoryCreate()"><section class="create-workspace category-workspace" role="dialog" aria-modal="true" aria-labelledby="category-create-title" (click)="$event.stopPropagation()"><form class="workspace-content admin-form-content" (ngSubmit)="createCategory()"><header class="workspace-top"><div><small>CATALOG MANAGEMENT</small><h2 id="category-create-title">Create a new category</h2></div><button type="button" class="workspace-close" (click)="closeCategoryCreate()" aria-label="Close category form">×</button></header><div class="workspace-body category-workspace-body admin-form-body"><section class="workspace-card" id="category-details"><small class="workspace-card-kicker">01 · CATEGORY DETAILS</small><h3>General information</h3><label>Category name<input name="categoryName" [(ngModel)]="categoryName" placeholder="e.g. Fresh Produce" required maxlength="80"></label><label>Description<textarea name="categoryDescription" [(ngModel)]="categoryDescription" placeholder="What products belong in this category?" rows="5" maxlength="300"></textarea><span class="field-hint">Keep the description short and helpful for shoppers.</span></label></section><aside class="workspace-aside"><section class="workspace-preview category-preview"><small>CATEGORY PREVIEW</small><div class="preview-icon">▧</div><strong>{{categoryName || 'Your category name'}}</strong><span>{{categoryDescription || 'A short category description appears here.'}}</span></section><section class="workspace-card"><small class="workspace-card-kicker">PUBLISHING</small><h3>Visibility</h3><p>New categories are created as available in the market catalog.</p></section></aside></div><p class="workspace-error" *ngIf="error()">{{error()}}</p><footer class="workspace-actions admin-form-actions"><button type="button" class="cancel-workspace" (click)="closeCategoryCreate()">CANCEL</button><span class="action-spacer"></span><button type="submit" class="publish-workspace" [disabled]="savingCategory() || !categoryName.trim()">{{savingCategory()?'CREATING…':'CREATE CATEGORY'}} <span>↗</span></button></footer></form></section></div></main>`,
  styles: [`
    :host{display:block;background:#f3f4f6;color:#17212b}.admin-page{max-width:1360px;min-height:70vh;margin:auto;padding:34px 28px 54px}.page-head{display:flex;align-items:center;justify-content:space-between;gap:18px;margin-bottom:24px}.eyebrow,.panel-head>div>small,.side-panel>small{font-size:10px;letter-spacing:.15em;color:#79838c;font-weight:700}.page-head h1{font-family:'Retro Blendy','Righteous','Arial Black',sans-serif;font-size:clamp(36px,4vw,52px);letter-spacing:-.06em;margin:7px 0}.page-head h1 span{color:#f18a16}.page-head p{margin:0;color:#73808a;font-size:14px}.refresh{height:42px;padding:0 14px;border:1px solid #d7dde1;background:#fff;display:flex;align-items:center;gap:9px;font-size:10px;font-weight:750;letter-spacing:.1em;cursor:pointer}.refresh:hover{border-color:#f18a16;color:#c76500}.refresh:disabled{opacity:.55;cursor:wait}.spinning{animation:spin 1s linear infinite}@keyframes spin{to{transform:rotate(360deg)}}.metrics{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:13px;margin-bottom:16px}.metric{position:relative;min-height:148px;padding:17px;background:#fff;border:1px solid #e0e5e8;display:flex;flex-direction:column;align-items:flex-start}.metric-icon,.quick-icon{width:37px;height:37px;background:#fff1df;color:#d8750d;display:grid;place-items:center}.metric-icon.green,.quick-icon.green{background:#edf4ed;color:#527558}.metric-icon.blue{background:#eaf1f8;color:#4d6e91}.metric-icon.orange{background:#fff0df;color:#d8750d}.metric>small{margin-top:12px;color:#79838c;font-size:9px;letter-spacing:.13em;font-weight:700}.metric>strong{font-size:28px;line-height:1.1;margin-top:4px;font-variant-numeric:tabular-nums}.metric>span{margin-top:4px;color:#89929a;font-size:10px}.dashboard-grid{display:grid;grid-template-columns:minmax(0,1fr) 290px;gap:15px;align-items:start}.panel{background:white;border:1px solid #e0e5e8}.orders-panel{min-width:0}.panel-head{display:flex;align-items:center;justify-content:space-between;padding:18px 19px;border-bottom:1px solid #e9edef;gap:10px}.panel-head h2,.side-panel h2{margin:5px 0 0;font-size:21px;letter-spacing:-.04em}.panel-head>a{display:flex;align-items:center;gap:5px;color:#65717a;text-decoration:none;font-size:9px;letter-spacing:.1em}.panel-head>a:hover{color:#d8750d}.orders-table-wrap{overflow-x:auto}table{width:100%;border-collapse:collapse;text-align:left}th{padding:12px 14px;background:#fafbfb;color:#859098;font-size:8px;letter-spacing:.12em;font-weight:700;white-space:nowrap}td{padding:13px 14px;border-top:1px solid #edf0f2;color:#66717a;font-size:10px;white-space:nowrap}td:first-child{display:grid;gap:4px}td:first-child strong{color:#202b35;font-size:10px}td:first-child small{font-size:9px;color:#87919a}.money{font-weight:750;color:#19242e}.status{display:inline-flex;padding:6px 8px;background:#edf4ed;color:#557650;font-size:8px;text-transform:uppercase;letter-spacing:.06em}.status.pending{background:#fff3e3;color:#ad610f}.status.cancelled{background:#f8ece9;color:#a45143}.actions{display:flex;gap:5px}.actions button{min-height:29px;padding:0 9px;border:0;background:#ff941c;color:#17212b;font-size:8px;font-weight:750;letter-spacing:.06em;cursor:pointer;transition:background-color .18s,transform .18s}.actions button:hover:not(:disabled){transform:translateY(-1px);background:#ffad42}.actions button:disabled{opacity:.55;cursor:wait}.actions .cancel{width:28px;padding:0;background:#fff;border:1px solid #e0e5e8;font-size:16px}.actions .cancel:hover:not(:disabled){background:#fff0ed;color:#a43b24}.done{color:#87919a;font-size:8px;letter-spacing:.08em}.side-panel{padding:18px}.side-panel h2{margin-bottom:16px}.side-panel>a{display:flex;align-items:center;gap:10px;padding:12px 0;border-top:1px solid #edf0f2;color:inherit;text-decoration:none;transition:transform .18s}.side-panel>a:hover{transform:translateX(3px)}.side-panel>a>span:nth-child(2){flex:1;display:grid;gap:4px}.side-panel>a strong{font-size:11px}.side-panel>a small{font-size:9px;color:#7e8991}.quick-arrow{color:#d8750d}.api-note{display:flex;align-items:flex-start;gap:9px;padding:12px;margin-top:11px;background:#f7f8f8;color:#68806c}.api-note span{display:grid;gap:4px}.api-note strong{font-size:9px;color:#47634b}.api-note small{font-size:9px;color:#76827a;line-height:1.45}.loading-state,.empty-state{min-height:190px;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:10px;color:#78838b;font-size:11px}.loading-state{flex-direction:row}.loading-state i{width:17px;height:17px;border:2px solid #e3e7e9;border-top-color:#f18a16;border-radius:50%;animation:spin .8s linear infinite}.empty-state svg{color:#df7a12}.empty-state strong{font-size:13px;color:#28343d}.empty-state span{font-size:10px}.access-card{min-height:160px;padding:25px;background:#fff;border:1px solid #e1e5e8;display:flex;align-items:center;gap:15px}.access-mark{width:38px;height:38px;display:grid;place-items:center;background:#fff1df;color:#d8750d;font-weight:800}.access-card>div{flex:1}.access-card h2{margin:0 0 5px;font-size:19px}.access-card p{margin:0;color:#71808a;font-size:12px}.access-card>a{display:flex;align-items:center;gap:7px;padding:12px 14px;background:#ff941c;color:#17212b;text-decoration:none;font-size:9px;font-weight:750;letter-spacing:.1em}.denied .access-mark{background:#fff0ed;color:#ae4f3d}.error-banner{display:flex;align-items:center;justify-content:space-between;gap:12px;margin-bottom:14px;padding:12px 14px;border:1px solid #efd0c7;background:#fff7f4;color:#a43b24;font-size:11px}.error-banner button{border:1px solid #e6b9ad;background:#fff;padding:8px 10px;color:#8f3829;font-size:8px;font-weight:700;cursor:pointer}.admin-foot{padding:17px 2px;color:#9099a0;font-size:8px;letter-spacing:.14em}
    @media(max-width:960px){.dashboard-grid{grid-template-columns:1fr}.side-panel{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:0 15px}.side-panel>small,.side-panel h2,.api-note{grid-column:1/-1}.side-panel>a{border-bottom:1px solid #edf0f2}}@media(max-width:700px){.admin-page{padding:23px 13px 40px}.page-head{align-items:flex-start}.page-head h1{font-size:34px}.page-head p{font-size:12px}.refresh{height:37px;padding:0 9px}.metrics{grid-template-columns:repeat(2,minmax(0,1fr));gap:8px}.metric{min-height:130px;padding:13px}.metric>strong{font-size:24px}.panel-head{padding:14px}.panel-head h2{font-size:18px}.panel-head>a{font-size:7px}.side-panel{display:block}.access-card{align-items:flex-start;flex-wrap:wrap;padding:18px}.access-card>div{flex-basis:calc(100% - 55px)}.access-card>a{margin-left:52px}}
    .management-grid{display:grid;grid-template-columns:minmax(0,1.5fr) minmax(290px,1fr);gap:15px;margin-top:15px;align-items:start}.inventory-list{padding:0 16px}.inventory-row{min-height:67px;display:flex;align-items:center;gap:11px;border-bottom:1px solid #edf0f2}.inventory-row:last-child{border:0}.product-mark{width:40px;height:40px;flex:none;display:grid;place-items:center;background:#f2f3f2;color:#db780f}.product-mark img{width:100%;height:100%;object-fit:cover}.inventory-name{flex:1;min-width:0;display:grid;gap:4px}.inventory-name strong{font-size:10px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.inventory-name small{color:#808a92;font-size:9px}.stock-control{display:flex;align-items:center;gap:6px}.stock-control button{width:25px;height:25px;border:1px solid #dfe4e7;background:white;font-size:15px;cursor:pointer}.stock-control button:hover:not(:disabled){border-color:#f18a16;color:#d8750d}.stock-control button:disabled{opacity:.4;cursor:not-allowed}.stock-control b{min-width:24px;text-align:center;font-size:10px}.active-toggle{min-width:61px;padding:6px;border:1px solid #d7e6d7;background:#edf4ed;color:#527558;font-size:7px;font-weight:750;letter-spacing:.05em;cursor:pointer}.active-toggle.inactive{border-color:#e6e0d8;background:#f5f3ef;color:#77716a}.active-toggle:disabled{opacity:.55;cursor:wait}.category-form{padding:15px;display:grid;gap:10px;border-bottom:1px solid #edf0f2}.category-form label{display:grid;gap:5px;color:#78838b;font-size:8px;font-weight:700;letter-spacing:.11em}.category-form input{height:36px;width:100%;padding:0 9px;border:1px solid #dce1e5;font:inherit;font-size:10px;letter-spacing:0}.category-form button{height:36px;border:0;background:#ff941c;color:#17212b;font-size:9px;font-weight:750;letter-spacing:.08em;cursor:pointer}.category-form button:disabled{opacity:.5;cursor:not-allowed}.category-list{max-height:270px;overflow:auto;padding:3px 15px}.category-list>div{min-height:49px;display:flex;align-items:center;gap:9px;border-bottom:1px solid #edf0f2}.category-list>div:last-child{border:0}.category-list .quick-icon{width:30px;height:30px;flex:none}.category-list>div>span:nth-child(2){flex:1;display:grid;gap:3px}.category-list strong{font-size:10px}.category-list small{font-size:8px;color:#869098}.category-list b{font-size:7px;letter-spacing:.07em;color:#527558}.category-list b.archived{color:#9b6658}.empty-small{padding:17px;color:#87919a;font-size:10px}@media(max-width:960px){.management-grid{grid-template-columns:1fr}}@media(max-width:520px){.inventory-list{padding:0 9px}.inventory-row{gap:7px}.product-mark{width:34px;height:34px}.inventory-name small{font-size:8px}.stock-control{gap:3px}.stock-control button{width:22px;height:22px}.active-toggle{min-width:54px;padding:5px}}
    .product-create{padding:12px;display:grid;grid-template-columns:1fr .55fr .45fr;gap:7px;border-bottom:1px solid #edf0f2}.product-create input,.product-create select,.product-category-control{min-width:0;height:31px;padding:0 7px;border:1px solid #dce1e5;background:#fff;font:inherit;font-size:9px}.product-create .description-input{grid-column:1/3}.product-create button{border:0;background:#ff941c;font-size:8px;font-weight:750;letter-spacing:.06em;cursor:pointer}.product-create button:disabled{opacity:.5;cursor:not-allowed}.product-category-control{width:130px;font-size:9px}.row-action{height:25px;padding:0 6px;border:1px solid #dce1e5;background:white;font-size:8px;cursor:pointer}.row-action:hover{border-color:#f18a16;color:#c76a08}.row-action.delete{width:23px;padding:0;color:#a85143;font-size:15px}.category-list .row-action{flex:none}.review-moderation{margin-top:15px}.review-product-select{display:grid;gap:4px;color:#7c868e;font-size:8px;letter-spacing:.1em}.review-product-select select{width:min(300px,45vw);height:34px;padding:0 9px;border:1px solid #dce1e5;background:#fff;font:inherit;font-size:10px;letter-spacing:0}.review-admin-list{padding:0 17px}.review-admin-list article{display:flex;align-items:flex-start;gap:15px;padding:14px 0;border-bottom:1px solid #edf0f2}.review-admin-main{flex:1;min-width:0;display:grid;grid-template-columns:auto auto 1fr;align-items:center;gap:6px 12px}.review-admin-main>strong{font-size:11px}.review-admin-main>small{justify-self:end;color:#828c94;font-size:9px}.review-admin-main .stars{color:#e98716;font-size:12px;letter-spacing:.05em}.review-admin-main .stars i{color:#dce0e3;font-style:normal}.review-admin-main p{grid-column:1/-1;margin:0;color:#68747d;font-size:11px;line-height:1.5}.review-admin-list article>button{border:1px solid #ecd0c9;background:#fff9f7;color:#a34a38;padding:8px 10px;font-size:8px;font-weight:700;letter-spacing:.05em;cursor:pointer}.review-admin-list article>button:disabled{opacity:.5;cursor:wait}@media(max-width:600px){.inventory-row{flex-wrap:wrap;padding:9px 0}.inventory-name{flex-basis:calc(100% - 55px)}.product-category-control{width:120px;margin-left:43px}.review-product-select select{width:42vw}.review-admin-main{grid-template-columns:1fr auto}.review-admin-main>small{grid-column:1/-1;justify-self:start}}
  `]
})
export class AdminDashboardComponent {
  readonly auth = inject(AuthService);
  private readonly admin = inject(AdminService);
  private readonly reviewApi = inject(ReviewService);
  readonly hasAdminAccess = computed(() => this.auth.isAuthenticated() && !!this.auth.user()?.roles.some(role => role.toLowerCase() === 'admin'));
  readonly orders = signal<AdminOrderSummary[]>([]);
  readonly products = signal<AdminProductSummary[]>([]);
  readonly categories = signal<AdminCategorySummary[]>([]);
  readonly categoryOptions = signal<AdminCategorySummary[]>([]);
  readonly allCategories = signal<AdminCategorySummary[]>([]);
  readonly openCategoryMenu = signal('');
  categorySearch = '';
  categoryFilterTerm = '';
  readonly loading = signal(false);
  readonly productsLoading = signal(false);
  readonly error = signal('');
  readonly productCount = signal(0);
  readonly productPage = signal(1);
  readonly productPages = signal(0);
  readonly productPageSize = 8;
  readonly categoryCount = signal(0);
  readonly categoryPage = signal(1);
  readonly categoryPages = signal(0);
  readonly categoryPageSize = 5;
  readonly orderCount = signal(0);
  readonly pendingCount = signal(0);
  readonly productReviews = signal<ProductReview[]>([]);
  readonly reviewLoading = signal(false);
  readonly busyReview = signal('');
  reviewProductId = '';
  readonly busyOrder = signal('');
  readonly selectedOrder = signal<AdminOrderDetail | null>(null);
  readonly orderDetailLoading = signal(false);
  readonly busyProduct = signal('');
  readonly savingCategory = signal(false);
  readonly savingProduct = signal(false);
  readonly productCreateOpen = signal(false);
  readonly categoryCreateOpen = signal(false);
  readonly editOpen = signal(false);
  readonly editType = signal<'product' | 'category'>('product');
  readonly savingEdit = signal(false);
  readonly editError = signal('');
  private editingProduct: AdminProductSummary | null = null;
  private editingCategory: AdminCategorySummary | null = null;
  editName = '';
  editDescription = '';
  editPrice = 0;
  categoryName = '';
  categoryDescription = '';
  newProductName = '';
  newProductDescription = '';
  newProductPrice: number | null = null;
  newProductStock = 0;
  newProductCategoryId = '';
  productSearchTerm = '';
  private readonly productSearchChanges = new Subject<string>();
  private productRequestId = 0;
  readonly icons = { Boxes: LucideBoxes, Tags: LucideTags, Bag: LucideShoppingBag, Activity: LucideActivity, Truck: LucideTruck, Arrow: LucideArrowUpRight, Refresh: LucideRefreshCw };

  constructor() {
    this.productSearchChanges.pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed()).subscribe(term => this.fetchProducts(term));
    if (this.hasAdminAccess()) this.load();
  }

  load(): void {
    if (!this.hasAdminAccess()) return;
    this.loading.set(true);
    this.error.set('');
    forkJoin({
      orders: this.admin.getOrders(1, 6),
      products: this.admin.getProducts(this.productPage(), this.productPageSize, this.productSearchTerm),
      categories: this.admin.getCategories(this.categoryPage(), this.categoryPageSize),
      allCategories: this.admin.getAllCategories(),
      pending: this.admin.getOrders(1, 1, 'Pending')
    }).subscribe({
      next: data => {
        this.productPages.set(Math.ceil(data.products.totalCount / this.productPageSize));
        this.categoryPages.set(Math.ceil(data.categories.totalCount / this.categoryPageSize));
        if (!data.products.items.length && data.products.totalCount > 0 && this.productPage() > 1) {
          this.productPage.set(Math.min(this.productPage() - 1, this.productPages()));
          this.load();
          return;
        }
        if (!data.categories.items.length && data.categories.totalCount > 0 && this.categoryPage() > 1) {
          this.categoryPage.set(Math.min(this.categoryPage() - 1, this.categoryPages()));
          this.load();
          return;
        }
        this.orders.set(data.orders.items || []);
        this.products.set(data.products.items || []);
        this.categories.set(data.categories.items || []);
        this.allCategories.set(data.allCategories);
        this.categoryOptions.set(data.allCategories.filter(category => !category.isDeleted));
        this.productCount.set(data.products.totalCount || 0);
        this.categoryCount.set(data.categories.totalCount || 0);
        this.orderCount.set(data.orders.totalCount || 0);
        this.pendingCount.set(data.pending.totalCount || 0);
        this.loading.set(false);
      },
      error: error => {
        if (error.status === 401) {
          this.auth.logout();
          this.error.set('Your admin session expired. Sign in again with an Admin account.');
        } else if (error.status === 403) {
          this.error.set('The API denied admin access for this account. Sign in with an Admin role.');
        } else {
          this.error.set('The admin API could not load dashboard data. Check the API connection and retry.');
        }
        this.loading.set(false);
      }
    });
  }

  goProducts(page: number): void { if (page < 1 || page > this.productPages() || page === this.productPage()) return; this.productPage.set(page); this.fetchProducts(); }
  goCategories(page: number): void { if (page < 1 || page > this.categoryPages() || page === this.categoryPage()) return; this.categoryPage.set(page); this.load(); }
  onProductSearchChange(term: string): void { this.productSearchTerm = term; this.productPage.set(1); this.productSearchChanges.next(term); }
  searchProducts(): void { this.productSearchChanges.next(this.productSearchTerm); }
  clearProductSearch(): void { this.productSearchTerm = ''; this.productPage.set(1); this.productSearchChanges.next(''); }

  private fetchProducts(term = this.productSearchTerm): void {
    if (!this.hasAdminAccess()) return;
    const requestId = ++this.productRequestId;
    this.productsLoading.set(true);
    this.admin.getProducts(this.productPage(), this.productPageSize, term).subscribe({
      next: result => {
        if (requestId !== this.productRequestId) return;
        const pages = Math.ceil((result.totalCount || 0) / this.productPageSize);
        this.productPages.set(pages);
        if (pages > 0 && this.productPage() > pages) { this.productPage.set(pages); this.fetchProducts(term); return; }
        this.products.set(result.items || []);
        this.productCount.set(result.totalCount || 0);
        this.productsLoading.set(false);
      },
      error: error => {
        if (requestId !== this.productRequestId) return;
        this.productsLoading.set(false);
        if (error.status === 401) { this.auth.logout(); this.error.set('Your admin session expired. Sign in again with an Admin account.'); }
        else if (error.status === 403) this.error.set('The API denied admin access for this account. Sign in with an Admin role.');
        else this.error.set('Product search could not reach the admin API. Check the API connection and retry.');
      }
    });
  }
  visibleCategories(): AdminCategorySummary[] {
    const query = this.categoryFilterTerm.trim().toLocaleLowerCase();
    return query ? this.allCategories().filter(category => category.name.toLocaleLowerCase().includes(query)) : this.categories();
  }

  selectedCategoryName(id: string): string {
    return id ? this.categoryOptions().find(category => category.id === id)?.name || 'Unknown category' : 'No category';
  }

  filteredCategoryOptions(): AdminCategorySummary[] {
    const query = this.categorySearch.trim().toLocaleLowerCase();
    return this.categoryOptions().filter(category => !query || category.name.toLocaleLowerCase().includes(query));
  }

  toggleCategoryMenu(target: string): void {
    this.categorySearch = '';
    this.openCategoryMenu.set(this.openCategoryMenu() === target ? '' : target);
  }

  chooseCategory(target: string, categoryId: string): void {
    this.openCategoryMenu.set('');
    this.categorySearch = '';
    if (target === 'new') { this.newProductCategoryId = categoryId; return; }
    const product = this.products().find(item => item.id === target);
    if (product) this.changeProductCategory(product, categoryId);
  }

  nextAction(order: AdminOrderSummary): { key: 'confirm' | 'ship' | 'deliver'; label: string } | null {
    switch (order.status.toLowerCase()) {
      case 'pending': return { key: 'confirm', label: 'CONFIRM' };
      case 'confirmed': return { key: 'ship', label: 'MARK SHIPPED' };
      case 'shipped': return { key: 'deliver', label: 'MARK DELIVERED' };
      default: return null;
    }
  }

  loadOrderDetail(id: string): void {
    if (this.selectedOrder()?.id === id) { this.closeOrderDetail(); return; }
    this.selectedOrder.set({ id, status: '', createdAt: '', totalAmount: 0 });
    this.orderDetailLoading.set(true);
    this.admin.getOrder(id).subscribe({
      next: order => { if (this.selectedOrder()?.id === id) this.selectedOrder.set(order); this.orderDetailLoading.set(false); },
      error: error => { if (this.selectedOrder()?.id === id) this.selectedOrder.set(null); this.orderDetailLoading.set(false); this.error.set(error.status === 403 ? 'The API denied order detail access.' : 'Order details could not be loaded.'); }
    });
  }

  closeOrderDetail(): void { this.selectedOrder.set(null); this.orderDetailLoading.set(false); }

  updateOrder(order: AdminOrderSummary, action: { key: 'confirm' | 'ship' | 'deliver' | 'cancel'; label: string }): void {
    this.busyOrder.set(order.id);
    this.error.set('');
    this.admin.updateOrder(order.id, action.key).subscribe({
      next: () => { this.busyOrder.set(''); this.load(); },
      error: error => {
        this.busyOrder.set('');
        this.error.set(error.status === 401 ? 'Your admin session expired. Sign in again with an Admin account.' : error.status === 403 ? 'The API denied this admin action.' : 'The order status could not be updated. Please retry.');
        if (error.status === 401) this.auth.logout();
      }
    });
  }

  changeStock(product: AdminProductSummary, delta: number): void {
    const quantity = product.stockQuantity + delta;
    if (quantity < 0) return;
    this.busyProduct.set(product.id);
    this.admin.updateStock(product.id, quantity).subscribe({
      next: () => { this.busyProduct.set(''); this.products.update(items => items.map(item => item.id === product.id ? { ...item, stockQuantity: quantity } : item)); },
      error: error => { this.busyProduct.set(''); this.error.set(error.status === 403 ? 'The API denied this inventory update.' : 'Stock could not be updated. Please retry.'); }
    });
  }

  toggleProduct(product: AdminProductSummary): void {
    this.busyProduct.set(product.id);
    this.admin.setProductActive(product.id, !product.isActive).subscribe({
      next: () => { this.busyProduct.set(''); this.products.update(items => items.map(item => item.id === product.id ? { ...item, isActive: !product.isActive } : item)); },
      error: error => { this.busyProduct.set(''); this.error.set(error.status === 403 ? 'The API denied this availability update.' : 'Product availability could not be updated. Please retry.'); }
    });
  }

  createCategory(): void {
    const name = this.categoryName.trim();
    if (!name || this.savingCategory()) return;
    this.savingCategory.set(true);
    this.admin.createCategory(name, this.categoryDescription.trim()).subscribe({
      next: () => { this.resetCategoryForm(); this.categoryCreateOpen.set(false); this.savingCategory.set(false); this.load(); },
      error: error => { this.savingCategory.set(false); this.error.set(error.status === 403 ? 'The API denied category creation.' : 'Category could not be created. Check the details and retry.'); }
    });
  }

  loadProductReviews(): void {
    if (!this.reviewProductId) { this.productReviews.set([]); return; }
    this.reviewLoading.set(true);
    this.reviewApi.getForProduct(this.reviewProductId, 1, 50).subscribe({
      next: result => { this.productReviews.set(result.reviews.items || []); this.reviewLoading.set(false); },
      error: error => { this.reviewLoading.set(false); this.error.set(error.status === 403 ? 'The API denied review access.' : 'Product reviews could not be loaded.'); }
    });
  }

  removeReview(review: ProductReview): void {
    if (!this.reviewProductId || this.busyReview()) return;
    this.busyReview.set(review.id);
    this.reviewApi.removeAsAdmin(review.id).subscribe({
      next: () => { this.productReviews.update(items => items.filter(item => item.id !== review.id)); this.busyReview.set(''); },
      error: error => { this.busyReview.set(''); this.error.set(error.status === 403 ? 'The API denied review removal.' : 'Review could not be removed. Please retry.'); }
    });
  }

  createProduct(): void {
    const name = this.newProductName.trim();
    if (!name || this.newProductPrice === null || this.savingProduct()) return;
    this.savingProduct.set(true);
    this.admin.createProduct({ name, description: this.newProductDescription.trim(), price: Number(this.newProductPrice), stockQuantity: Number(this.newProductStock), categoryId: this.newProductCategoryId || null }).subscribe({
      next: () => { this.resetProductForm(); this.productCreateOpen.set(false); this.savingProduct.set(false); this.load(); },
      error: error => { this.savingProduct.set(false); this.error.set(error.status === 403 ? 'The API denied product creation.' : 'Product could not be created. Check the details and retry.'); }
    });
  }

  openProductCreate(): void { this.error.set(''); this.openCategoryMenu.set(''); this.productCreateOpen.set(true); }
  closeProductCreate(): void { if (this.savingProduct()) return; this.productCreateOpen.set(false); this.openCategoryMenu.set(''); this.resetProductForm(); }
  private resetProductForm(): void { this.newProductName = ''; this.newProductDescription = ''; this.newProductPrice = null; this.newProductStock = 0; this.newProductCategoryId = ''; this.categorySearch = ''; }
  openCategoryCreate(): void { this.error.set(''); this.categoryCreateOpen.set(true); }
  closeCategoryCreate(): void { if (this.savingCategory()) return; this.categoryCreateOpen.set(false); this.resetCategoryForm(); }
  private resetCategoryForm(): void { this.categoryName = ''; this.categoryDescription = ''; }

  editProduct(product: AdminProductSummary): void {
    this.editType.set('product');
    this.editingProduct = product;
    this.editingCategory = null;
    this.editName = product.name;
    this.editDescription = product.description || '';
    this.editPrice = product.price;
    this.editError.set('');
    this.editOpen.set(true);
  }

  deleteProduct(product: AdminProductSummary): void {
    if (!window.confirm(`Delete “${product.name}”? This removes the product from the catalog.`)) return;
    this.busyProduct.set(product.id);
    this.admin.deleteProduct(product.id).subscribe({
      next: () => { this.busyProduct.set(''); this.load(); },
      error: error => { this.busyProduct.set(''); this.error.set(error.status === 403 ? 'The API denied product deletion.' : 'Product could not be deleted.'); }
    });
  }

  changeProductCategory(product: AdminProductSummary, categoryId: string): void {
    this.busyProduct.set(product.id);
    this.admin.setProductCategory(product.id, categoryId || null).subscribe({
      next: () => { this.busyProduct.set(''); this.load(); },
      error: error => { this.busyProduct.set(''); this.error.set(error.status === 403 ? 'The API denied this category update.' : 'Product category could not be updated.'); }
    });
  }

  editCategory(category: AdminCategorySummary): void {
    this.editType.set('category');
    this.editingCategory = category;
    this.editingProduct = null;
    this.editName = category.name;
    this.editDescription = category.description || '';
    this.editPrice = 0;
    this.editError.set('');
    this.editOpen.set(true);
  }

  closeEdit(): void { if (this.savingEdit()) return; this.editOpen.set(false); this.editError.set(''); }

  saveEdit(): void {
    if (this.savingEdit() || !this.editName.trim()) return;
    this.editError.set('');
    this.savingEdit.set(true);
    const request = this.editType() === 'product' && this.editingProduct
      ? this.admin.updateProduct(this.editingProduct.id, { name: this.editName.trim(), description: this.editDescription.trim(), price: Number(this.editPrice) })
      : this.editingCategory
        ? this.admin.updateCategory(this.editingCategory.id, this.editName.trim(), this.editDescription.trim())
        : null;
    if (!request) { this.savingEdit.set(false); return; }
    request.subscribe({
      next: () => { this.savingEdit.set(false); this.editOpen.set(false); this.load(); },
      error: error => { this.savingEdit.set(false); this.editError.set(error.status === 403 ? 'The API denied this update.' : error.error?.error || error.error?.detail || 'Changes could not be saved. Please retry.'); }
    });
  }

  deleteCategory(category: AdminCategorySummary): void {
    if (!window.confirm(`Delete “${category.name}”?`)) return;
    this.admin.deleteCategory(category.id).subscribe({
      next: () => this.load(),
      error: error => this.error.set(error.status === 403 ? 'The API denied category deletion.' : 'Category could not be deleted.')
    });
  }
}
