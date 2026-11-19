import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ProductReview {
  id: string;
  rating: number;
  comment: string | null;
  createdAt: string;
  authorName: string;
}

export interface ProductReviewResult {
  averageRating: number;
  reviews: { items: ProductReview[]; pageNumber: number; pageSize: number; totalCount: number; totalPages: number };
}

@Injectable({ providedIn: 'root' })
export class ReviewService {
  private readonly http = inject(HttpClient);

  getForProduct(productId: string, pageNumber = 1, pageSize = 10): Observable<ProductReviewResult> {
    const params = new HttpParams().set('PageNumber', pageNumber).set('PageSize', pageSize);
    return this.http.get<ProductReviewResult>(`/api/products/${productId}/reviews`, { params });
  }

  add(productId: string, rating: number, comment: string): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`/api/products/${productId}/reviews`, { rating, comment });
  }

  update(productId: string, rating: number, comment: string): Observable<void> {
    return this.http.put<void>(`/api/products/${productId}/reviews`, { rating, comment });
  }

  removeOwn(productId: string): Observable<void> {
    return this.http.delete<void>(`/api/products/${productId}/reviews`);
  }

  removeAsAdmin(reviewId: string): Observable<void> {
    return this.http.delete<void>(`/api/admin/reviews/${reviewId}`);
  }
}
