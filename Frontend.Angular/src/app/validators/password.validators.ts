import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const UPPERCASE_PATTERN = /[A-Z]/;
export const LOWERCASE_PATTERN = /[a-z]/;
export const DIGIT_PATTERN = /[0-9]/;
export const SYMBOL_PATTERN = /[^a-zA-Z0-9]/;

/**
 * Validates that a control's value meets common password complexity rules.
 * Returns individual error flags for missing character classes.
 */
export function passwordComplexityValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = control.value as string;
    if (!value) {
      return null;
    }

    const errors: ValidationErrors = {};
    if (!UPPERCASE_PATTERN.test(value)) {
      errors['noUppercase'] = true;
    }
    if (!LOWERCASE_PATTERN.test(value)) {
      errors['noLowercase'] = true;
    }
    if (!DIGIT_PATTERN.test(value)) {
      errors['noDigit'] = true;
    }
    if (!SYMBOL_PATTERN.test(value)) {
      errors['noNonAlphanumeric'] = true;
    }

    return Object.keys(errors).length ? errors : null;
  };
}
