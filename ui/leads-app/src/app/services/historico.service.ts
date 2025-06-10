import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface HistoricoExportacao {
  id: number;
  clienteId: number;
  cliente?: {
    razaoSocial: string;
    cnpj: string;
  };
  usuarioId: number;
  usuario?: {
    nomeUsuario: string;
    email: string;
  };
  dataExportacao: Date;
  quantidadeLeads: number;
  filtrosUtilizados: string;
  nomeArquivo: string;
  emailDestino: string;
  enviadoPorEmail: boolean;
  status: string;
  mensagemErro?: string;
  planoNome: string;
  limiteDisponivel: number;
}

@Injectable({
  providedIn: 'root'
})
export class HistoricoService {
  private baseUrl = '/api/historico';

  constructor(private http: HttpClient) {}

  listarHistorico(
    pagina: number = 1,
    tamanhoPagina: number = 10,
    dataInicio?: Date,
    dataFim?: Date,
    clienteId?: number
  ): Observable<any> {

    debugger
    let params = new HttpParams()
      .set('pagina', pagina.toString())
      .set('tamanhoPagina', tamanhoPagina.toString());

    if (dataInicio) {
      params = params.set('dataInicio', dataInicio.toISOString());
    }

    if (dataFim) {
      params = params.set('dataFim', dataFim.toISOString());
    }

    if (clienteId) {
      params = params.set('clienteId', clienteId.toString());
    }

    return this.http.get<any>(`${this.baseUrl}`, { params });
  }

  obterDetalhes(id: number): Observable<HistoricoExportacao> {
    return this.http.get<HistoricoExportacao>(`${this.baseUrl}/${id}`);
  }

  reenviarEmail(id: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/${id}/reenviar-email`, {});
  }

  exportarRelatorio(dataInicio: Date, dataFim: Date): Observable<Blob> {
    const params = new HttpParams()
      .set('dataInicio', dataInicio.toISOString())
      .set('dataFim', dataFim.toISOString());

    return this.http.get(`${this.baseUrl}/relatorio`, {
      params,
      responseType: 'blob'
    });
  }
}