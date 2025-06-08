import { Routes } from '@angular/router';
import { LoginComponent } from './auth/login/login.component';
import { AuthGuard } from './auth/auth.guard';
import { AdminGuard } from './auth/admin.guard';
import { LayoutComponent } from './shared/layout/layout.component';
import { HomeComponent } from './pages/home/home.component';
import { UploadComponent } from './admin/upload/upload.component';
import { ClientesComponent } from './admin/clientes/clientes.component';
import { PlanosComponent } from './admin/planos/planos.component';
import { HistoricoComponent } from './pages/historico/historico.component';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginComponent
  },
  {
    path: '',
    component: LayoutComponent,
    canActivate: [AuthGuard],
    children: [
      {
        path: '',
        redirectTo: 'exportar',
        pathMatch: 'full'
      },
      {
        path: 'exportar',
        component: HomeComponent
      },
      {
        path: 'historico',
        component: HistoricoComponent
      },
      {
        path: 'admin/importar',
        component: UploadComponent,
        canActivate: [AdminGuard]
      },
      {
        path: 'admin/arquivos',
        component: UploadComponent,
        canActivate: [AdminGuard]
      },
      {
        path: 'admin/clientes',
        component: ClientesComponent,
        canActivate: [AdminGuard]
      },
      {
        path: 'admin/planos',
        component: PlanosComponent,
        canActivate: [AdminGuard]
      },
      {
        path: 'minha-conta',
        // component: MinhaContaComponent // A ser criado
        component: HomeComponent // Temporário
      }
    ]
  },
  {
    path: '**',
    redirectTo: ''
  }
];