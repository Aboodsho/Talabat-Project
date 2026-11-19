import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LucideDynamicIcon, LucideArrowLeft, LucideArrowUpRight, LucideHeart, LucideMinus, LucidePlus, LucideShoppingBag } from '@lucide/angular';
import { CatalogService, ProductDto } from '../../core/catalog.service';
import { CartService } from '../../core/cart.service';
import { WishlistService } from '../../core/wishlist.service';
import { ToastService } from '../../core/toast.service';
import { AuthService } from '../../core/auth.service';
import { ProductReview, ReviewService } from '../../core/review.service';

@Component({selector:'app-product-detail',standalone:true,imports:[CommonModule,FormsModule,RouterLink,LucideDynamicIcon],templateUrl: './product-detail.component.html', styleUrl: './product-detail.component.css' })
export class ProductDetailComponent {
 private readonly catalog=inject(CatalogService); private readonly route=inject(ActivatedRoute); readonly wishlist=inject(WishlistService); readonly cart=inject(CartService); private readonly toast=inject(ToastService); readonly auth=inject(AuthService); private readonly reviewApi=inject(ReviewService);
 readonly product=signal<ProductDto|null>(null); readonly loading=signal(true); readonly error=signal(''); readonly quantity=signal(1); readonly activeImage=signal('');
 readonly reviews=signal<ProductReview[]>([]); readonly reviewAverage=signal(0); readonly reviewCount=signal(0); readonly reviewPage=signal(1); readonly reviewPages=signal(0); readonly reviewsLoading=signal(false); readonly reviewBusy=signal(false); readonly reviewError=signal(''); readonly reviewMessage=signal(''); readonly selectedRating=signal(5); reviewComment=''; readonly reviewPageSize=10;
 readonly icons={ArrowLeft:LucideArrowLeft,ArrowUpRight:LucideArrowUpRight,Heart:LucideHeart,Minus:LucideMinus,Plus:LucidePlus,ShoppingBag:LucideShoppingBag};
 readonly images=()=>this.product()?.images?.length?this.product()!.images:[this.fallback]; private get fallback(){return 'https://images.unsplash.com/photo-1542838132-92c53300491e?auto=format&fit=crop&w=900&q=80'}
 constructor(){this.route.paramMap.subscribe(params=>{const id=params.get('id');if(!id)return;this.loading.set(true);this.catalog.getProduct(id).subscribe({next:p=>{this.product.set(p);this.activeImage.set(p.images?.[0]||this.fallback);this.loading.set(false);this.loadReviews(1)},error:()=>{this.error.set('We could not load this product from the catalog.');this.loading.set(false)}})})}
  add(p:ProductDto){this.cart.add(p,this.quantity());this.toast.show(`${p.name} added to your basket`)}
  decrease(){this.quantity.update(value=>Math.max(1,value-1))}
  increase(){this.quantity.update(value=>Math.min(this.cart.remainingFor(this.product()?.id || ""),value+1))}
 save(p:ProductDto){this.wishlist.toggle(p);this.toast.show(this.wishlist.has(p.id)?'Added to wishlist':'Removed from wishlist')}
 loadReviews(page:number){const product=this.product();if(!product||page<1)return;this.reviewsLoading.set(true);this.reviewError.set('');this.reviewApi.getForProduct(product.id,page,this.reviewPageSize).subscribe({next:result=>{const pages=Math.ceil(result.reviews.totalCount/this.reviewPageSize);if(!result.reviews.items.length&&result.reviews.totalCount>0&&page>1){this.reviewPages.set(pages);this.loadReviews(Math.min(page-1,pages));return}this.reviews.set(result.reviews.items||[]);this.reviewAverage.set(result.averageRating||0);this.reviewCount.set(result.reviews.totalCount||0);this.reviewPages.set(pages);this.reviewPage.set(Math.max(1,Math.min(page,Math.max(pages,1))));this.reviewsLoading.set(false)},error:()=>{this.reviewError.set('Reviews could not be loaded. Please retry.');this.reviewsLoading.set(false)}})}
 submitReview(){const product=this.product();if(!product||this.reviewBusy())return;this.reviewError.set('');this.reviewMessage.set('');this.reviewBusy.set(true);this.reviewApi.add(product.id,this.selectedRating(),this.reviewComment.trim()).subscribe({next:()=>{this.reviewComment='';this.selectedRating.set(5);this.reviewBusy.set(false);this.reviewMessage.set('Thanks for sharing your review.');this.loadReviews(1)},error:error=>{this.reviewBusy.set(false);if(error.status===401){this.auth.logout();this.reviewError.set('Your session expired. Sign in again to submit a review.')}else this.reviewError.set(error.error?.error||error.error?.detail||'Your review could not be submitted. You may already have reviewed this product.')}})}
}
