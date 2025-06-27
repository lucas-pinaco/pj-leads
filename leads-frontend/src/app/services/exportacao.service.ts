import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ExportacaoService {
   private baseUrl = `${environment.apiUrl}/exportacao`;

  constructor(private http: HttpClient) {}

  exportarLeads(filtros: any) {
    return this.http.post(`${this.baseUrl}/exportar`, filtros, {
      responseType: 'blob',
    });
  }
}