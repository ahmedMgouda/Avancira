// Shared password rules used across the Angular app.
// Keep in sync with api/Avancira.Application/Identity/Users/Constants/PasswordRules.cs

export const MIN_PASSWORD_LENGTH = 8;
export const UPPERCASE_PATTERN = /[A-Z]/;
export const LOWERCASE_PATTERN = /[a-z]/;
export const DIGIT_PATTERN = /[0-9]/;
export const SYMBOL_PATTERN = /[^a-zA-Z0-9]/;
