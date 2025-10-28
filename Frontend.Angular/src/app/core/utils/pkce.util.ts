/**
 * PKCE (Proof Key for Public Clients) utilities
 * Implements RFC 7636 S256 (SHA-256) challenge method
 */
export async function createPkcePair(): Promise<{ code_verifier: string; code_challenge: string }> {
  // Generate 32 random bytes for code_verifier
  const verifierBytes = crypto.getRandomValues(new Uint8Array(32));
  const code_verifier = encodeBase64Url(String.fromCharCode(...verifierBytes));

  // SHA-256 hash of verifier
  const encoder = new TextEncoder();
  const data = encoder.encode(code_verifier);
  const digest = await crypto.subtle.digest('SHA-256', data);
  const code_challenge = encodeBase64Url(String.fromCharCode(...new Uint8Array(digest)));

  return { code_verifier, code_challenge };
}

/**
 * Base64 URL encoding (RFC 4648)
 */
function encodeBase64Url(str: string): string {
  return btoa(str).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

/**
 * Generate cryptographically secure random state (CSRF protection)
 */
export function generateRandomState(): string {
  const bytes = new Uint8Array(16);
  crypto.getRandomValues(bytes);
  return Array.from(bytes, (x) => x.toString(16).padStart(2, '0')).join('');
}
