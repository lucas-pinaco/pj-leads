import { Component, OnInit, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subject, interval } from 'rxjs';
import { takeUntil, switchMap } from 'rxjs/operators';
import { trigger, state, style, transition, animate } from '@angular/animations';
import { AdminService } from '../../services/admin.service';
import { SignalRService } from '../../services/signalr.service';
import { environment } from "../../../environments/environment";
// Interface unificada para JobStatus
interface JobStatus {
  jobId: string;
  usuarioId: number;
  status: 'Iniciado' | 'Processando' | 'Concluido' | 'Erro' | 'Cancelado';
  totalArquivos: number;
  arquivosProcessados: number;
  totalLeads: number;
  leadsProcessados: number;
  iniciado: Date | string; // Permitir string também
  finalizado?: Date | string;
  mensagemErro?: string;
  arquivos: ArquivoJobStatus[];
  progressoPercentual: number;
  tempoDecorrido?: any;
}

interface ArquivoJobStatus {
  nome: string;
  tamanho: number;
  status: 'Aguardando' | 'Processando' | 'Concluido' | 'Erro';
  totalLeads: number;
  leadsProcessados: number;
  iniciado?: Date | string;
  finalizado?: Date | string;
  mensagemErro?: string;
  progressoPercentual: number;
}

interface FileItem {
  file: File;
  id: string;
  status: 'selected' | 'uploading' | 'success' | 'error';
  progress?: number;
  error?: string;
}

interface Statistics {
  totalArquivos: number;
  arquivosHoje: number;
  arquivosSemana: number;
  arquivosMes: number;
  totalLeads: number;
  leadsHoje: number;
  leadsSemana: number;
  leadsMes: number;
  arquivosComErro: number;
  ultimaImportacao?: Date;
}

interface RecentFile {
  id: number;
  nome: string;
  contentType: string;
  dataUpload: Date;
  processadoEm?: Date;
  quantidadeLeads?: number;
  erroProcessamento?: string;
  processado: boolean;
  sucesso: boolean;
  tamanhoFormatado: string;
}

@Component({
  selector: 'app-upload',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './upload.component.html',
  styleUrls: ['./upload.component.scss'],
  animations: [
    trigger('slideIn', [
      transition(':enter', [
        style({ transform: 'translateX(100%)', opacity: 0 }),
        animate('300ms ease-in-out', style({ transform: 'translateX(0)', opacity: 1 }))
      ]),
      transition(':leave', [
        animate('300ms ease-in-out', style({ transform: 'translateX(100%)', opacity: 0 }))
      ])
    ])
  ]
})
export class UploadComponent implements OnInit, OnDestroy {
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

  // Estados do componente
  selectedFiles: FileItem[] = [];
  isDragOver = false;
  isUploading = false;
  useBackgroundProcessing = true;

  // Jobs e dados
  activeJobs: JobStatus[] = [];
  recentFiles: RecentFile[] = [];
  statistics: Statistics | null = null;

  // Mensagens
  successMessage: string | null = null;
  errorMessage: string | null = null;

  // Controle de lifecycle
  private destroy$ = new Subject<void>();

  // ✅ CONFIGURAÇÕES VINDAS DO ENVIRONMENT
  private readonly MAX_FILE_SIZE = environment.maxFileSize;
  private readonly ACCEPTED_TYPES = environment.acceptedFileTypes;
  private readonly POLL_INTERVAL = environment.defaultPollInterval;

