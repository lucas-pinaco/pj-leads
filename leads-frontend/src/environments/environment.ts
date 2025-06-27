export const environment = {
  production: false,
  apiUrl: 'https://localhost:7167/api',
  signalRUrl: 'https://localhost:7167/hub/import-progress',
  appName: 'PJ Leads',
  version: '1.0.0',
  enableLogging: true,
  enableSignalR: true,
  maxFileSize: 50 * 1024 * 1024, // 50MB
  acceptedFileTypes: ['.csv', '.xlsx', '.xls'],
  defaultPollInterval: 2000, // 2 segundos
  auth: {
    tokenKey: 'pj_auth_token',
    userProfileKey: 'pj_user_profile',
    rememberMeKey: 'pj_remember_me',
    rememberEmailKey: 'pj_remember_email'
  }
};