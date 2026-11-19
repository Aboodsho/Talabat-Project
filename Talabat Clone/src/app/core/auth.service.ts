import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

export interface LoginResponse {
  accessToken: string;
  expireAtUtc: string;
  user: { id?: string; email?: string; fullName?: string; fullname?: string; FullName?: string; Fullname?: string; name?: string; roles?: string[] };
  fullName?: string;
  fullname?: string;
  FullName?: string;
  Fullname?: string;
}

export interface AuthUser { id: string; email: string; fullName: string; roles: string[]; }

export interface RegisteredUser {
  id: string;
  email: string;
  fullname: string;
  roles: string[];
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly storageKey = 'near-market-access-token';
  private readonly userStorageKey = 'near-market-user';
  readonly token = signal<string | null>(this.readToken());
  readonly isAuthenticated = signal(!!this.token());
  readonly user = signal<AuthUser | null>(this.readUser());

  constructor(private readonly http: HttpClient) {}

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/auth/login', { email, password }).pipe(
      tap(response => {
        this.token.set(response.accessToken);
        this.isAuthenticated.set(true);
        const user = this.normalizeUser(response.user, response);
        this.user.set(user);
        try {
          localStorage.setItem(this.storageKey, response.accessToken);
          localStorage.setItem(this.userStorageKey, JSON.stringify(user));
        } catch { /* storage may be unavailable */ }
      })
    );
  }

  register(fullName: string, email: string, password: string): Observable<RegisteredUser> {
    return this.http.post<RegisteredUser>('/api/auth/register', { fullName, email, password });
  }

  requestPasswordReset(email: string): Observable<void> {
    return this.http.post<void>('/api/auth/forgot-password', { email });
  }

  resetPassword(userId: string, token: string, newPassword: string): Observable<void> {
    return this.http.post<void>('/api/auth/reset-password', { userId, token, newPassword });
  }

  confirmEmail(userId: string, token: string): Observable<void> {
    return this.http.post<void>('/api/auth/confirm-email', { userId, token });
  }

  logout(): void {
    localStorage.removeItem(this.storageKey);
    localStorage.removeItem(this.userStorageKey);
    this.token.set(null);
    this.isAuthenticated.set(false);
    this.user.set(null);
  }

  private readToken(): string | null {
    try { return localStorage.getItem(this.storageKey); } catch { return null; }
  }

  private readUser(): AuthUser | null {
    try {
      const stored = JSON.parse(localStorage.getItem(this.userStorageKey) || 'null') as (Partial<AuthUser> & { fullname?: string; FullName?: string; Fullname?: string; name?: string }) | null;
      if (!stored || typeof stored.email !== 'string') return null;
      const fullName = stored.fullName || stored.fullname || stored.FullName || stored.Fullname || stored.name;
      return typeof fullName === 'string' && fullName.trim() ? { id: stored.id || '', email: stored.email, fullName, roles: stored.roles || [] } : null;
    } catch { return null; }
  }

  private normalizeUser(user: LoginResponse['user'] | undefined, response: LoginResponse): AuthUser {
    const fullName = user?.fullName || user?.fullname || user?.FullName || user?.Fullname || user?.name || response.fullName || response.fullname || response.FullName || response.Fullname || '';
    return { id: user?.id || '', email: user?.email || '', fullName, roles: user?.roles || [] };
  }
}
