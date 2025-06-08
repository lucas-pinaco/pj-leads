import { Component, OnInit } from '@angular/core';
import { HttpEventType, HttpResponse } from '@angular/common/http';
import { AdminService } from '../admin.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';  
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';

interface ArquivoInfo {
  id: number;
  nome: string;
  contentType: string;
  dataUpload: Date;
  processadoEm?: Date;
  quantidadeLeads?: number;
  erroProcessamento?: string;
  processado: boolean;
  sucesso: boolean;
}

@Component({
  selector: 'app-upload',
  templateUrl: './upload.component.html',
  styleUrls: ['./upload.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatButtonModule,
    MatToolbarModule,
    MatIconModule,   
    MatCardModule,
    MatListModule,
    MatProgressBarModule,
    MatTableModule,
    MatChipsModule
  ],
})
export class UploadComponent implements OnInit {
  arquivosSelecionados: File[] = [];
  progressoEnvio: number = 0;
  mensagem: string | null = null;
  estaEnviando: boolean = false;
  arquivosProcessados: ArquivoInfo[] = [];
  displayedColumns: string[] = ['nome', 'dataUpload', 'status', 'quantidadeLeads', 'acoes'];

  constructor(private adminService: AdminService) {}

  ngOnInit(): void {
    this.carregarArquivos();
  }

  // Carrega lista de arquivos processados
  carregarArquivos(): void {
    this.adminService.listarMetadados().subscribe({
      next: (arquivos) => {
        this.arquivosProcessados = arquivos;
      },
      error: (err) => {
        console.error('Erro ao carregar arquivos:', err);
      }
    });
  }

  // Captura os arquivos selecionados
  onFileChange(event: any): void {
    const files: FileList = event.target.files;
    this.arquivosSelecionados = [];
    
    for (let i = 0; i < files.length; i++) {
      const file = files.item(i)!;
      const ext = file.name.toLowerCase();
      
      // Validação de tipo de arquivo
      if (!ext.endsWith('.csv') && !ext.endsWith('.xlsx') && !ext.endsWith('.xls')) {
        this.mensagem = 'Apenas arquivos CSV e Excel são aceitos.';
        continue;
      }
      
      this.arquivosSelecionados.push(file);
    }
    
    this.progressoEnvio = 0;
    if (this.arquivosSelecionados.length === 0 && files.length > 0) {
      this.mensagem = 'Nenhum arquivo válido selecionado. Use apenas CSV ou Excel.';
    } else {
      this.mensagem = null;
    }
  }

  // Envia e processa os arquivos
  upload(): void {
    if (!this.arquivosSelecionados.length) {
      this.mensagem = 'Selecione pelo menos um arquivo CSV ou Excel.';
      return;
    }

    this.progressoEnvio = 0;
    this.estaEnviando = true;
    this.mensagem = null;

    this.adminService.uploadArquivos(this.arquivosSelecionados).subscribe({
      next: (event) => {
        if (event.type === HttpEventType.UploadProgress) {
          if (event.total) {
            this.progressoEnvio = Math.round(100 * (event.loaded / event.total));
          }
        } else if (event instanceof HttpResponse) {
          this.estaEnviando = false;
          const response = event.body;
          this.mensagem = `${response.message} Total de ${response.totalLeadsImportados} leads importados.`;
          
          // Recarrega a lista de arquivos
          this.carregarArquivos();
          
          // Limpa seleção
          this.arquivosSelecionados = [];
        }
      },
      error: (err) => {
        this.estaEnviando = false;
        this.mensagem = err.error?.message || 'Erro ao enviar arquivos.';
      }
    });
  }

  // Reprocessa um arquivo
  reprocessar(arquivoId: number): void {
    if (confirm('Deseja reprocessar este arquivo?')) {
      this.adminService.reprocessarArquivo(arquivoId).subscribe({
        next: (response) => {
          this.mensagem = response.message;
          this.carregarArquivos();
        },
        error: (err) => {
          this.mensagem = err.error?.message || 'Erro ao reprocessar arquivo.';
        }
      });
    }
  }

  // Download de um arquivo
  baixar(arquivo: ArquivoInfo): void {
    this.adminService.downloadArquivo(arquivo.id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = arquivo.nome;
        a.click();
        URL.revokeObjectURL(url);
      },
      error: (err) => {
        this.mensagem = 'Erro ao baixar arquivo.';
      }
    });
  }
}