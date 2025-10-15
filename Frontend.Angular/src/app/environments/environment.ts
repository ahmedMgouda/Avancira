export const environment = {
  baseApiUrl: 'https://localhost:9200',
  apiUrl: `https://localhost:9200/api`,
  authUrl: `https://localhost:9200/bff/auth`,
  frontendUrl: 'https://localhost:9200',
  useSignalR: true,
  /** OAuth client identifier */
  clientId: 'avancira-web',
  /** Callback URL used after authentication */
  redirectUri: 'https://localhost:9200/auth/callback',
  /** URL to redirect to after logging out */
  postLogoutRedirectUri: 'https://localhost:9200',

  production: false,
};
