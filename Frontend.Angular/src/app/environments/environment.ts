export const environment = {
  baseApiUrl: 'https://localhost:9000/',
  apiUrl: `https://localhost:9000/api`,
  frontendUrl: 'https://localhost:4200',
  useSignalR: true,
  /** OAuth client identifier */
  clientId: 'avancira-web',
  /** Callback URL used after authentication */
  redirectUri: 'https://localhost:4200/auth/callback',
  /** URL to redirect to after logging out */
  postLogoutRedirectUri: 'https://localhost:4200',
};
