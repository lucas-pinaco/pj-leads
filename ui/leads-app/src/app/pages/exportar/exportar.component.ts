import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ExportacaoService } from '../../services/exportacao.service';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { saveAs } from 'file-saver';

@Component({
  selector: 'app-exportar',
  templateUrl: './exportar.component.html',
  styleUrls: ['./exportar.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatButtonModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatCardModule,
    MatIconModule,
    MatChipsModule,
    MatExpansionModule,
    MatProgressBarModule
  ]
})
export class ExportarComponent {
  filtros = {
    incluirDuplicados: false,
    quantidade: 100,
    razaoOuFantasia: '',
    cnae: '',
    naturezaJuridica: '',
    situacoesCadastrais: [] as string[],
    estado: '',
    municipio: '',
    bairro: '',
    cep: '',
    ddd: '',
    dataAberturaDe: null as Date | null,
    dataAberturaAte: null as Date | null,
    capitalSocialMinimo: null as number | null,
    capitalSocialMaximo: null as number | null,
    somenteMEI: false,
    excluirMEI: false,
    somenteMatriz: false,
    somenteFilial: false,
    comTelefone: false,
    somenteFixo: false,
    somenteCelular: false,
    comEmail: false,
    emailDestino: '',
    nomeArquivo: 'leads-exportados.xlsx'
  };

  situacoesCadastraisOpcoes = [
    'Ativa',
    'Baixada',
    'Inapta',
    'Suspensa',
    'Nula'
  ];

  estados = [
    'AC', 'AL', 'AP', 'AM', 'BA', 'CE', 'DF', 'ES', 'GO', 'MA',
    'MT', 'MS', 'MG', 'PA', 'PB', 'PR', 'PE', 'PI', 'RJ', 'RN',
    'RS', 'RO', 'RR', 'SC', 'SP', 'SE', 'TO'
  ];

  exportando = false;
  mensagemErro: string | null = null;
  mensagemSucesso: string | null = null;

  constructor(private exportacaoService: ExportacaoService) {}

  exportar(): void {
    this.exportando = true;
    this.mensagemErro = null;
    this.mensagemSucesso = null;

    // Log para debug
    console.log('Iniciando exportação com filtros:', this.filtros);

    this.exportacaoService.exportarLeads(this.filtros).subscribe({
      next: (blob) => {
        // Salvar arquivo
        const nomeArquivo = this.filtros.nomeArquivo || 'leads-exportados.xlsx';
        saveAs(blob, nomeArquivo);
        
        this.mensagemSucesso = 'Exportação realizada com sucesso!';
        this.exportando = false;

        // Se foi enviado por email também
        if (this.filtros.emailDestino) {
          this.mensagemSucesso += ` O arquivo também foi enviado para ${this.filtros.emailDestino}`;
        }
      },
      error: (err) => {
        console.error('Erro na exportação:', err);
        this.mensagemErro = err.error?.message || 'Erro ao exportar leads. Verifique os filtros e tente novamente.';
        this.exportando = false;
      }
    });
  }

  limparFiltros(): void {
    this.filtros = {
      incluirDuplicados: false,
      quantidade: 100,
      razaoOuFantasia: '',
      cnae: '',
      naturezaJuridica: '',
      situacoesCadastrais: [],
      estado: '',
      municipio: '',
      bairro: '',
      cep: '',
      ddd: '',
      dataAberturaDe: null,
      dataAberturaAte: null,
      capitalSocialMinimo: null,
      capitalSocialMaximo: null,
      somenteMEI: false,
      excluirMEI: false,
      somenteMatriz: false,
      somenteFilial: false,
      comTelefone: false,
      somenteFixo: false,
      somenteCelular: false,
      comEmail: false,
      emailDestino: '',
      nomeArquivo: 'leads-exportados.xlsx'
    };
    this.mensagemErro = null;
    this.mensagemSucesso = null;
  }

  onTipoEmpresaChange(): void {
    // Se marcar somente MEI, desmarcar excluir MEI
    if (this.filtros.somenteMEI && this.filtros.excluirMEI) {
      this.filtros.excluirMEI = false;
    }
    // Se marcar excluir MEI, desmarcar somente MEI
    if (this.filtros.excluirMEI && this.filtros.somenteMEI) {
      this.filtros.somenteMEI = false;
    }
  }

  onTipoEstabelecimentoChange(): void {
    // Se marcar somente matriz, desmarcar somente filial
    if (this.filtros.somenteMatriz && this.filtros.somenteFilial) {
      this.filtros.somenteFilial = false;
    }
    // Se marcar somente filial, desmarcar somente matriz
    if (this.filtros.somenteFilial && this.filtros.somenteMatriz) {
      this.filtros.somenteMatriz = false;
    }
  }

  onTipoTelefoneChange(): void {
    // Se marcar somente fixo, desmarcar somente celular
    if (this.filtros.somenteFixo && this.filtros.somenteCelular) {
      this.filtros.somenteCelular = false;
    }
    // Se marcar somente celular, desmarcar somente fixo
    if (this.filtros.somenteCelular && this.filtros.somenteFixo) {
      this.filtros.somenteFixo = false;
    }
  }
}