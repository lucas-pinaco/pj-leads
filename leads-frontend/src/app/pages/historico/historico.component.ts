import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatExpansionModule } from '@angular/material/expansion';
import { HistoricoService, HistoricoExportacao } from '../../services/historico.service';
import { AuthService } from '../../services/auth.service';
import { saveAs } from 'file-saver';

@Component({
  selector: 'app-historico',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatChipsModule,
    MatTooltipModule,
    MatProgressBarModule,
    MatExpansionModule
  ],
  templateUrl: './historico.component.html',
  styleUrls: ['./historico.component.scss']
})
export class HistoricoComponent implements OnInit {
  displayedColumns: string[] = [
    'data',
    'arquivo',
    'quantidade',
    'destino',
    'status',
    'acoes'
  ];

  // Adicionar coluna cliente se for admin
  isAdmin = false;
  
  historico: HistoricoExportacao[] = [];
  totalItens = 0;
  paginaAtual = 1;
  tamanhoPagina = 10;
  carregando = false;

  // Filtros
  dataInicio: Date | null = null;
  dataFim: Date | null = null;

  constructor(
    private historicoService: HistoricoService,
    private authService: AuthService
  ) {
    // Verificar se é admin
    this.isAdmin = false; // Implementar lógica real
    
    if (this.isAdmin) {
      this.displayedColumns.splice(1, 0, 'cliente');
    }
  }

  ngOnInit(): void {
    // Definir filtro padrão - últimos 30 dias
    this.dataFim = new Date();
    this.dataInicio = new Date();
    this.dataInicio.setDate(this.dataInicio.getDate() - 30);
    
    this.carregarHistorico();
  }

  carregarHistorico(): void {
    this.carregando = true;
    
    this.historicoService.listarHistorico(
      this.paginaAtual,
      this.tamanhoPagina,
      this.dataInicio || undefined,
      this.dataFim || undefined
    ).subscribe({
      next: (response) => {
        this.historico = response.items;
        this.totalItens = response.totalItens;
        this.carregando = false;
      },
      error: (err) => {
        console.error('Erro ao carregar histórico:', err);
        this.carregando = false;
      }
    });
  }

  aplicarFiltros(): void {
    this.paginaAtual = 1;
    this.carregarHistorico();
  }

  limparFiltros(): void {
    this.dataInicio = null;
    this.dataFim = null;
    this.aplicarFiltros();
  }

  mudarPagina(event: any): void {
    this.paginaAtual = event.pageIndex + 1;
    this.tamanhoPagina = event.pageSize;
    this.carregarHistorico();
  }

  verDetalhes(item: HistoricoExportacao): void {
    // Expandir para mostrar os filtros utilizados
    try {
      const filtros = JSON.parse(item.filtrosUtilizados);
      console.log('Filtros utilizados:', filtros);
      // Implementar dialog ou expansão para mostrar detalhes
    } catch (e) {
      console.error('Erro ao parsear filtros:', e);
    }
  }

  reenviarEmail(item: HistoricoExportacao): void {
    if (confirm(`Reenviar arquivo para ${item.emailDestino}?`)) {
      this.historicoService.reenviarEmail(item.id).subscribe({
        next: () => {
          alert('Email reenviado com sucesso!');
          this.carregarHistorico();
        },
        error: (err) => {
          alert(err.error?.message || 'Erro ao reenviar email');
        }
      });
    }
  }

  exportarRelatorio(): void {
    if (!this.dataInicio || !this.dataFim) {
      alert('Selecione o período para exportar o relatório');
      return;
    }

    this.historicoService.exportarRelatorio(this.dataInicio, this.dataFim).subscribe({
      next: (blob) => {
        saveAs(blob, `relatorio-exportacoes-${this.formatarData(new Date())}.xlsx`);
      },
      error: (err) => {
        alert('Erro ao gerar relatório');
      }
    });
  }

  formatarData(data: Date | string): string {
    if (typeof data === 'string') {
      data = new Date(data);
    }
    return data.toLocaleDateString('pt-BR') + ' ' + data.toLocaleTimeString('pt-BR');
  }

  getStatusColor(status: string): string {
    switch (status.toLowerCase()) {
      case 'concluido': return 'primary';
      case 'erro': return 'warn';
      case 'processando': return 'accent';
      default: return '';
    }
  }

  getStatusIcon(status: string): string {
    switch (status.toLowerCase()) {
      case 'concluido': return 'check_circle';
      case 'erro': return 'error';
      case 'processando': return 'hourglass_empty';
      default: return 'help';
    }
  }
}