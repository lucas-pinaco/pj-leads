import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ExportacaoService } from '../../services/exportacao.service';
import { ClienteService } from '../../services/cliente.service';
import { saveAs } from 'file-saver';

// Angular Material Modules
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';

@Component({
  standalone: true,
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
  imports: [
    CommonModule,
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatButtonModule,
    MatToolbarModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatCardModule,
    MatChipsModule,
    MatIconModule
  ],
})
export class HomeComponent implements OnInit {
  meuPerfil: any = null;
  carregandoPerfil = true;
  isAdmin = false;

  filtros = {
    RazaoOuFantasia: '',
    CNAE: '',
    NaturezaJuridica: '',
    SituacoesCadastrais: [] as string[],
    Estado: '',
    Municipio: '',
    Bairro: '',
    CEP: '',
    DDD: '',
    DataAberturaDe: null as Date | null,
    DataAberturaAte: null as Date | null,
    CapitalSocialMinimo: null as number | null,
    CapitalSocialMaximo: null as number | null,
    SomenteMEI: false,
    ExcluirMEI: false,
    SomenteMatriz: false,
    SomenteFilial: false,
    ComTelefone: false,
    SomenteFixo: false,
    SomenteCelular: false,
    ComEmail: false,
    Quantidade: 100,
    IncluirDuplicados: false,
    EmailDestino: '',
    NomeArquivo: '',
  };

  situacoes = ['ATIVA', 'BAIXADA', 'INAPTA', 'SUSPENSA'];

  constructor(
    private exportacaoService: ExportacaoService,
    private clienteService: ClienteService
  ) {}

  ngOnInit(): void {
    this.carregarPerfil();
  }

  carregarPerfil(): void {
    this.clienteService.obterMeuPerfil().subscribe({
      next: (perfil: any) => {
        this.meuPerfil = perfil;
        this.isAdmin = perfil.isAdmin || false;
        this.carregandoPerfil = false;
        
        // Admin não tem limite, outros usuários sim
        if (!this.isAdmin && perfil.plano.limiteLeadsPorExportacao < this.filtros.Quantidade) {
          this.filtros.Quantidade = perfil.plano.limiteLeadsPorExportacao;
        }
      },
      error: (err) => {
        console.error('Erro ao carregar perfil:', err);
        this.carregandoPerfil = false;
      }
    });
  }

  get limiteAtingido(): boolean {
    // Admin nunca tem limite atingido
    if (this.isAdmin) return false;
    
    if (!this.meuPerfil) return false;
    
    const limite = this.meuPerfil.plano.limiteExportacoesMes;
    const utilizadas = this.meuPerfil.plano.exportacoesUtilizadas;
    
    return limite !== -1 && utilizadas >= limite;
  }

  get podeExportar(): boolean {
    return !this.carregandoPerfil && (this.isAdmin || !this.limiteAtingido);
  }

  toggleSituacao(situacao: string) {
    const index = this.filtros.SituacoesCadastrais.indexOf(situacao);
    if (index >= 0) {
      this.filtros.SituacoesCadastrais.splice(index, 1);
    } else {
      this.filtros.SituacoesCadastrais.push(situacao);
    }
  }

  exportar() {
    if (!this.podeExportar) {
      alert('Você atingiu o limite de exportações do seu plano.');
      return;
    }

    const f = this.filtros;
  
    const filtrosCorrigidos = {
      ...this.filtros,
      EmailDestino: this.filtros.EmailDestino || 'meuemail@teste.com',
      NomeArquivo: this.filtros.NomeArquivo || 'leads-exportados.xlsx',
      DataAberturaDe: this.filtros.DataAberturaDe instanceof Date ? this.filtros.DataAberturaDe.toISOString() : null,
      DataAberturaAte: this.filtros.DataAberturaAte instanceof Date ? this.filtros.DataAberturaAte.toISOString() : null
    };
  
    const vazio = Object.values(filtrosCorrigidos).every(x => x === '' || x === null || x === false || (Array.isArray(x) && x.length === 0));
    if (vazio) {
      alert('Preencha ao menos um filtro antes de exportar.');
      return;
    }
  
    this.exportacaoService.exportarLeads(filtrosCorrigidos).subscribe({
      next: (blob) => {
        saveAs(blob, 'leads-exportados.xlsx');
        // Recarregar perfil para atualizar contador
        this.carregarPerfil();
      },
      error: (err) => {
        console.error('Erro ao exportar:', err);
        alert(err.error?.message || 'Erro ao exportar leads. Verifique os filtros.');
      }
    });
  }
}