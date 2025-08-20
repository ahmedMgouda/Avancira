# Avancira.API

## Rate limiting

The `reset-password` and `forgot-password` endpoints use a fixed window rate limiter.

- **Limit:** 5 requests per 15-minute window per IP address.
- **Enforcement:** Requests beyond this limit receive HTTP 429 responses.
- **Monitoring:** Rejected requests are logged, enabling detection of potential abuse.

