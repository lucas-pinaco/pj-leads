import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

interface LoginResponse {
  token: string;
  expiracao: string;
  perfil?: string;
}

interface TokenPayload {
  sub: string;
  email: string;
  perfil?: string;
  exp: number;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private baseUrl = 'https://localhost:7167/api/auth';
  private currentTokenSubject: BehaviorSubject<string | null>;
  private _isBrowser: boolean;

  constructor(
    private http: HttpClient,
    private router: Router,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {
    // Verifica se estamos realmente no navegador
    this._isBrowser = isPlatformBrowser(this.platformId);

    // Se estivermos no browser, lemos do localStorage; caso contrário, null
    const tokenInStorage = this._isBrowser
      ? localStorage.getItem('token')
      : null;

    this.currentTokenSubject = new BehaviorSubject<string | null>(tokenInStorage);
  }

  private readToken(): string | null {
    if (!this._isBrowser) {
      return null;
    }
    return localStorage.getItem('token');
  }

  private writeToken(token: string): void {
    if (!this._isBrowser) {
      return;
    }
    localStorage.setItem('token', token);
  }

  private removeToken(): void {
    if (!this._isBrowser) {
      return;
    }
    localStorage.removeItem('token');
    localStorage.removeItem('userProfile'); // Limpar cache do perfil
  }

  login(email: string, senha: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${this.baseUrl}/login`, { email, senha })
      .pipe(
        tap((resp) => {
          this.writeToken(resp.token);
          this.currentTokenSubject.next(resp.token);

          // Decodificar token para pegar informações do usuário
          if (resp.token) {
            const payload = this.decodeToken(resp.token);
            if (payload?.perfil) {
              localStorage.setItem('userProfile', JSON.stringify({
                perfil: payload.perfil,
                email: payload.email,
                timestamp: new Date().getTime()
              }));
            }
          }
        })
      );
  }

  logout(): void {
    this.removeToken();
    this.currentTokenSubject.next(null);
    this.router.navigate(['/login']);
  }

  public get tokenValue(): string | null {
    return this.currentTokenSubject.value;
  }

  public isAuthenticated(): boolean {
    const token = this.currentTokenSubject.value;
    if (!token) return false;

    // Verificar se o token não expirou
    try {
      const payload = this.decodeToken(token);
      if (payload && payload.exp) {
        const expirationDate = new Date(payload.exp * 1000);
        return expirationDate > new Date();
      }
    } catch (e) {
      console.error('Erro ao decodificar token:', e);
    }

    return false;
  }

  public getUserPerfil(): string | null {
    const token = this.currentTokenSubject.value;
    if (!token) return null;

    try {
      const payload = this.decodeToken(token);
      return payload?.perfil || null;
    } catch (e) {
      console.error('Erro ao obter perfil:', e);
      return null;
    }
  }

  private decodeToken(token: string): TokenPayload | null {
    try {
      const base64Url = token.split('.')[1];
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      const jsonPayload = decodeURIComponent(
        atob(base64)
          .split('')
          .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      );

      return JSON.parse(jsonPayload);
    } catch (e) {
      console.error('Erro ao decodificar token:', e);
      return null;
    }
  }
}