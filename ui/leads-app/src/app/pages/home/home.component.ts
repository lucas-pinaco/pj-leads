import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { ClienteService, MeuPerfil } from '../../services/cliente.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    RouterLink
  ],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit {
  isAdmin = false;
  carregando = false;
  meuPerfil: MeuPerfil | null = null;

  constructor(
    private authService: AuthService,
    private clienteService: ClienteService
  ) {}

  ngOnInit(): void {
    this.isAdmin = this.authService.getUserPerfil() === 'Admin';

    if (!this.isAdmin) {
      this.carregando = true;
      this.clienteService.obterMeuPerfil().subscribe({
        next: perfil => {
          this.meuPerfil = perfil;
          this.carregando = false;
        },
        error: err => {
          console.error('Erro ao carregar perfil:', err);
          this.carregando = false;
        }
      });
    }
  }
}
