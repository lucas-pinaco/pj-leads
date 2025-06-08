import { Injectable } from '@angular/core';
import {
  CanActivate,
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
  Router
} from '@angular/router';
import { Observable, map, catchError, of } from 'rxjs';
import { AuthService } from './auth.service';
import { ClienteService } from '../services/cliente.service';

@Injectable({
  providedIn: 'root'
})
export class AdminGuard implements CanActivate {
  constructor(
    private authService: AuthService,
    private clienteService: ClienteService,
    private router: Router
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean> | boolean {
    // Primeiro verifica se está autenticado
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login']);
      return false;
    }

    // Verifica se tem o perfil no localStorage (cache)
    const perfilCache = localStorage.getItem('userProfile');
    if (perfilCache) {
      try {
        const profile = JSON.parse(perfilCache);
        if (profile.perfil === 'Admin') {
          return true;
        }
      } catch (e) {
        console.error('Erro ao ler cache do perfil:', e);
      }
    }

    // Se não tem cache, busca do servidor
    return this.clienteService.obterMeuPerfil().pipe(
      map(perfil => {
        // Salva no cache para próximas verificações
        localStorage.setItem('userProfile', JSON.stringify({
          perfil: this.determinarPerfil(perfil),
          timestamp: new Date().getTime()
        }));

        const isAdmin = this.determinarPerfil(perfil) === 'Admin';
        
        if (!isAdmin) {
          this.router.navigate(['/']);
          return false;
        }
        
        return true;
      }),
      catchError(err => {
        console.error('Erro ao verificar perfil:', err);
        this.router.navigate(['/']);
        return of(false);
      })
    );
  }

  private determinarPerfil(perfil: any): string {
    // Lógica para determinar se é admin
    // Pode ser baseado em uma propriedade específica do backend
    // Por exemplo, se o usuário tem um campo 'perfil' ou 'role'
    
    // Temporariamente, vamos considerar admin se o email contém 'admin'
    // Em produção, isso deve vir do backend
    if (perfil.cliente?.email?.includes('admin')) {
      return 'Admin';
    }
    
    return 'Cliente';
  }
}