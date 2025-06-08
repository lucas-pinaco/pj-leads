import { Injectable } from '@angular/core';
import { HttpClient, HttpEvent, HttpEventType, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private baseUrl = 'https://localhost:5001/api/admin'; // ajuste a URL conforme sua API

  constructor(private http: HttpClient) { }

  /**
   * Faz upload de um ou mais arquivos. Retorna um Observable<HttpEvent<any>>
   * para que possamos acompanhar progresso, se quisermos.
   */
  uploadArquivos(arquivos: File[]): Observable<HttpEvent<any>> {
    const formData = new FormData();
    arquivos.forEach(file => {
      formData.append('files', file, file.name);
    });

    // Monta o header sem Content-Type, pois o browser define automaticamente
    return this.http.post<any>(`${this.baseUrl}/upload`, formData, {
      reportProgress: true,
      observe: 'events'
    });
  }

  /**
   * Opcional: listar metadados dos arquivos já enviados.
   */
  listarMetadados(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/listar`);
  }

  /**
   * Opcional: download de um arquivo.
   */
  downloadArquivo(id: number): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/download/${id}`, {
      responseType: 'blob'
    });
  }
  /**
   * Reprocessa um arquivo já carregado.
   */
  reprocessarArquivo(id: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/reprocessar/${id}`, {});
  }


}