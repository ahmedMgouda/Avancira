import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export class ValidatorService {

  // Validator for non-alphanumeric characters
  static hasNonAlphanumeric(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const value = control.value;
      if (value && !/[^\w\s]/.test(value)) {
        return { noNonAlphanumeric: true };
      }
      return null;
    };
  }

  // Validator for at least one lowercase letter
  static hasLowercase(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const value = control.value;
      if (value && !/[a-z]/.test(value)) {
        return { noLowercase: true };
      }
      return null;
    };
  }

  // Validator for at least one uppercase letter
  static hasUppercase(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const value = control.value;
      if (value && !/[A-Z]/.test(value)) {
        return { noUppercase: true };
      }
      return null;
    };
  }

  static matchesPassword(passwordField: string): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.parent) {
        return null; // Skip validation if control is not yet attached to a form group
      }

      const password = control.parent.get(passwordField)?.value;
      const confirmPassword = control.value;

      if (password && confirmPassword && password !== confirmPassword) {
        return { passwordsMismatch: true };
      }

      return null; // Valid case
    };
  }
}
