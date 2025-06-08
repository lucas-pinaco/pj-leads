import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../auth.service';

// Módulos Angular Material
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';       // ← IMPORTAR MatIconModule
import { MatCardModule } from '@angular/material/card';       // ← IMPORTAR MatCardModule

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatButtonModule,
    MatToolbarModule,
    MatIconModule,   
    MatCardModule   
  ],
})
export class LoginComponent {
  model = {
    email: '',
    senha: ''
  };
  erroMsg: string | null = null;

  hide: boolean = true;

  constructor(private authService: AuthService, private router: Router) {}

  onSubmit(): void {
    this.erroMsg = null;
    this.authService.login(this.model.email, this.model.senha).subscribe({
      next: () => {
        this.router.navigate(['/']);
      },
      error: err => {
        this.erroMsg = err.error?.message || 'Falha no login. Verifique suas credenciais.';
      }
    });
  }
}
