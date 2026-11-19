import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ProductDto {
  id: string;
  name: string;
  description: string | null;
  price: number;
  categoryName: string | null;
  images: string[];
  isAvailable: boolean;
  stockQuantity?: number;
}

export interface CategoryDto {
  id: string;
  name: string;
  description: string | null;
}

export interface PagedResult<T> {
  items: T[];
  pageSize: number;
  pageNumber: number;
  totalCount: number;
  totalPages: number;
}

@Injectable({ providedIn: 'root' })
export class CatalogService {
  constructor(private readonly http: HttpClient) {}

  getProducts(pageSize = 12, searchTerm?: string, categoryId?: string, pageNumber = 1, minPrice?: number, maxPrice?: number): Observable<PagedResult<ProductDto>> {
    let params = new HttpParams().set('PageNumber', pageNumber).set('PageSize', pageSize);
    if (searchTerm?.trim()) params = params.set('SearchTerm', searchTerm.trim());
    if (categoryId) params = params.set('CategoryId', categoryId);
    if (minPrice !== undefined) params = params.set('MinPrice', minPrice);
    if (maxPrice !== undefined) params = params.set('MaxPrice', maxPrice);
    return this.http.get<PagedResult<ProductDto>>('/api/products', { params });
  }

  getProduct(id: string): Observable<ProductDto> { return this.http.get<ProductDto>(`/api/products/${id}`); }

  getCategories(pageSize = 50): Observable<PagedResult<CategoryDto>> {
    const params = new HttpParams().set('PageNumber', 1).set('PageSize', pageSize);
    return this.http.get<PagedResult<CategoryDto>>('/api/categories', { params });
  }
}
