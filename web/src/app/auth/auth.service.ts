import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import {
  DecodedUser,
  LoginRequest,
  RegisterRequest,
  RegisterResult,
  TokenResult,
} from './models/auth.models';

const STORAGE_KEY = 'securegate.session';

interface StoredSession {
  token: string;
  expiresAt: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  constructor(private readonly http: HttpClient) {}

  register(request: RegisterRequest): Observable<RegisterResult> {
    return this.http.post<RegisterResult>('/api/auth/register', request);
  }

  login(request: LoginRequest): Observable<TokenResult> {
    return this.http
      .post<TokenResult>('/api/auth/login', request)
      .pipe(tap((result) => this.storeSession(result)));
  }

  logout(): void {
    localStorage.removeItem(STORAGE_KEY);
  }

  isAuthenticated(): boolean {
    const session = this.readSession();
    if (!session) {
      return false;
    }
    return new Date(session.expiresAt).getTime() > Date.now();
  }

  getDecodedUser(): DecodedUser | null {
    const session = this.readSession();
    if (!session) {
      return null;
    }
    return this.decodeToken(session.token);
  }

  private storeSession(result: TokenResult): void {
    const session: StoredSession = { token: result.token, expiresAt: result.expiresAt };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
  }

  private readSession(): StoredSession | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return null;
    }
    try {
      return JSON.parse(raw) as StoredSession;
    } catch {
      return null;
    }
  }

  private decodeToken(token: string): DecodedUser | null {
    const payload = token.split('.')[1];
    if (!payload) {
      return null;
    }
    try {
      const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
      const json = atob(normalized);
      return JSON.parse(json) as DecodedUser;
    } catch {
      return null;
    }
  }
}
