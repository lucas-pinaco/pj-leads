import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ClienteService, Cliente, Plano } from '../../services/cliente.service';

@Component({
  selector: 'app-cliente-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './cliente-form.component.html',
  styleUrls: ['./cliente-form.component.scss']
})
export class ClienteFormComponent implements OnInit {
  form: FormGroup;
  planos: Plano[] = [];
  salvando = false;
  carregandoPlanos = true;

  constructor(
    private fb: FormBuilder,
    private clienteService: ClienteService,
    public dialogRef: MatDialogRef<ClienteFormComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { cliente: Cliente | null }
  ) {
    this.form = this.createForm();
  }

  ngOnInit(): void {
    this.carregarPlanos();
    if (this.data.cliente) {
      this.preencherForm(this.data.cliente);
    }
  }

  createForm(): FormGroup {
    return this.fb.group({
      razaoSocial: ['', [Validators.required, Validators.maxLength(200)]],
      nomeFantasia: ['', Validators.maxLength(200)],
      cnpj: ['', [Validators.required, Validators.pattern(/^\d{14}$/)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(200)]],
      telefone: ['', [Validators.maxLength(20)]],
      endereco: ['', Validators.maxLength(200)],
      cidade: ['', Validators.maxLength(100)],
      estado: ['', [Validators.maxLength(2), Validators.pattern(/^[A-Z]{2}$/)]],
      cep: ['', [Validators.pattern(/^\d{8}$/)]],
      planoId: ['', Validators.required],
      ativo: [true]
    });
  }

  preencherForm(cliente: Cliente): void {
    this.form.patchValue({
      razaoSocial: cliente.razaoSocial,
      nomeFantasia: cliente.nomeFantasia,
      cnpj: cliente.cnpj,
      email: cliente.email,
      telefone: cliente.telefone,
      endereco: cliente.endereco,
      cidade: cliente.cidade,
      estado: cliente.estado,
      cep: cliente.cep,
      planoId: cliente.planoId,
      ativo: cliente.ativo
    });
  }

  carregarPlanos(): void {
    this.clienteService.listarPlanos().subscribe({
      next: (planos) => {
        this.planos = planos;
        this.carregandoPlanos = false;
      },
      error: (err) => {
        console.error('Erro ao carregar planos:', err);
        this.carregandoPlanos = false;
      }
    });
  }

  salvar(): void {
    if (this.form.invalid) {
      this.marcarCamposComoTocados();
      return;
    }

    this.salvando = true;
    const formValue = this.form.value;

    const operacao = this.data.cliente
      ? this.clienteService.atualizarCliente(this.data.cliente.id, formValue)
      : this.clienteService.criarCliente(formValue);

    operacao.subscribe({
      next: () => {
        this.dialogRef.close(true);
      },
      error: (err) => {
        this.salvando = false;
        alert(err.error?.message || 'Erro ao salvar cliente');
      }
    });
  }

  cancelar(): void {
    this.dialogRef.close(false);
  }

  marcarCamposComoTocados(): void {
    Object.keys(this.form.controls).forEach(key => {
      this.form.get(key)?.markAsTouched();
    });
  }

  formatarCNPJ(event: any): void {
    let value = event.target.value.replace(/\D/g, '');
    if (value.length > 14) {
      value = value.substring(0, 14);
    }
    this.form.get('cnpj')?.setValue(value);
  }

  formatarCEP(event: any): void {
    let value = event.target.value.replace(/\D/g, '');
    if (value.length > 8) {
      value = value.substring(0, 8);
    }
    this.form.get('cep')?.setValue(value);
  }

  formatarEstado(event: any): void {
    const value = event.target.value.toUpperCase();
    this.form.get('estado')?.setValue(value);
  }

  getErrorMessage(fieldName: string): string {
    const field = this.form.get(fieldName);
    if (field?.hasError('required')) {
      return 'Campo obrigatório';
    }
    if (field?.hasError('email')) {
      return 'Email inválido';
    }
    if (field?.hasError('pattern')) {
      if (fieldName === 'cnpj') return 'CNPJ deve ter 14 dígitos';
      if (fieldName === 'cep') return 'CEP deve ter 8 dígitos';
      if (fieldName === 'estado') return 'Use a sigla do estado (ex: SP)';
    }
    if (field?.hasError('maxlength')) {
      return `Máximo ${field.errors?.['maxlength'].requiredLength} caracteres`;
    }
    return '';
  }
}