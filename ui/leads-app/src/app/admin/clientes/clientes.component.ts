import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatChipsModule } from '@angular/material/chips';
import { MatMenuModule } from '@angular/material/menu';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ClienteService, Cliente } from '../../services/cliente.service';
import { ClienteFormComponent } from './cliente-form/cliente-form.component';
// import { ClienteFormComponent } from './cliente-form/cliente-form.component';

@Component({
  selector: 'app-clientes',
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
    MatChipsModule,
    MatMenuModule,
    MatDialogModule,
    MatProgressBarModule
  ],
  templateUrl: './clientes.component.html',
  styleUrls: ['./clientes.component.scss']
})
export class ClientesComponent implements OnInit {
  displayedColumns: string[] = [
    'razaoSocial', 
    'cnpj', 
    'email', 
    'plano', 
    'status', 
    'exportacoes',
    'acoes'
  ];
  
  clientes: Cliente[] = [];
  totalItens = 0;
  paginaAtual = 1;
  tamanhoPagina = 10;
  busca = '';
  carregando = false;

  constructor(
    private clienteService: ClienteService,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.carregarClientes();
  }

  carregarClientes(): void {
    this.carregando = true;
    this.clienteService.listarClientes(this.paginaAtual, this.tamanhoPagina, this.busca)
      .subscribe({
        next: (response) => {
          this.clientes = response.clientes;
          this.totalItens = response.totalItens;
          this.carregando = false;
        },
        error: (err) => {
          console.error('Erro ao carregar clientes:', err);
          this.carregando = false;
        }
      });
  }

  pesquisar(): void {
    this.paginaAtual = 1;
    this.carregarClientes();
  }

  mudarPagina(event: any): void {
    this.paginaAtual = event.pageIndex + 1;
    this.tamanhoPagina = event.pageSize;
    this.carregarClientes();
  }

  novoCliente(): void {
    const dialogRef = this.dialog.open(ClienteFormComponent, {
      width: '600px',
      data: { cliente: null }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.carregarClientes();
      }
    });
    return;
  }

  editarCliente(cliente: Cliente): void {
    const dialogRef = this.dialog.open(ClienteFormComponent, {
      width: '600px',
      data: { cliente }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.carregarClientes();
      }
    });

    return;
  }

  excluirCliente(cliente: Cliente): void {
    if (confirm(`Deseja realmente excluir o cliente ${cliente.razaoSocial}?`)) {
      this.clienteService.excluirCliente(cliente.id).subscribe({
        next: () => {
          this.carregarClientes();
        },
        error: (err) => {
          alert(err.error?.message || 'Erro ao excluir cliente');
        }
      });
    }
  }

  alterarPlano(cliente: Cliente): void {
    // Implementar dialog para alterar plano
    console.log('Alterar plano', cliente);
  }

  formatarCNPJ(cnpj: string): string {
    if (!cnpj) return '';
    return cnpj.replace(/^(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})$/, '$1.$2.$3/$4-$5');
  }

  formatarValor(valor: number): string {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL'
    }).format(valor);
  }

  getStatusColor(status: string): string {
    switch (status.toLowerCase()) {
      case 'pago': return 'primary';
      case 'pendente': return 'warn';
      case 'vencido': return 'error';
      default: return '';
    }
  }
}