/**
 * Parse JWT payload without verification
 * Only used for expiry timing (not for security decisions)
 */
export interface JwtPayload {
  exp?: number;
  iat?: number;
  nbf?: number;
  [key: string]: any;
}

/**
 * Decode Base64URL-encoded string (RFC 4648)
 * Handles non-ASCII payloads safely using TextDecoder (no deprecated escape)
 */
function decodeBase64Url(base64: string): string {
  // Convert Base64URL to Base64
  base64 = base64.replace(/-/g, '+').replace(/_/g, '/');
  
  // Add padding
  const pad = base64.length % 4 ? 4 - (base64.length % 4) : 0;
  base64 += '='.repeat(pad);
  
  // Decode binary string to bytes, then UTF-8 decode safely
  const binaryString = atob(base64);
  const bytes = new Uint8Array(binaryString.length);
  for (let i = 0; i < binaryString.length; i++) {
    bytes[i] = binaryString.charCodeAt(i);
  }
  return new TextDecoder().decode(bytes);
}

export function parseJwtPayload(token: string): JwtPayload | null {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) {
      console.warn('Invalid JWT format');
      return null;
    }
    const payload = JSON.parse(decodeBase64Url(parts[1]));
    return payload as JwtPayload;
  } catch (error) {
    console.warn('Failed to parse JWT payload:', error);
    return null;
  }
}

/**
 * Extract exp claim in seconds (Unix timestamp)
 */
export function getJwtExpSeconds(token: string): number | null {
  const payload = parseJwtPayload(token);
  return payload?.exp && typeof payload.exp === 'number' ? payload.exp : null;
}

