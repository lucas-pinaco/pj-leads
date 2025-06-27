import { Component, OnInit, OnDestroy, Inject, PLATFORM_ID } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { StorageService } from '../../services/storage.service';

interface FloatingIcon {
  x: number;
  y: number;
  delay: number;
}

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit, OnDestroy {
  // Modelo de dados do formulário
  model = {
    email: '',
    senha: ''
  };

  // Estados do componente
  showPassword = false;
  rememberMe = false;
  isLoading = false;
  errorMessage: string | null = null;

  // Ícones flutuantes para o background
  iconPositions: FloatingIcon[] = [];

  // Subject para controlar unsubscribe
  private destroy$ = new Subject<void>();

  constructor(
    private authService: AuthService,
    private router: Router,
    private storageService: StorageService,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {
    this.generateFloatingIcons();
  }

  ngOnInit(): void {
    // Verificar se já está logado
    if (this.authService.isAuthenticated()) {
      this.router.navigate(['/']);
      return;
    }

    // Limpar mensagens de erro ao inicializar
    this.errorMessage = null;

    // Se há dados salvos no localStorage (lembrar de mim)
    this.loadSavedCredentials();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }



  /**
   * Verifica se está executando no browser
   */
  private get isBrowser(): boolean {
    return isPlatformBrowser(this.platformId);
  }


  /**
   * Limpa mensagens de erro quando usuário começa a digitar
   */


  /**
   * Announcement para screen readers
   */


  /**
   * Detectimport { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../auth.service';
import { StorageService } from '../../services/storage.service';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

interface FloatingIcon {
  x: number;
  y: number;
  delay: number;
}

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit, OnDestroy {
  // Modelo de dados do formulário
  model = {
    email: '',
    senha: ''
  };

  // Estados do componente
  showPassword = false;
  rememberMe = false;
  isLoading = false;
  errorMessage: string | null = null;

  // Ícones flutuantes para o background
  iconPositions: FloatingIcon[] = [];

  // Subject para controlar unsubscribe
  private destroy$ = new Subject<void>();

  constructor(
    private authService: AuthService,
    private router: Router,
    private storageService: StorageService
  ) {
    this.generateFloatingIcons();
  }

  ngOnInit(): void {
    // Verificar se já está logado
    if (this.authService.isAuthenticated()) {
      this.router.navigate(['/']);
      return;
    }

    // Limpar mensagens de erro ao inicializar
    this.errorMessage = null;

    // Se há dados salvos no localStorage (lembrar de mim)
    this.loadSavedCredentials();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Gera posições aleatórias para os ícones flutuantes no background
   */
  private generateFloatingIcons(): void {
    const iconCount = 20; // Número de ícones flutuantes
    this.iconPositions = [];

    for (let i = 0; i < iconCount; i++) {
      this.iconPositions.push({
        x: Math.random() * 100, // Posição X em %
        y: Math.random() * 100, // Posição Y em %
        delay: Math.random() * 6 // Delay da animação em segundos
      });
    }
  }

  /**
   * Carrega credenciais salvas se "lembrar de mim" estiver ativo
   */
  private loadSavedCredentials(): void {
    const savedEmail = this.storageService.getItem('pj_remember_email');
    const savedRemember = this.storageService.getItem('pj_remember_me');

    if (savedEmail && savedRemember === 'true') {
      this.model.email = savedEmail;
      this.rememberMe = true;
    }
  }

  /**
   * Salva ou remove credenciais baseado na opção "lembrar de mim"
   */
  private handleRememberMe(): void {
    if (this.rememberMe) {
      this.storageService.setItem('pj_remember_email', this.model.email);
      this.storageService.setItem('pj_remember_me', 'true');
    } else {
      this.storageService.removeItem('pj_remember_email');
      this.storageService.removeItem('pj_remember_me');
    }
  }

  /**
   * Alterna visibilidade da senha
   */
  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }

  /**
   * Valida os dados do formulário
   */
  private validateForm(): boolean {
    // Reset de erro
    this.errorMessage = null;

    // Validação de email
    if (!this.model.email) {
      this.errorMessage = 'E-mail é obrigatório';
      return false;
    }

    if (!this.isValidEmail(this.model.email)) {
      this.errorMessage = 'E-mail deve ter um formato válido';
      return false;
    }

    // Validação de senha
    if (!this.model.senha) {
      this.errorMessage = 'Senha é obrigatória';
      return false;
    }

    // if (this.model.senha.length < 6) {
    //   this.errorMessage = 'Senha deve ter pelo menos 6 caracteres';
    //   return false;
    // }

    return true;
  }

  /**
   * Valida formato do email
   */
  private isValidEmail(email: string): boolean {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }

  /**
   * Submete o formulário de login
   */
  onSubmit(): void {
    // Validar formulário
    if (!this.validateForm()) {
      return;
    }

    // Iniciar loading
    this.isLoading = true;
    this.errorMessage = null;

    // Chamada para o serviço de autenticação
    this.authService.login(this.model.email.trim(), this.model.senha)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          // Login bem-sucedido
          this.isLoading = false;

          // Gerenciar "lembrar de mim"
          this.handleRememberMe();

          // Log para debug
          console.log('Login realizado com sucesso:', response);

          // Redirecionar para página principal
          this.router.navigate(['/']);
        },
        error: (error) => {
          // Erro no login
          this.isLoading = false;

          console.error('Erro no login:', error);

          // Determinar mensagem de erro
          if (error.status === 401) {
            this.errorMessage = 'E-mail ou senha incorretos';
          } else if (error.status === 0) {
            this.errorMessage = 'Erro de conexão. Verifique sua internet e tente novamente';
          } else if (error.error?.message) {
            this.errorMessage = error.error.message;
          } else {
            this.errorMessage = 'Ocorreu um erro inesperado. Tente novamente';
          }

          // Limpar senha em caso de erro
          this.model.senha = '';
        }
      });
  }

  /**
   * Navega para página de recuperação de senha
   */
  goToForgotPassword(): void {
    // Implementar navegação para recuperação de senha
    console.log('Navegando para recuperação de senha');
    // this.router.navigate(['/auth/forgot-password']);
  }

  /**
   * Navega para página de registro
   */
  goToRegister(): void {
    // Implementar navegação para registro
    console.log('Navegando para registro');
    // this.router.navigate(['/auth/register']);
  }

  /**
   * Tratamento de teclas especiais
   */
  onKeyPress(event: KeyboardEvent): void {
    // Enter submete o formulário
    if (event.key === 'Enter') {
      this.onSubmit();
    }
  }

  /**
   * Limpa mensagens de erro quando usuário começa a digitar
   */
  onInputChange(): void {
    if (this.errorMessage) {
      this.errorMessage = null;
    }
  }

  /**
   * Métodos para acessibilidade e UX
   */

  /**
   * Focus no primeiro campo com erro
   */
  private focusFirstError(): void {
    if (!this.storageService.isAvailable()) {
      return;
    }

    setTimeout(() => {
      const errorElement = document.querySelector('.error') as HTMLElement;
      if (errorElement) {
        errorElement.focus();
      }
    }, 100);
  }

  /**
   * Announcement para screen readers
   */
  private announceError(message: string): void {
    if (!this.storageService.isAvailable()) {
      return;
    }

    try {
      // Criar elemento para anunciar erro para screen readers
      const announcement = document.createElement('div');
      announcement.setAttribute('aria-live', 'polite');
      announcement.setAttribute('aria-atomic', 'true');
      announcement.className = 'sr-only';
      announcement.textContent = `Erro: ${message}`;

      document.body.appendChild(announcement);

      // Remover após anunciar
      setTimeout(() => {
        if (document.body.contains(announcement)) {
          document.body.removeChild(announcement);
        }
      }, 1000);
    } catch (error) {
      console.warn('Erro ao anunciar mensagem:', error);
    }
  }

  /**
   * Detecta se está sendo executado em dispositivo móvel
   */
  get isMobile(): boolean {
    if (!this.storageService.isAvailable()) {
      return false;
    }
    return window.innerWidth <= 768;
  }

  /**
   * Detecta se usuário prefere motion reduzido
   */
  get prefersReducedMotion(): boolean {
    if (!this.storageService.isAvailable()) {
      return false;
    }
    return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  }
}