import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

import {
  UPPERCASE_PATTERN,
  LOWERCASE_PATTERN,
  DIGIT_PATTERN,
  SYMBOL_PATTERN,
} from './password-rules';

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

/**
 * Validates that the `newPassword` and `confirmPassword` controls
 * within a form group contain identical values.
 */
export function matchPasswords(): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const newPassword = group.get('newPassword')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;

    if (!newPassword || !confirmPassword) {
      return null;
    }

    return newPassword === confirmPassword
      ? null
      : { passwordsMismatch: true };
  };
}
