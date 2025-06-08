import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ExportacaoService {
  private baseUrl = '/api/exportacao';
  // ajuste se necessário

  constructor(private http: HttpClient) {}

  exportarLeads(filtros: any) {
    return this.http.post(`${this.baseUrl}/exportar`, filtros, {
      responseType: 'blob',
    });
  }
}