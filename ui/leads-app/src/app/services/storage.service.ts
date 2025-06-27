import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

@Injectable({
  providedIn: 'root'
})
export class StorageService {
  private isBrowser: boolean;

  constructor(@Inject(PLATFORM_ID) private platformId: Object) {
    this.isBrowser = isPlatformBrowser(this.platformId);
  }

  /**
   * Salva um item no localStorage
   */
  setItem(key: string, value: string): void {
    if (!this.isBrowser) {
      return;
    }

    try {
      localStorage.setItem(key, value);
    } catch (error) {
      console.warn(`Erro ao salvar no localStorage (${key}):`, error);
    }
  }

  /**
   * Recupera um item do localStorage
   */
  getItem(key: string): string | null {
    if (!this.isBrowser) {
      return null;
    }

    try {
      return localStorage.getItem(key);
    } catch (error) {
      console.warn(`Erro ao recuperar do localStorage (${key}):`, error);
      return null;
    }
  }

  /**
   * Remove um item do localStorage
   */
  removeItem(key: string): void {
    if (!this.isBrowser) {
      return;
    }

    try {
      localStorage.removeItem(key);
    } catch (error) {
      console.warn(`Erro ao remover do localStorage (${key}):`, error);
    }
  }

  /**
   * Limpa todo o localStorage
   */
  clear(): void {
    if (!this.isBrowser) {
      return;
    }

    try {
      localStorage.clear();
    } catch (error) {
      console.warn('Erro ao limpar localStorage:', error);
    }
  }

  /**
   * Verifica se localStorage está disponível
   */
  isAvailable(): boolean {
    return this.isBrowser;
  }

  /**
   * Salva um objeto como JSON
   */
  setObject(key: string, value: any): void {
    if (!this.isBrowser) {
      return;
    }

    try {
      const serializedValue = JSON.stringify(value);
      this.setItem(key, serializedValue);
    } catch (error) {
      console.warn(`Erro ao salvar objeto no localStorage (${key}):`, error);
    }
  }

  /**
   * Recupera um objeto do JSON
   */
  getObject<T>(key: string): T | null {
    if (!this.isBrowser) {
      return null;
    }

    try {
      const serializedValue = this.getItem(key);
      if (serializedValue === null) {
        return null;
      }
      return JSON.parse(serializedValue) as T;
    } catch (error) {
      console.warn(`Erro ao recuperar objeto do localStorage (${key}):`, error);
      return null;
    }
  }

  /**
   * Verifica se uma chave existe no localStorage
   */
  hasItem(key: string): boolean {
    return this.getItem(key) !== null;
  }

  /**
   * Retorna todas as chaves do localStorage
   */
  getAllKeys(): string[] {
    if (!this.isBrowser) {
      return [];
    }

    try {
      const keys: string[] = [];
      for (let i = 0; i < localStorage.length; i++) {
        const key = localStorage.key(i);
        if (key) {
          keys.push(key);
        }
      }
      return keys;
    } catch (error) {
      console.warn('Erro ao obter chaves do localStorage:', error);
      return [];
    }
  }

  /**
   * Salva dados com expiração
   */
  setItemWithExpiry(key: string, value: string, expiryInMinutes: number): void {
    if (!this.isBrowser) {
      return;
    }

    const now = new Date();
    const item = {
      value: value,
      expiry: now.getTime() + (expiryInMinutes * 60 * 1000)
    };

    this.setObject(key, item);
  }

  /**
   * Recupera dados verificando expiração
   */
  getItemWithExpiry(key: string): string | null {
    if (!this.isBrowser) {
      return null;
    }

    const item = this.getObject<{value: string, expiry: number}>(key);
    
    if (!item) {
      return null;
    }

    const now = new Date();
    
    // Se expirou, remove o item
    if (now.getTime() > item.expiry) {
      this.removeItem(key);
      return null;
    }

    return item.value;
  }

  /**
   * Session Storage methods
   */
  
  setSessionItem(key: string, value: string): void {
    if (!this.isBrowser) {
      return;
    }

    try {
      sessionStorage.setItem(key, value);
    } catch (error) {
      console.warn(`Erro ao salvar no sessionStorage (${key}):`, error);
    }
  }

  getSessionItem(key: string): string | null {
    if (!this.isBrowser) {
      return null;
    }

    try {
      return sessionStorage.getItem(key);
    } catch (error) {
      console.warn(`Erro ao recuperar do sessionStorage (${key}):`, error);
      return null;
    }
  }

  removeSessionItem(key: string): void {
    if (!this.isBrowser) {
      return;
    }

    try {
      sessionStorage.removeItem(key);
    } catch (error) {
      console.warn(`Erro ao remover do sessionStorage (${key}):`, error);
    }
  }

  clearSession(): void {
    if (!this.isBrowser) {
      return;
    }

    try {
      sessionStorage.clear();
    } catch (error) {
      console.warn('Erro ao limpar sessionStorage:', error);
    }
  }
}