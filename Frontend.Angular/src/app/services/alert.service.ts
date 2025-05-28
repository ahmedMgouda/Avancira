import { Injectable } from '@angular/core';
import Swal from 'sweetalert2';

@Injectable({
  providedIn: 'root'
})
export class AlertService {

  constructor() { }

  /**
 * Show a confirmation dialog using SweetAlert2
 */
  confirm(
    message: string,
    title: string = 'Are you sure?',
    confirmButtonText: string = 'Yes, confirm'
  ): Promise<boolean> {
    return Swal.fire({
      title: title,
      text: message,
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: confirmButtonText,
      cancelButtonText: 'No, cancel',
      confirmButtonColor: '#d33',
      cancelButtonColor: '#3085d6',
    }).then(result => result.isConfirmed);
  }

  /**
   * Show a SweetAlert2 Success Message
   */
  successAlert(message: string, title: string = 'Success', confirmButtonText: string = 'OK') {
    Swal.fire({
      title: title,
      text: message,
      icon: 'success',
      confirmButtonText: confirmButtonText,
      confirmButtonColor: '#28a745',
    });
  }

  /**
   * Show a SweetAlert2 Error Message
   */
  errorAlert(message: string, title: string = 'Error') {
    Swal.fire({
      title: title,
      text: message,
      icon: 'error',
      confirmButtonColor: '#dc3545',
    });
  }

  /**
   * Show a SweetAlert2 Warning Message
   */
  warningAlert(message: string, title: string = 'Warning') {
    Swal.fire({
      title: title,
      text: message,
      icon: 'warning',
      confirmButtonColor: '#ffc107',
    });
  }

  /**
   * Show a SweetAlert2 Info Message
   */
  infoAlert(message: string, title: string = 'Information') {
    Swal.fire({
      title: title,
      text: message,
      icon: 'info',
      confirmButtonColor: '#17a2b8',
    });
  }

  /**
   * Prompt user for input (reusable)
   */
  promptForInput(
    title: string,
    message: string,
    inputType: 'text' | 'email' | 'password',
    placeholder: string,
    confirmButtonText: string = 'Submit'
  ): Promise<string | null> {
    return Swal.fire({
      title: title,
      text: message,
      input: inputType,
      inputPlaceholder: placeholder,
      showCancelButton: true,
      confirmButtonText: confirmButtonText,
      cancelButtonText: 'Cancel',
      preConfirm: (inputValue) => {
        if (!inputValue) {
          Swal.showValidationMessage('This field cannot be empty');
        }
        return inputValue;
      }
    }).then(result => (result.isConfirmed ? result.value : null));
  }

}
