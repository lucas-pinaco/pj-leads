// signalr.service.ts - Correções

import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { BehaviorSubject, Subject, Observable } from 'rxjs';
import { AuthService } from './auth.service';
import { environment } from '../../environments/environment';

// Interface atualizada para compatibilidade
export interface JobStatusUpdate {
  jobId: string;
  usuarioId: number;
  status: 'Iniciado' | 'Processando' | 'Concluido' | 'Erro' | 'Cancelado'; // Tipos específicos
  totalArquivos: number;
  arquivosProcessados: number;
  totalLeads: number;
  leadsProcessados: number;
  progressoPercentual: number;
  mensagemErro?: string;
  arquivos: ArquivoJobStatus[];
  iniciado?: Date | string;
  finalizado?: Date | string;
}

export interface ArquivoJobStatus {
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

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection: HubConnection | null = null;
  private connectionStateSubject = new BehaviorSubject<'disconnected' | 'connecting' | 'connected'>('disconnected');

  // Subjects para eventos
  private jobStatusUpdateSubject = new Subject<JobStatusUpdate>();
  private userJobsUpdateSubject = new Subject<JobStatusUpdate[]>();

  // Observables públicos
  public connectionState$ = this.connectionStateSubject.asObservable();
  public jobStatusUpdate$ = this.jobStatusUpdateSubject.asObservable();
  public userJobsUpdate$ = this.userJobsUpdateSubject.asObservable();

  private readonly hubUrl = environment.signalRUrl;

  constructor(private authService: AuthService) { }

  /**
   * Conecta ao hub SignalR
   */
  public async connect(): Promise<void> {
    if (this.hubConnection?.state === 'Connected') {
      return;
    }

    try {
      this.connectionStateSubject.next('connecting');

      // Obter token atual
      const token = this.authService.tokenValue;
      if (!token) {
        throw new Error('Token de autenticação não disponível');
      }

      // Criar conexão
      this.hubConnection = new HubConnectionBuilder()
        .withUrl(this.hubUrl, {
          accessTokenFactory: () => token,
          skipNegotiation: false, // Permitir negociação
          transport: 1 | 2 | 4 // WebSockets, ServerSentEvents, LongPolling
        })
        .withAutomaticReconnect([0, 2000, 10000, 30000]) // Retry delays in ms
        .configureLogging(LogLevel.Information)
        .build();

      // Configurar event handlers
      this.setupEventHandlers();

      // Conectar
      await this.hubConnection.start();

      this.connectionStateSubject.next('connected');
      console.log('SignalR conectado com sucesso');

      // Solicitar jobs do usuário após conectar
      await this.requestMyJobs();

    } catch (error) {
      console.error('Erro ao conectar SignalR:', error);
      this.connectionStateSubject.next('disconnected');
      throw error;
    }
  }

