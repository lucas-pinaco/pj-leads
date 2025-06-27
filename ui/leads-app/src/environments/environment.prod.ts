export const environment = {
  production: true,
  apiUrl: 'https://sua-api-producao.com/api', // ⚠️ ALTERAR PARA SUA API DE PRODUÇÃO
  signalRUrl: 'https://sua-api-producao.com/hub/import-progress', // ⚠️ ALTERAR
  appName: 'PJ Leads',
  version: '1.0.0',
  enableLogging: false, // Desabilitar logs em produção
  enableSignalR: true,
  maxFileSize: 50 * 1024 * 1024, // 50MB
  acceptedFileTypes: ['.csv', '.xlsx', '.xls'],
  defaultPollInterval: 5000, // 5 segundos em produção (menos agressivo)
  auth: {
    tokenKey: 'pj_auth_token',
    userProfileKey: 'pj_user_profile',
    rememberMeKey: 'pj_remember_me',
    rememberEmailKey: 'pj_remember_email'
  }
};