  constructor(
    private adminService: AdminService,
    private signalRService: SignalRService
  ) { }

private setupSignalRConnection(): void {
    // ✅ VERIFICAR SE SIGNALR ESTÁ HABILITADO
    if (!environment.enableSignalR) {
      if (environment.enableLogging) {
        console.log('SignalR desabilitado por configuração');
      }
      this.startJobPolling(); // Fallback para polling
      return;
    }

    // Converter JobStatusUpdate para JobStatus
    this.signalRService.jobStatusUpdate$
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (jobStatusUpdate) => {
          const jobStatus: JobStatus = {
            ...jobStatusUpdate,
            iniciado: jobStatusUpdate.iniciado || new Date(),
            arquivos: jobStatusUpdate.arquivos || [],
            progressoPercentual: jobStatusUpdate.progressoPercentual || 0,
            status: jobStatusUpdate.status as 'Iniciado' | 'Processando' | 'Concluido' | 'Erro' | 'Cancelado'
          };
          this.updateJobStatus(jobStatus);
        },
        error: (error) => {
          if (environment.enableLogging) {
            console.warn('Erro no SignalR JobStatusUpdate:', error);
          }
        }
      });

    this.signalRService.connect()
      .catch(err => {
        if (environment.enableLogging) {
          console.warn('Erro ao conectar SignalR:', err);
        }
        // Fallback para polling se SignalR falhar
        this.startJobPolling();
      });
  }

  /**
   * Versão melhorada do handleFiles com logging condicional
   */
  private handleFiles(files: File[]): void {
    if (environment.enableLogging) {
      console.log('📥 Processando', files.length, 'arquivo(s)');
    }
    
    const validFiles: FileItem[] = [];
    const errors: string[] = [];

    files.forEach(file => {
      if (environment.enableLogging) {
        console.log('🔍 Validando arquivo:', file.name);
      }
      
      // Validar tipo
      const extension = this.getFileExtension(file.name);
      if (!this.ACCEPTED_TYPES.includes(extension)) {
        const error = `${file.name}: Tipo não suportado. Use ${this.ACCEPTED_TYPES.join(', ')}.`;
        errors.push(error);
        if (environment.enableLogging) {
          console.warn('❌', error);
        }
        return;
      }

      // Validar tamanho
      if (file.size > this.MAX_FILE_SIZE) {
        const maxSizeMB = Math.round(this.MAX_FILE_SIZE / (1024 * 1024));
        const error = `${file.name}: Arquivo muito grande. Máximo ${maxSizeMB}MB.`;
        errors.push(error);
        if (environment.enableLogging) {
          console.warn('❌', error);
        }
        return;
      }

      // Verificar duplicação
      if (!this.isFileUnique(file)) {
        const error = `${file.name}: Arquivo já selecionado.`;
        errors.push(error);
        if (environment.enableLogging) {
          console.warn('❌', error);
        }
        return;
      }

      const fileItem: FileItem = {
        file,
        id: this.generateFileId(),
        status: 'selected'
      };

      validFiles.push(fileItem);
      if (environment.enableLogging) {
        console.log('✅ Arquivo válido adicionado:', file.name, 'ID:', fileItem.id);
      }
    });

    // Adicionar arquivos válidos
    if (validFiles.length > 0) {
      this.selectedFiles = [...this.selectedFiles, ...validFiles];
      if (environment.enableLogging) {
        console.log('📋 Total de arquivos após adição:', this.selectedFiles.length);
      }
    }

    // Mostrar erros se houver
    if (errors.length > 0) {
      this.showError(errors.join('\n'));
    }

    // Limpar input
    if (this.fileInput) {
      this.fileInput.nativeElement.value = '';
    }
  }

  /**
   * Remove arquivo da lista
   */
  removeFile(fileItem: FileItem): void {
    this.selectedFiles = this.selectedFiles.filter(f => f.id !== fileItem.id);

    // Log para debug
    if (environment.enableLogging) {
      console.log('Arquivo removido:', fileItem.file.name);
      console.log('Arquivos restantes:', this.selectedFiles.length);
    }
  }

  /**
   * Limpa todos os arquivos
   */
  clearFiles(): void {
    this.selectedFiles = [];

    // Limpar também o input se existir
    if (this.fileInput) {
      this.fileInput.nativeElement.value = '';
    }

    if (environment.enableLogging) {
      console.log('Todos os arquivos removidos');
    }
  }

  /**
   * Inicia polling para jobs ativos
   */
  private startJobPolling(): void {
    interval(this.POLL_INTERVAL)
      .pipe(
        takeUntil(this.destroy$),
        switchMap(() => this.adminService.getMyJobs())
      )
      .subscribe({
        next: (jobs) => {
          this.activeJobs = jobs.filter((job: JobStatus) =>
            job.status === 'Iniciado' || job.status === 'Processando'
          );
        },
        error: (err) => {
          if (environment.enableLogging) {
            console.warn('Erro no polling de jobs:', err);
          }
        }
      });
  }

  /**
   * Carrega dados iniciais
   */
  private async loadInitialData(): Promise<void> {
    try {
      // Carregar estatísticas
      this.adminService.getEstatisticas()
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (stats) => this.statistics = stats,
          error: (err) => {
            if (environment.enableLogging) {
              console.warn('Erro ao carregar estatísticas:', err);
            }
          }
        });

      // Carregar arquivos recentes
      this.adminService.listarMetadados(1, 5)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response) => this.recentFiles = response.arquivos || [],
          error: (err) => {
            if (environment.enableLogging) {
              console.warn('Erro ao carregar arquivos recentes:', err);
            }
          }
        });

      // Carregar jobs ativos
      this.refreshJobs();
    } catch (error) {
      if (environment.enableLogging) {
        console.error('Erro ao carregar dados iniciais:', error);
      }
    }
  }

  /**
   * Override do ngOnInit para adicionar logging condicional
   */
  ngOnInit(): void {
    if (environment.enableLogging) {
      console.log('🚀 Inicializando componente de upload');
      console.log('Environment:', {
        production: environment.production,
        apiUrl: environment.apiUrl,
        signalREnabled: environment.enableSignalR,
        maxFileSize: `${Math.round(environment.maxFileSize / (1024 * 1024))}MB`,
        acceptedTypes: environment.acceptedFileTypes
      });
    }
    
    this.loadInitialData();
    this.setupSignalRConnection();
    this.startJobPolling();
  }

  /**
   * Override do ngOnDestroy para cleanup
   */
  ngOnDestroy(): void {
    if (environment.enableLogging) {
      console.log('🛑 Destruindo componente de upload');
    }
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ✅ MÉTODOS AUXILIARES ATUALIZADOS
  formatFileSize(bytes: number): string {
    const sizes = ['B', 'KB', 'MB', 'GB'];
    if (bytes === 0) return '0 B';
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return `${(bytes / Math.pow(1024, i)).toFixed(1)} ${sizes[i]}`;
  }

  getFileType(fileName: string): string {
    const extension = this.getFileExtension(fileName);
    switch (extension) {
      case '.csv': return 'CSV';
      case '.xlsx': return 'XLSX';
      case '.xls': return 'XLS';
      default: return 'Arquivo';
    }
  }

  getFileExtension(fileName: string): string {
    return fileName.toLowerCase().substring(fileName.lastIndexOf('.'));
  }

  formatJobTime(date: Date | string): string {
    const dateObj = typeof date === 'string' ? new Date(date) : date;
    return dateObj.toLocaleString('pt-BR');
  }

  /**
   * Atualiza status de um job específico
   */
  private updateJobStatus(jobStatus: JobStatus): void {
    const index = this.activeJobs.findIndex(j => j.jobId === jobStatus.jobId);

    if (index >= 0) {
      this.activeJobs[index] = {
        ...jobStatus,
        iniciado: new Date(jobStatus.iniciado), // Garantir que seja Date
        finalizado: jobStatus.finalizado ? new Date(jobStatus.finalizado) : undefined
      };

      // Remover job se completado ou com erro
      if (jobStatus.status === 'Concluido' || jobStatus.status === 'Erro') {
        setTimeout(() => {
          this.activeJobs = this.activeJobs.filter(j => j.jobId !== jobStatus.jobId);
          this.loadInitialData(); // Recarregar dados
        }, 3000);
      }
    } else if (jobStatus.status === 'Iniciado' || jobStatus.status === 'Processando') {
      this.activeJobs.push({
        ...jobStatus,
        iniciado: new Date(jobStatus.iniciado),
        finalizado: jobStatus.finalizado ? new Date(jobStatus.finalizado) : undefined
      });
    }
  }

  /**
   * Eventos de drag and drop
   */
  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      // Converter FileList para Array
      const filesArray = Array.from(files);
      this.handleFiles(filesArray);
    }
  }

  /**
   * Evento de seleção de arquivos
   */
  onFileSelect(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      // Converter FileList para Array
      const filesArray = Array.from(input.files);
      this.handleFiles(filesArray);

      // IMPORTANTE: Limpar o input imediatamente após processar
      input.value = '';
    }
  }

  /**
   * Processa arquivos selecionados
   */
  // private handleFiles(files: File[]): void {
  //   const validFiles: FileItem[] = [];
  //   const errors: string[] = [];

  //   files.forEach(file => {
  //     // Validar tipo
  //     const extension = this.getFileExtension(file.name);
  //     if (!this.ACCEPTED_TYPES.includes(extension)) {
  //       errors.push(`${file.name}: Tipo não suportado. Use CSV, XLSX ou XLS.`);
  //       return;
  //     }

  //     // Validar tamanho
  //     if (file.size > this.MAX_FILE_SIZE) {
  //       errors.push(`${file.name}: Arquivo muito grande. Máximo 50MB.`);
  //       return;
  //     }

  //     // CORREÇÃO: Verificar duplicação mais específica
  //     const isDuplicate = this.selectedFiles.some(existingFile =>
  //       existingFile.file.name === file.name &&
  //       existingFile.file.size === file.size &&
  //       existingFile.file.lastModified === file.lastModified
  //     );

  //     if (isDuplicate) {
  //       errors.push(`${file.name}: Arquivo já selecionado.`);
  //       return;
  //     }

  //     validFiles.push({
  //       file,
  //       id: this.generateFileId(),
  //       status: 'selected'
  //     });
  //   });

  //   // CORREÇÃO: Verificar se já existem arquivos com os mesmos IDs antes de adicionar
  //   const newFiles = validFiles.filter(newFile =>
  //     !this.selectedFiles.some(existing => existing.id === newFile.id)
  //   );

  //   // Adicionar apenas arquivos novos
  //   this.selectedFiles = [...this.selectedFiles, ...newFiles];

  //   // Mostrar erros se houver
  //   if (errors.length > 0) {
  //     this.showError(errors.join('\n'));
  //   }

  //   // Limpar input - IMPORTANTE para evitar problemas de estado
  //   if (this.fileInput) {
  //     this.fileInput.nativeElement.value = '';
  //   }
  // }


  /**
   * Inicia upload
   */
  async startUpload(): Promise<void> {
    if (this.selectedFiles.length === 0 || this.isUploading) {
      return;
    }

    this.isUploading = true;
    this.clearMessages();

    try {
      if (this.useBackgroundProcessing) {
        await this.uploadWithBackground();
      } else {
        await this.uploadSynchronous();
      }
    } catch (error) {
      console.error('Erro no upload:', error);
      this.showError('Erro inesperado durante o upload.');
    } finally {
      this.isUploading = false;
    }
  }

  /**
   * Upload com processamento em background
   */
  private async uploadWithBackground(): Promise<void> {
    const formData = new FormData();

    this.selectedFiles.forEach(fileItem => {
      formData.append('files', fileItem.file);
    });

    this.adminService.uploadBackground(formData)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.showSuccess(`Upload iniciado! ${response.totalArquivos} arquivo(s) em processamento.`);
          this.selectedFiles = [];
          this.refreshJobs();
        },
        error: (error) => {
          this.showError(error.error?.message || 'Erro ao iniciar upload em background.');
        }
      });
  }

  /**
   * Upload síncrono
   */
  private async uploadSynchronous(): Promise<void> {
    const formData = new FormData();

    this.selectedFiles.forEach(fileItem => {
      formData.append('files', fileItem.file);
      fileItem.status = 'uploading';
    });

    this.adminService.uploadArquivos(formData)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response: any) => {
          this.selectedFiles.forEach(fileItem => {
            fileItem.status = 'success';
          });

          this.showSuccess(`Upload concluído! ${response.totalLeadsImportados || 0} leads importados.`);

          setTimeout(() => {
            this.selectedFiles = [];
            this.loadInitialData();
          }, 2000);
        },
        error: (error) => {
          this.selectedFiles.forEach(fileItem => {
            fileItem.status = 'error';
            fileItem.error = 'Erro no upload';
          });

          this.showError(error.error?.message || 'Erro durante o upload.');
        }
      });
  }

  /**
   * Atualiza lista de jobs
   */
  refreshJobs(): void {
    this.adminService.getMyJobs()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (jobs) => {
          this.activeJobs = jobs.filter((job: JobStatus) =>
            job.status === 'Iniciado' || job.status === 'Processando'
          );
        },
        error: (err) => console.warn('Erro ao atualizar jobs:', err)
      });
  }

  /**
   * Download de arquivo
   */
  downloadFile(arquivo: RecentFile): void {
    this.adminService.downloadArquivo(arquivo.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = arquivo.nome;
          a.click();
          URL.revokeObjectURL(url);
        },
        error: (err) => {
          this.showError('Erro ao fazer download do arquivo.');
        }
      });
  }

  /**
   * Reprocessa arquivo
   */
  reprocessFile(arquivo: RecentFile): void {
    if (confirm(`Reprocessar o arquivo ${arquivo.nome}?`)) {
      this.adminService.reprocessarArquivo(arquivo.id)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (response) => {
            this.showSuccess(response.message);
            this.loadInitialData();
          },
          error: (err) => {
            this.showError(err.error?.message || 'Erro ao reprocessar arquivo.');
          }
        });
    }
  }

  // Métodos utilitários
  trackFile(index: number, item: FileItem): string {
    return item.id; // Usar ID único ao invés do index
  }


  getJobStatusClass(status: string): string {
    switch (status) {
      case 'Processando':
      case 'Iniciado':
        return 'processing';
      case 'Concluido':
        return 'completed';
      case 'Erro':
        return 'error';
      default:
        return '';
    }
  }

  getJobStatusText(status: string): string {
    switch (status) {
      case 'Iniciado': return 'Iniciado';
      case 'Processando': return 'Processando';
      case 'Concluido': return 'Concluído';
      case 'Erro': return 'Erro';
      case 'Cancelado': return 'Cancelado';
      default: return status;
    }
  }

  getFileStatusClass(arquivo: any): string {
    if (typeof arquivo === 'string') {
      // Para ArquivoJobStatus
      switch (arquivo) {
        case 'Processando': return 'processing';
        case 'Concluido': return 'completed';
        case 'Erro': return 'error';
        default: return '';
      }
    } else {
      // Para RecentFile
      if (arquivo.erroProcessamento) return 'error';
      if (arquivo.processado) return 'success';
      return 'processing';
    }
  }

  getFileStatusText(arquivo: any): string {
    if (typeof arquivo === 'string') {
      // Para ArquivoJobStatus
      switch (arquivo) {
        case 'Aguardando': return 'Aguardando';
        case 'Processando': return 'Processando';
        case 'Concluido': return 'Concluído';
        case 'Erro': return 'Erro';
        default: return arquivo;
      }
    } else {
      // Para RecentFile
      if (arquivo.erroProcessamento) return 'Erro';
      if (arquivo.processado) return 'Processado';
      return 'Processando';
    }
  }

  // Métodos de mensagens
  private showSuccess(message: string): void {
    this.successMessage = message;
    this.errorMessage = null;
    setTimeout(() => this.clearMessages(), 5000);
  }

  private showError(message: string): void {
    this.errorMessage = message;
    this.successMessage = null;
    setTimeout(() => this.clearMessages(), 8000);
  }

  clearMessage(): void {
    this.successMessage = null;
    this.errorMessage = null;
  }

  private clearMessages(): void {
    this.successMessage = null;
    this.errorMessage = null;
  }

  private generateFileId(): string {
    const timestamp = Date.now().toString(36);
    const random = Math.random().toString(36).substring(2, 15);
    return `file_${timestamp}_${random}`;
  }

  private debugFileList(): void {
    console.log('=== DEBUG FILE LIST ===');
    this.selectedFiles.forEach((fileItem, index) => {
      console.log(`${index}: ${fileItem.file.name} (ID: ${fileItem.id})`);
    });
    console.log('=====================');
  }

  // getFileStatusText(status: string): string {
  // switch (status) {
  //   case 'selected': return 'Selecionado';
  //   case 'uploading': return 'Enviando';
  //   case 'success': return 'Sucesso';
  //   case 'error': return 'Erro';
  //   default: return status;
  // }



/**
 * Método de debugging para verificar estado dos arquivos
 */
// private logFileState(): void {
//   if (!environment.production) { // Só em desenvolvimento
//     console.group('🗂️ Estado dos Arquivos');
//     console.log('Total de arquivos:', this.selectedFiles.length);
    
//     this.selectedFiles.forEach((fileItem, index) => {
//       console.log(`${index + 1}. ${fileItem.file.name}`, {
//         id: fileItem.id,
//         size: this.formatFileSize(fileItem.file.size),
//         status: fileItem.status,
//         lastModified: new Date(fileItem.file.lastModified).toISOString()
//       });
//     });
    
//     console.groupEnd();
//   }
// }

/**
 * Valida se o arquivo é realmente único
 */
private isFileUnique(newFile: File): boolean {
    return !this.selectedFiles.some(existingFileItem => {
      const existing = existingFileItem.file;
      return (
        existing.name === newFile.name &&
        existing.size === newFile.size &&
        existing.lastModified === newFile.lastModified &&
        existing.type === newFile.type
      );
    });
  }


}