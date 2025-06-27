import { Injectable } from '@angular/core';
import { HttpClient, HttpEvent, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private baseUrl = `${environment.apiUrl}/admin`;

  constructor(private http: HttpClient) {}

  /**
   * Upload com processamento em background
   */
  uploadBackground(formData: FormData): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/upload-background`, formData);
  }

  /**
   * Upload síncrono (método original)
   */
  uploadArquivos(formData: FormData): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/upload`, formData);
  }

  /**
   * Consultar status de um job específico
   */
  getJobStatus(jobId: string): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/job-status/${jobId}`);
  }

  /**
   * Listar jobs do usuário atual
   */
  getMyJobs(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/my-jobs`);
  }

  /**
   * Listar metadados dos arquivos
   */
  listarMetadados(page: number = 1, pageSize: number = 10): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/listar?page=${page}&pageSize=${pageSize}`);
  }

  /**
   * Download de arquivo
   */
  downloadArquivo(id: number): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/download/${id}`, {
      responseType: 'blob'
    });
  }

  /**
   * Reprocessar arquivo
   */
  reprocessarArquivo(id: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/reprocessar/${id}`, {});
  }

  /**
   * Obter estatísticas
   */
  getEstatisticas(): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/estatisticas`);
  }
}