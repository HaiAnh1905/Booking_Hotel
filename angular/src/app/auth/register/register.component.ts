import { Component, OnInit, AfterViewInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AuthService, ConfigStateService, LocalizationPipe } from '@abp/ng.core';
import { OAuthStorage } from 'angular-oauth2-oidc';
import { environment } from '../../../environments/environment';
import { AuthProxyService } from '../services/auth-proxy.service';
import { PasswordCriteriaState, checkPasswordCriteria, passwordComplexityValidator } from '../validators/password-complexity.validator';

declare const google: any;

@Component({
  selector: 'app-auth-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, LocalizationPipe],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent implements OnInit, AfterViewInit {
  private fb = inject(FormBuilder);
  private authProxy = inject(AuthProxyService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private authService = inject(AuthService);
  private oAuthStorage = inject(OAuthStorage);
  private configState = inject(ConfigStateService);
  private cdr = inject(ChangeDetectorRef);

  currentStep = 1; // 1: Account, 2: Name, 3: Phone, 4: Done
  registrationToken = '';
  registeredEmail = '';
  isLoading = false;
  isGoogleLoaded = false;
  isGoogleOnboarding = false;
  errorMessage = '';

  step1Form!: FormGroup;
  step2Form!: FormGroup;
  step3Form!: FormGroup;

  get passwordCriteria(): PasswordCriteriaState {
    const val = this.step1Form?.get('password')?.value || '';
    return checkPasswordCriteria(val);
  }

  ngOnInit(): void {
    // Nếu người dùng đã đăng nhập thành công trước đó thì buộc chuyển hướng về trang chủ
    const token = this.oAuthStorage.getItem('access_token') || (typeof localStorage !== 'undefined' ? localStorage.getItem('access_token') : null);
    const hasAccessToken = !!token && token !== 'undefined' && token !== 'null';
    if (this.authService.isAuthenticated || hasAccessToken) {
      this.router.navigate(['/']);
      return;
    }

    this.step1Form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, passwordComplexityValidator]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordMatchValidator });

    this.step2Form = this.fb.group({
      fullName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(128)]]
    });

    this.step3Form = this.fb.group({
      phoneNumber: ['', [
        Validators.required,
        Validators.pattern(/^(\+84|0)(3[2-9]|5[6|8|9]|7[0|6-9]|8[1-9]|9[0-9])[0-9]{7}$/)
      ]]
    });

    // Kiểm tra nếu có Google Token hoặc Onboarding Token từ query param
    this.route.queryParams.subscribe(params => {
      if (params['token']) {
        this.isGoogleOnboarding = true;
        this.registrationToken = params['token'];
        this.registeredEmail = params['email'] || '';
        if (params['name']) {
          this.step2Form.patchValue({ fullName: params['name'] });
        }
        this.currentStep = 2; // Chuyển thẳng tới trang điền thông tin người dùng mới (Bước 2: Họ và Tên)
        this.cdr.detectChanges();
      } else if (params['fromGoogle']) {
        this.isGoogleOnboarding = true;
      }

      if (params['googleToken']) {
        this.isGoogleOnboarding = true;
        this.handleGoogleInit(params['googleToken']);
      }
    });
  }

  ngAfterViewInit(): void {
    this.initGoogleSignUp();
  }

  private initGoogleSignUp(): void {
    if (typeof window === 'undefined') return;
    const clientId = environment.googleClientId;
    if (!clientId) return;

    let attempts = 0;
    const checkGsi = () => {
      if (typeof google !== 'undefined' && google.accounts?.id) {
        google.accounts.id.initialize({
          client_id: clientId,
          callback: (res: any) => this.handleGoogleInit(res?.credential)
        });

        const btnEl = document.getElementById('googleBtnRegister');
        if (btnEl) {
          google.accounts.id.renderButton(btnEl, {
            theme: 'outline',
            size: 'large',
            width: 320,
            text: 'signup_with',
            shape: 'rectangular',
            logo_alignment: 'left'
          });
          this.isGoogleLoaded = true;
          this.cdr.detectChanges();
        }
      } else if (attempts < 25) {
        attempts++;
        setTimeout(checkGsi, 200);
      }
    };
    checkGsi();
  }

  passwordMatchValidator(form: FormGroup) {
    const password = form.get('password')?.value;
    const confirm = form.get('confirmPassword')?.value;
    return password === confirm ? null : { passwordMismatch: true };
  }

  onStep1Submit(): void {
    if (this.step1Form.invalid) return;

    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    this.authProxy.registerStep1(this.step1Form.value).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.registrationToken = res.registrationToken;
        this.registeredEmail = res.email;
        this.currentStep = 2;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err?.error?.error?.message || 'Không thể khởi tạo đăng ký. Vui lòng thử lại.';
        this.cdr.detectChanges();
      }
    });
  }

  handleGoogleInit(token?: string): void {
    const idToken = token;
    if (!idToken) {
      if (typeof google !== 'undefined' && google.accounts?.id) {
        google.accounts.id.prompt();
        return;
      }
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    this.authProxy.registerGoogleInit({ idToken }).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.isGoogleOnboarding = true;
        this.registrationToken = res.registrationToken;
        this.registeredEmail = res.email;
        if (res.prefilledName) {
          this.step2Form.patchValue({ fullName: res.prefilledName });
        }
        this.currentStep = 2; // Luôn chuyển tới Bước 2 để người dùng xác nhận Họ và Tên
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err?.error?.error?.message || 'Đăng ký bằng Google không thành công.';
        this.cdr.detectChanges();
      }
    });
  }

  onStep2Submit(): void {
    if (this.step2Form.invalid) return;

    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    this.authProxy.registerStep2({
      registrationToken: this.registrationToken,
      fullName: this.step2Form.value.fullName
    }).subscribe({
      next: () => {
        this.isLoading = false;
        this.currentStep = 3;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err?.error?.error?.message || 'Cập nhật họ tên không thành công.';
        this.cdr.detectChanges();
      }
    });
  }

  onStep3Submit(): void {
    if (this.step3Form.invalid) return;

    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    this.authProxy.registerStep3({
      registrationToken: this.registrationToken,
      phoneNumber: this.step3Form.value.phoneNumber
    }).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.isSuccess) {
          if (res.accessToken) {
            this.handleSuccessfulAuth(res.accessToken, res.expiresIn);
          }
          this.currentStep = 4; // Hoàn tất
        } else {
          this.errorMessage = res.message || 'Hoàn tất đăng ký không thành công.';
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err?.error?.error?.message || 'Hoàn tất đăng ký không thành công.';
        this.cdr.detectChanges();
      }
    });
  }

  private handleSuccessfulAuth(accessToken: string, expiresIn?: number): void {
    this.oAuthStorage.setItem('access_token', accessToken);
    this.oAuthStorage.setItem('token_type', 'Bearer');
    if (expiresIn) {
      const expiresAt = Date.now() + expiresIn * 1000;
      this.oAuthStorage.setItem('expires_at', expiresAt.toString());
    }

    if (typeof localStorage !== 'undefined') {
      localStorage.setItem('access_token', accessToken);
    }

    this.configState.refreshAppState().subscribe();
  }
}

