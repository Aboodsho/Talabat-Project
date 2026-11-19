import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { EMPTY, Observable, expand, reduce } from 'rxjs';

export interface AdminPagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface AdminOrderSummary {
  id: string;
  status: string;
  createdAt: string;
  itemsCount: number;
  totalAmount: number;
}

export interface AdminOrderDetail {
  id: string;
  status: string;
  createdAt: string;
  totalAmount: number;
  shippingAddress?: { street: string; city: string; state?: string | null; postalCode?: string | null; country: string };
  items?: Array<{ productId: string; prodcutName?: string; productName?: string; quantity: number; unitPrice: number; lineTotal: number }>;
}

export interface AdminProductSummary {
  id: string;
  name: string;
  price: number;
  stockQuantity: number;
  isActive: boolean;
  categoryName: string | null;
  categoryId?: string | null;
  description?: string | null;
  images: string[];
}

export interface AdminCategorySummary {
  id: string;
  name: string;
  description: string | null;
  isDeleted: boolean;
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);

  getOrders(pageNumber = 1, pageSize = 6, status?: string): Observable<AdminPagedResult<AdminOrderSummary>> {
    let params = new HttpParams().set('PageNumber', pageNumber).set('PageSize', pageSize);
    if (status) params = params.set('Status', status);
    return this.http.get<AdminPagedResult<AdminOrderSummary>>('/api/admin/orders', { params });
  }

  getOrder(id: string): Observable<AdminOrderDetail> {
    return this.http.get<AdminOrderDetail>(`/api/admin/orders/${id}`);
  }

  getProducts(pageNumber = 1, pageSize = 1, searchTerm?: string): Observable<AdminPagedResult<AdminProductSummary>> {
    let params = new HttpParams().set('PageNumber', pageNumber).set('PageSize', pageSize);
    if (searchTerm?.trim()) params = params.set('SearchTerm', searchTerm.trim());
    return this.http.get<AdminPagedResult<AdminProductSummary>>('/api/admin/products', { params });
  }

  getProduct(id: string): Observable<AdminProductSummary> {
    return this.http.get<AdminProductSummary>(`/api/admin/products/${id}`);
  }

  createProduct(product: { name: string; description: string; price: number; stockQuantity: number; categoryId: string | null }): Observable<string> {
    return this.http.post<string>('/api/admin/products', product);
  }

  updateProduct(id: string, product: { name: string; description: string; price: number }): Observable<void> {
    return this.http.put<void>(`/api/admin/products/${id}`, product);
  }

  setProductCategory(id: string, categoryId: string | null): Observable<void> {
    return this.http.patch<void>(`/api/admin/products/${id}/category`, categoryId);
  }

  deleteProduct(id: string): Observable<void> {
    return this.http.delete<void>(`/api/admin/products/${id}`);
  }

  getCategories(pageNumber = 1, pageSize = 1): Observable<AdminPagedResult<AdminCategorySummary>> {
    const params = new HttpParams().set('PageNumber', pageNumber).set('PageSize', pageSize);
    return this.http.get<AdminPagedResult<AdminCategorySummary>>('/api/admin/categories', { params });
  }

  getAllCategories(pageSize = 50): Observable<AdminCategorySummary[]> {
    return this.getCategories(1, pageSize).pipe(
      expand((page, index) => {
        const nextPage = index + 2;
        const pageCount = Math.ceil(page.totalCount / pageSize);
        return nextPage <= pageCount ? this.getCategories(nextPage, pageSize) : EMPTY;
      }),
      reduce((all, page) => all.concat(page.items), [] as AdminCategorySummary[])
    );
  }

  createCategory(name: string, description: string): Observable<string> {
    return this.http.post<string>('/api/admin/categories', { name, description });
  }

  updateCategory(id: string, name: string, description: string): Observable<void> {
    return this.http.put<void>(`/api/admin/categories/${id}`, { name, description });
  }

  deleteCategory(id: string): Observable<void> {
    return this.http.delete<void>(`/api/admin/categories/${id}`);
  }

  updateStock(productId: string, quantity: number): Observable<void> {
    return this.http.patch<void>(`/api/admin/products/${productId}/stock`, quantity);
  }

  setProductActive(productId: string, isActive: boolean): Observable<void> {
    return this.http.patch<void>(`/api/admin/products/${productId}/status`, isActive);
  }

  updateOrder(id: string, action: 'confirm' | 'ship' | 'deliver' | 'cancel'): Observable<void> {
    return this.http.post<void>(`/api/admin/orders/${id}/${action}`, {});
  }
}
