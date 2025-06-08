import { TestBed } from '@angular/core/testing';

import { ExportacaoService } from './exportacao.service';

describe('ExportacaoService', () => {
  let service: ExportacaoService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ExportacaoService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
