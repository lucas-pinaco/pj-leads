import { Routes } from '@angular/router';
import { LoginComponent } from './pages/login/login.component';
import { AuthGuard } from './auth/auth.guard';
import { AdminGuard } from './auth/admin.guard';
import { LayoutComponent } from './shared/layout/layout.component';
import { HomeComponent } from './pages/home/home.component';
import { ExportarComponent } from './pages/exportar/exportar.component';
import { UploadComponent } from './pages/upload/upload.component';
import { ClientesComponent } from './pages/clientes/clientes.component';
import { PlanosComponent } from './pages/planos/planos.component';
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
        redirectTo: 'home',
        pathMatch: 'full'
      },
      {
        path: 'home',
        component: HomeComponent
      },
      {
        path: 'exportar',
        component: ExportarComponent
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