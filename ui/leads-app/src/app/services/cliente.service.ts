import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Plano {
  id: number;
  nome: string;
  descricao: string;
  valor: number;
  limiteExportacoesMes: number;
  limiteLeadsPorExportacao: number;
  permiteExportarEmail: boolean;
  permiteExportarTelefone: boolean;
  permiteFiltrosAvancados: boolean;
  suportePrioritario: boolean;
  recursos: string[];
}

export interface Cliente {
  id: number;
  razaoSocial: string;
  nomeFantasia: string;
  cnpj: string;
  email: string;
  telefone: string;
  endereco: string;
  cidade: string;
  estado: string;
  cep: string;
  dataCadastro: Date;
  ativo: boolean;
  planoId: number;
  plano?: {
    id: number;
    nome: string;
    valor: number;
  };
  exportacoesRealizadasMes: number;
  statusPagamento: string;
  dataVencimento?: Date;
  totalUsuarios?: number;
}

export interface MeuPerfil {
  cliente: {
    id: number;
    razaoSocial: string;
    nomeFantasia: string;
    cnpj: string;
    email: string;
    statusPagamento: string;
    dataVencimento?: Date;
  };
  plano: {
    id: number;
    nome: string;
    limiteExportacoesMes: number;
    limiteLeadsPorExportacao: number;
    exportacoesUtilizadas: number;
    exportacoesDisponiveis: string;
  };
}

@Injectable({
  providedIn: 'root'
})
export class ClienteService {
  private baseUrl = '/api';

  constructor(private http: HttpClient) {}

  // Planos
  listarPlanos(): Observable<Plano[]> {
    return this.http.get<Plano[]>(`${this.baseUrl}/planos`);
  }

  obterPlano(id: number): Observable<Plano> {
    return this.http.get<Plano>(`${this.baseUrl}/planos/${id}`);
  }

  // Clientes
  listarClientes(pagina: number = 1, tamanhoPagina: number = 10, busca?: string): Observable<any> {
    let params = new HttpParams()
      .set('pagina', pagina.toString())
      .set('tamanhoPagina', tamanhoPagina.toString());

    if (busca) {
      params = params.set('busca', busca);
    }

    return this.http.get<any>(`${this.baseUrl}/clientes`, { params });
  }

  obterCliente(id: number): Observable<Cliente> {
    return this.http.get<Cliente>(`${this.baseUrl}/clientes/${id}`);
  }

  criarCliente(cliente: Partial<Cliente>): Observable<Cliente> {
    return this.http.post<Cliente>(`${this.baseUrl}/clientes`, cliente);
  }

  atualizarCliente(id: number, cliente: Partial<Cliente>): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/clientes/${id}`, cliente);
  }

  excluirCliente(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/clientes/${id}`);
  }

  alterarPlano(clienteId: number, novoPlanoId: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/clientes/${clienteId}/alterar-plano`, {
      novoPlanoId
    });
  }

  // Perfil do usuário logado
  obterMeuPerfil(): Observable<MeuPerfil> {
    return this.http.get<MeuPerfil>(`${this.baseUrl}/clientes/meu-perfil`);
  }
}