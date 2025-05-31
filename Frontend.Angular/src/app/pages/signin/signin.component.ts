import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { loadGapiInsideDOM } from 'gapi-script';
import { FacebookService, InitParams, LoginResponse } from 'ngx-facebook';
import { ToastrService } from 'ngx-toastr';

import { AlertService } from '../../services/alert.service';
import { AuthService } from '../../services/auth.service';
import { ConfigService } from '../../services/config.service';
import { SpinnerService } from '../../services/spinner.service'; 
import { UserService } from '../../services/user.service';



declare const gapi: any;
@Component({
  selector: 'app-signin',
  templateUrl: './signin.component.html',
  styleUrls: ['./signin.component.scss'],
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterModule],
})
export class SigninComponent {
  loginForm!: FormGroup;
  invalidLogin = false;
  returnUrl: string = '/';

  constructor(
    private fb: FacebookService, 
    private form: FormBuilder, 
    private route: ActivatedRoute, 
    private router: Router, 
    private authService: AuthService,
    private userService: UserService,
    private configService: ConfigService, 
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

    // Initialize the Facebook SDK with your App ID
    this.configService.loadConfig().subscribe({
      next: async () => {

        const initParams: InitParams = {
          appId: this.configService.get('facebookAppId'),
          cookie: true,
          xfbml: true,
          version: 'v21.0',
        };
        this.fb.init(initParams);

        await loadGapiInsideDOM();
        gapi.load('auth2', () => {
          gapi.auth2.init({
            client_id: this.configService.get('googleClientId'),
            scope: 'profile email',
          });
        });
      },
      error: (err) => {
        console.error('Failed to load configuration:', err.message);
      },
    });
  }


  // Handle the login form submission
  onLogin(): void {
    if (this.loginForm.invalid) {
      return;
    }

    this.spinner.show();
    this.authService.login(this.loginForm.value.email, this.loginForm.value.password).subscribe({
      next: () => {
        this.router.navigateByUrl(this.returnUrl);
        this.spinner.hide();
      },
      error: (error) => {
        console.error(error);
        this.spinner.hide();
        this.toastr.error('Invalid email or password.', 'Error');
      },
    });
  }

  /** ✅ Facebook Login */
  loginWithFacebook(): void {
    this.spinner.show();
    this.fb
      .login({ scope: 'email,public_profile' })
      .then(async (response: LoginResponse) => {
        const accessToken = response.authResponse.accessToken;
        console.log('✅ Facebook Login Success:', accessToken);
        await this.handleSocialLogin('facebook', accessToken);
      })
      .catch((error) => {
        console.error('❌ Facebook login error:', error);
        this.toastr.error('Invalid email or password.', 'Error');
        this.spinner.hide();
      });
  }

  /** ✅ Google Login */
  async loginWithGoogle(): Promise<void> {
    try {
      this.spinner.show();
      const auth2 = gapi.auth2.getAuthInstance();
      if (!auth2) throw new Error('Google Auth instance not initialized');

      const googleUser = await auth2.signIn();
      const idToken = googleUser.getAuthResponse().id_token;
      console.log('✅ Google Login Success:', idToken);

      await this.handleSocialLogin('google', idToken);
    } catch (error) {
      this.spinner.hide();
      console.error('❌ Google Login Failed:', error);
    }
  }

  /** ✅ Handle Social Login */
  handleSocialLogin(provider: string, token: string): void {
    this.spinner.hide();
    this.authService.socialLogin(provider, token).subscribe({
      next: (result) => {
        this.authService.saveToken(result.token);
        this.authService.saveRoles(result.roles);
        this.router.navigateByUrl(this.returnUrl);
      },
      error: (error) => {
        console.error(`❌ ${provider} login verification failed:`, error.message);
        this.toastr.error('Invalid email or password.', 'Error');
      },
    });
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
