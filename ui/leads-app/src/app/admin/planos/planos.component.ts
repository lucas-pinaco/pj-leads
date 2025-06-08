import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { ClienteService, Plano } from '../../services/cliente.service';

@Component({
  selector: 'app-planos',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatDividerModule
  ],
  templateUrl: './planos.component.html',
  styleUrls: ['./planos.component.scss']
})
export class PlanosComponent implements OnInit {
  planos: Plano[] = [];
  carregando = true;

  constructor(private clienteService: ClienteService) {}

  ngOnInit(): void {
    this.carregarPlanos();
  }

  carregarPlanos(): void {
    this.clienteService.listarPlanos().subscribe({
      next: (planos) => {
        this.planos = planos;
        this.carregando = false;
      },
      error: (err) => {
        console.error('Erro ao carregar planos:', err);
        this.carregando = false;
      }
    });
  }

  formatarValor(valor: number): string {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL'
    }).format(valor);
  }

  getIconeRecurso(recurso: string): string {
    if (recurso.includes('exportações')) return 'download';
    if (recurso.includes('leads')) return 'group';
    if (recurso.includes('e-mail')) return 'email';
    if (recurso.includes('telefone')) return 'phone';
    if (recurso.includes('filtros')) return 'filter_list';
    if (recurso.includes('suporte')) return 'support_agent';
    return 'check';
  }

  isPlanoDestaque(plano: Plano): boolean {
    return plano.nome === 'Profissional';
  }
}