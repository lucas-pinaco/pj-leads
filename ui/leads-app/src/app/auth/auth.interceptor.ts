import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.tokenValue;
  
  // Log para debug
  console.log('Interceptor - Token:', token ? 'Present' : 'Missing');
  console.log('Interceptor - URL:', req.url);
  
  if (token && !req.url.includes('/auth/')) {
    const cloned = req.clone({
      headers: req.headers.set('Authorization', `Bearer ${token}`)
    });
    console.log('Interceptor - Adding Authorization header');
    return next(cloned);
  }
  
  return next(req);
};