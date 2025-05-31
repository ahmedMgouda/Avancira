import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { firstValueFrom } from 'rxjs';
import { finalize } from 'rxjs/operators';

import { AlertService } from '../../services/alert.service';
import { AuthService } from '../../services/auth.service';
import { SpinnerService } from '../../services/spinner.service';
import { SocialAuthService } from '../../services/social-auth.service';
import { UserService } from '../../services/user.service';
@Component({
  selector: 'app-signin',
  templateUrl: './signin.component.html',
  styleUrls: ['./signin.component.scss'],
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule],
})
export class SigninComponent {
  loginForm!: FormGroup;
  returnUrl: string = '/';
  showPassword = false;
  errorMessage = '';

  constructor(
    private socialAuth: SocialAuthService,
    private form: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService,
    private userService: UserService,
    private toastr: ToastrService,
    private spinner: SpinnerService,
    private alertService: AlertService
  ) { }

  ngOnInit(): void {
    // Initialize the form with Reactive Forms
    this.loginForm = this.form.group({
      email: [
        '',
        [
          Validators.required,
          Validators.email,
        ],
      ],
      password: ['', [Validators.required]],
    });

    // Get the returnUrl from query params or use the default '/'
    this.route.queryParams.subscribe(params => {
      this.returnUrl = params['returnUrl'] || '/';
    });

    // Initialize external auth providers (Facebook/Google)
    this.socialAuth.init();
  }


  // Handle the login form submission
  onLogin(): void {
    if (this.loginForm.invalid) {
      return;
    }

    this.spinner.show();
    this.authService
      .login(this.loginForm.value.email, this.loginForm.value.password)
      .pipe(finalize(() => this.spinner.hide()))
      .subscribe({
        next: () => {
          this.router.navigateByUrl(this.returnUrl);
        },
        error: (error) => {
          console.error(error);
          this.errorMessage = 'Invalid email or password.';
          this.toastr.error(this.errorMessage, 'Error');
        },
      });
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  /** ✅ Facebook Login */
  loginWithFacebook(): void {
    this.spinner.show();
    this.socialAuth
      .loginWithFacebook()
      .then(token => this.handleSocialLogin('facebook', token))
      .catch(error => {
        console.error('❌ Facebook login error:', error);
        this.toastr.error('Invalid email or password.', 'Error');
      })
      .finally(() => this.spinner.hide());
  }

  /** ✅ Google Login */
  async loginWithGoogle(): Promise<void> {
    try {
      this.spinner.show();
      const token = await this.socialAuth.loginWithGoogle();
      await this.handleSocialLogin('google', token);
    } catch (error) {
      console.error('❌ Google Login Failed:', error);
    } finally {
      this.spinner.hide();
    }
  }

  /** ✅ Handle Social Login */
  async handleSocialLogin(provider: string, token: string): Promise<void> {
    try {
      const result = await firstValueFrom(
        this.authService.socialLogin(provider, token)
      );
      this.authService.saveToken(result.token);
      this.authService.saveRoles(result.roles);
      this.router.navigateByUrl(this.returnUrl);
    } catch (error: any) {
      console.error(`❌ ${provider} login verification failed:`, error.message);
      this.errorMessage = 'Invalid email or password.';
      this.toastr.error(this.errorMessage, 'Error');
    }
  }

  async resetPassword() {
    const email = await this.alertService.promptForInput(
      'Reset my password',
      'To retrieve your password, please enter the e-mail address associated with your account below.',
      'email',
      'Enter your email',
      'Send'
    );

    if (email) {
      this.userService.requestPasswordReset(email).subscribe();

      this.alertService.successAlert(
        'Check your email',
        "An email with instructions on how to reset your password has been sent to you. If you don't receive this email, please check your spam folder.",
        'Return to Sign-in'
      );
    }

  }
}