  /**
   * Configura os event handlers do SignalR
   */
  private setupEventHandlers(): void {
    if (!this.hubConnection) return;

    // Handler para atualizações de status de job
    this.hubConnection.on('JobStatusUpdate', (jobStatus: any) => {
      console.log('Job status update recebido:', jobStatus);

      // Validar e converter o objeto recebido
      const validatedJobStatus: JobStatusUpdate = {
        jobId: jobStatus.jobId || '',
        usuarioId: jobStatus.usuarioId || 0,
        status: this.validateStatus(jobStatus.status),
        totalArquivos: jobStatus.totalArquivos || 0,
        arquivosProcessados: jobStatus.arquivosProcessados || 0,
        totalLeads: jobStatus.totalLeads || 0,
        leadsProcessados: jobStatus.leadsProcessados || 0,
        progressoPercentual: jobStatus.progressoPercentual || 0,
        mensagemErro: jobStatus.mensagemErro,
        arquivos: this.validateArquivos(jobStatus.arquivos || []),
        iniciado: jobStatus.iniciado,
        finalizado: jobStatus.finalizado
      };

      this.jobStatusUpdateSubject.next(validatedJobStatus);
    });

    // Handler para lista de jobs do usuário
    this.hubConnection.on('UserJobsUpdate', (jobs: any[]) => {
      console.log('User jobs update recebido:', jobs);

      const validatedJobs = jobs.map(job => ({
        jobId: job.jobId || '',
        usuarioId: job.usuarioId || 0,
        status: this.validateStatus(job.status),
        totalArquivos: job.totalArquivos || 0,
        arquivosProcessados: job.arquivosProcessados || 0,
        totalLeads: job.totalLeads || 0,
        leadsProcessados: job.leadsProcessados || 0,
        progressoPercentual: job.progressoPercentual || 0,
        mensagemErro: job.mensagemErro,
        arquivos: this.validateArquivos(job.arquivos || []),
        iniciado: job.iniciado,
        finalizado: job.finalizado
      }));

      this.userJobsUpdateSubject.next(validatedJobs);
    });

    // Handlers de conexão
    this.hubConnection.onreconnecting((error) => {
      console.warn('SignalR reconectando...', error);
      this.connectionStateSubject.next('connecting');
    });

    this.hubConnection.onreconnected((connectionId) => {
      console.log('SignalR reconectado:', connectionId);
      this.connectionStateSubject.next('connected');
      this.requestMyJobs().catch(err => console.error('Erro ao solicitar jobs após reconexão:', err));
    });

    this.hubConnection.onclose((error) => {
      console.warn('SignalR conexão fechada:', error);
      this.connectionStateSubject.next('disconnected');
    });
  }

  /**
   * Valida o status recebido
   */
  private validateStatus(status: any): 'Iniciado' | 'Processando' | 'Concluido' | 'Erro' | 'Cancelado' {
    const validStatuses = ['Iniciado', 'Processando', 'Concluido', 'Erro', 'Cancelado'];
    return validStatuses.includes(status) ? status : 'Erro';
  }

  /**
   * Valida os arquivos recebidos
   */
  private validateArquivos(arquivos: any[]): ArquivoJobStatus[] {
    return arquivos.map(arquivo => ({
      nome: arquivo.nome || '',
      tamanho: arquivo.tamanho || 0,
      status: this.validateArquivoStatus(arquivo.status),
      totalLeads: arquivo.totalLeads || 0,
      leadsProcessados: arquivo.leadsProcessados || 0,
      iniciado: arquivo.iniciado,
      finalizado: arquivo.finalizado,
      mensagemErro: arquivo.mensagemErro,
      progressoPercentual: arquivo.progressoPercentual || 0
    }));
  }

  /**
   * Valida o status do arquivo
   */
  private validateArquivoStatus(status: any): 'Aguardando' | 'Processando' | 'Concluido' | 'Erro' {
    const validStatuses = ['Aguardando', 'Processando', 'Concluido', 'Erro'];
    return validStatuses.includes(status) ? status : 'Erro';
  }

  // Resto dos métodos permanecem iguais...
  public async disconnect(): Promise<void> {
    if (this.hubConnection) {
      try {
        await this.hubConnection.stop();
        this.connectionStateSubject.next('disconnected');
        console.log('SignalR desconectado');
      } catch (error) {
        console.error('Erro ao desconectar SignalR:', error);
      }
    }
  }

  public async getJobStatus(jobId: string): Promise<void> {
    if (this.hubConnection?.state === 'Connected') {
      try {
        await this.hubConnection.invoke('GetJobStatus', jobId);
      } catch (error) {
        console.error('Erro ao solicitar status do job:', error);
      }
    }
  }

  public async requestMyJobs(): Promise<void> {
    if (this.hubConnection?.state === 'Connected') {
      try {
        await this.hubConnection.invoke('GetMyJobs');
      } catch (error) {
        console.error('Erro ao solicitar jobs do usuário:', error);
      }
    }
  }

  public get isConnected(): boolean {
    return this.hubConnection?.state === 'Connected';
  }

  public get connectionState(): string {
    return this.hubConnection?.state || 'Disconnected';
  }

  public async reconnect(): Promise<void> {
    await this.disconnect();
    await this.connect();
  }
}