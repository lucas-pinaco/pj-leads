export const environment = {
  production: true,
  apiUrl: 'https://leads-api-production.up.railway.app/api',
  signalRUrl: 'https://leads-api-production.up.railway.app/hub/import-progress', 
  appName: 'PJ Leads',
  version: '1.0.0',
  enableLogging: false, 
  enableSignalR: true,
  maxFileSize: 5 * 1024 * 1024, // 5MB
  acceptedFileTypes: ['.csv', '.xlsx', '.xls'],
  defaultPollInterval: 5000, // 5 segundos em produção (menos agressivo)
  auth: {
    tokenKey: 'pj_auth_token',
    userProfileKey: 'pj_user_profile',
    rememberMeKey: 'pj_remember_me',
    rememberEmailKey: 'pj_remember_email'
  }
};