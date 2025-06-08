import { Component } from '@angular/core';
import { Router, RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../auth/auth.service';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';

interface MenuItem {
  titulo: string;
  icone: string;
  rota: string;
  perfil?: string;
}

@Component({
  selector: 'app-layout',
  templateUrl: './layout.component.html',
  styleUrls: ['./layout.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    MatDividerModule
  ]
})
export class LayoutComponent {
  menuItems: MenuItem[] = [
    {
      titulo: 'Exportar Leads',
      icone: 'download',
      rota: '/exportar'
    },
    {
      titulo: 'Importar Arquivos',
      icone: 'upload',
      rota: '/admin/importar',
      perfil: 'Admin'
    },
    {
      titulo: 'Gerenciar Arquivos',
      icone: 'folder',
      rota: '/admin/arquivos',
      perfil: 'Admin'
    },
    {
      titulo: 'Clientes',
      icone: 'people',
      rota: '/admin/clientes',
      perfil: 'Admin'
    },
    {
      titulo: 'Planos',
      icone: 'card_membership',
      rota: '/admin/planos',
      perfil: 'Admin'
    },
    {
      titulo: 'Minha Conta',
      icone: 'account_circle',
      rota: '/minha-conta'
    },
    {
      titulo: 'Histórico',
      icone: 'history',
      rota: '/historico'
    }
  ];

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  get usuarioLogado(): string {
    // Implementar lógica para pegar nome do usuário
    return 'Usuário';
  }

  get isAdmin(): boolean {
    // Implementar lógica para verificar se é admin
    // Por enquanto, retorna true para testes
    return true;
  }

  menuFiltrado(): MenuItem[] {
    return this.menuItems.filter(item => {
      if (item.perfil === 'Admin' && !this.isAdmin) {
        return false;
      }
      return true;
    });
  }

  logout(): void {
    this.authService.logout();
  }
}