import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { StorageService } from './storage.service';
import { environment } from '../../environments/environment';

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
  private baseUrl = `${environment.apiUrl}/auth`; // ✅ USANDO ENVIRONMENT
  private currentTokenSubject: BehaviorSubject<string | null>;
  
  // Usando environment para as chaves
  private readonly TOKEN_KEY = environment.auth.tokenKey;
  private readonly USER_PROFILE_KEY = environment.auth.userProfileKey;
  
  constructor(
    private http: HttpClient,
    private router: Router,
    private storageService: StorageService
  ) {
    // Inicializar com token do storage (se existir e válido)
    const storedToken = this.storageService.getItem(this.TOKEN_KEY);
    const validToken = storedToken && this.isTokenValid(storedToken) ? storedToken : null;
    
    this.currentTokenSubject = new BehaviorSubject<string | null>(validToken);
    
    // Se token inválido, limpar storage
    if (storedToken && !validToken) {
      this.clearAuthData();
    }
  }

  /**
   * Realiza login do usuário
   */
  login(email: string, senha: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${this.baseUrl}/login`, { email, senha })
      .pipe(
        tap((response) => {
          this.handleAuthSuccess(response);
        })
      );
  }

  /**
   * Processa sucesso na autenticação
   */
  private handleAuthSuccess(response: LoginResponse): void {
    // Salvar token
    this.storageService.setItem(this.TOKEN_KEY, response.token);
    this.currentTokenSubject.next(response.token);

    // Decodificar e salvar perfil do usuário
    const payload = this.decodeToken(response.token);
    if (payload) {
      const userProfile = {
        perfil: payload.perfil || 'Cliente',
        email: payload.email,
        userId: payload.sub,
        timestamp: new Date().getTime()
      };
      
      this.storageService.setObject(this.USER_PROFILE_KEY, userProfile);
    }
  }

  /**
   * Realiza logout do usuário
   */
  logout(): void {
    this.clearAuthData();
    this.currentTokenSubject.next(null);
    this.router.navigate(['/login']);
  }

  /**
   * Limpa dados de autenticação
   */
  private clearAuthData(): void {
    this.storageService.removeItem(this.TOKEN_KEY);
    this.storageService.removeItem(this.USER_PROFILE_KEY);
  }

  /**
   * Retorna o token atual
   */
  public get tokenValue(): string | null {
    return this.currentTokenSubject.value;
  }

  /**
   * Observable do token atual
   */
  public get token$(): Observable<string | null> {
    return this.currentTokenSubject.asObservable();
  }

  /**
   * Verifica se usuário está autenticado
   */
  public isAuthenticated(): boolean {
    const token = this.currentTokenSubject.value;
    return token !== null && this.isTokenValid(token);
  }

  /**
   * Verifica se token é válido (não expirou)
   */
  private isTokenValid(token: string): boolean {
    if (!token) return false;

    try {
      const payload = this.decodeToken(token);
      if (!payload || !payload.exp) return false;

      const expirationDate = new Date(payload.exp * 1000);
      const now = new Date();
      
      return expirationDate > now;
    } catch (error) {
      console.warn('Erro ao validar token:', error);
      return false;
    }
  }

  /**
   * Retorna perfil do usuário logado
   */
  public getUserPerfil(): string | null {
    const token = this.currentTokenSubject.value;
    if (!token) return null;

    try {
      const payload = this.decodeToken(token);
      return payload?.perfil || null;
    } catch (error) {
      console.warn('Erro ao obter perfil:', error);
      return null;
    }
  }

  /**
   * Retorna dados completos do usuário
   */
  public getUserProfile(): any {
    return this.storageService.getObject(this.USER_PROFILE_KEY);
  }

  /**
   * Retorna email do usuário logado
   */
  public getUserEmail(): string | null {
    const token = this.currentTokenSubject.value;
    if (!token) return null;

    try {
      const payload = this.decodeToken(token);
      return payload?.email || null;
    } catch (error) {
      console.warn('Erro ao obter email:', error);
      return null;
    }
  }

  /**
   * Retorna ID do usuário logado
   */
  public getUserId(): string | null {
    const token = this.currentTokenSubject.value;
    if (!token) return null;

    try {
      const payload = this.decodeToken(token);
      return payload?.sub || null;
    } catch (error) {
      console.warn('Erro ao obter ID do usuário:', error);
      return null;
    }
  }

  /**
   * Verifica se usuário é admin
   */
  public isAdmin(): boolean {
    return this.getUserPerfil() === 'Admin';
  }

  /**
   * Decodifica payload do JWT token
   */
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
    } catch (error) {
      console.warn('Erro ao decodificar token:', error);
      return null;
    }
  }

  /**
   * Verifica se token expira em breve (próximos 5 minutos)
   */
  public isTokenExpiringSoon(): boolean {
    const token = this.currentTokenSubject.value;
    if (!token) return false;

    try {
      const payload = this.decodeToken(token);
      if (!payload || !payload.exp) return false;

      const expirationDate = new Date(payload.exp * 1000);
      const now = new Date();
      const fiveMinutesFromNow = new Date(now.getTime() + 5 * 60 * 1000);
      
      return expirationDate <= fiveMinutesFromNow;
    } catch (error) {
      return false;
    }
  }

  /**
   * Força verificação de token (para usar em guards)
   */
  public validateCurrentToken(): boolean {
    const token = this.currentTokenSubject.value;
    
    if (!token || !this.isTokenValid(token)) {
      this.logout();
      return false;
    }
    
    return true;
  }

  /**
   * Refresh token (se implementado no backend)
   */
  public refreshToken(): Observable<LoginResponse> {
    // Implementar quando backend suportar refresh token
    throw new Error('Refresh token não implementado');
  }

  /**
   * Registra novo usuário
   */
  public register(userData: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/register`, userData);
  }

  /**
   * Solicita reset de senha
   */
  public forgotPassword(email: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/forgot-password`, { email });
  }

  /**
   * Reseta senha com token
   */
  public resetPassword(token: string, newPassword: string): Observable<any> {
    return this.http.post(`${this.baseUrl}/reset-password`, { 
      token, 
      newPassword 
    });
  }
}