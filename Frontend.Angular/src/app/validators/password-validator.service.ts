import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export class ValidatorService {

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
