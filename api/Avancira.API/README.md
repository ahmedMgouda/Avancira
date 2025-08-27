# Avancira.API

## Rate limiting

The `reset-password` and `forgot-password` endpoints use a fixed window rate limiter.

- **Limit:** 5 requests per 15-minute window per IP address.
- **Enforcement:** Requests beyond this limit receive HTTP 429 responses.
- **Monitoring:** Rejected requests are logged, enabling detection of potential abuse.

## External authentication

Google and Facebook authentication handlers are added only when their configuration values are present:

- Google requires `Avancira:ExternalServices:Google:ClientId` and `ClientSecret`.
- Facebook requires `Avancira:ExternalServices:Facebook:AppId` and `AppSecret`.

If a provider's configuration is missing or incomplete, the application logs a warning and continues without registering that provider.

## Session management

- `GET /api/auth/sessions` – retrieve active sessions for the current user.
- `DELETE /api/auth/sessions/{id}` – revoke a single session by its ID.
- `POST /api/auth/sessions/batch` – revoke multiple sessions by providing a list of session IDs.

## Cookie configuration

Refresh token and device identifier cookies use `SameSite=Lax` by default to balance
security and usability. The value can be overridden for cross-site scenarios by
setting the `Cookies:SameSite` configuration value (`Cookies__SameSite` environment
variable) to `Strict`, `Lax`, or `None` as needed.

