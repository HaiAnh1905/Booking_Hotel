import { AfterViewInit, Component, OnDestroy, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AuthService, ConfigStateService, LocalizationPipe } from '@abp/ng.core';
import { OAuthStorage } from 'angular-oauth2-oidc';
import { environment } from '../../../environments/environment';
import { AuthProxyService, LoginResultDto } from '../services/auth-proxy.service';

declare const google: any;

@Component({
  selector: 'app-auth-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, LocalizationPipe],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent implements OnInit, AfterViewInit, OnDestroy {
  private fb = inject(FormBuilder);
  private authProxy = inject(AuthProxyService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private oAuthStorage = inject(OAuthStorage);
  private configState = inject(ConfigStateService);
  private authService = inject(AuthService);
  private cdr = inject(ChangeDetectorRef);

  loginForm!: FormGroup;
  isLoading = false;
  isGoogleLoaded = false;
  errorMessage = '';
  warningMessage = '';
  isLockedOut = false;
  lockoutRemainingSeconds = 0;
  isDeactivated = false;
  private timerSubscription: any;
  private readonly REMEMBER_ME_STORAGE_KEY = 'booking_hotel_remember_me';
  private readonly MAX_SESSION_DAYS = 15;

  // Thuộc tính chặn autofill của trình duyệt (Chrome/Edge)
  isEmailReadonly = true;
  isPasswordReadonly = true;
  private isRemembered = false;

  ngOnInit(): void {
    // Nếu người dùng đã đăng nhập thành công trước đó thì buộc chuyển hướng về trang chủ
    const token = this.oAuthStorage.getItem('access_token');
    const hasAccessToken = !!token && token !== 'undefined' && token !== 'null';
    if (this.authService.isAuthenticated || hasAccessToken) {
      this.router.navigate(['/']);
      return;
    }

    let initialEmail = '';
    let initialRememberMe = false;

    // Hiến pháp Điều IX: Tuyệt đối không hiển thị thông tin đăng nhập của bất kỳ tài khoản nào (kể cả admin).
    // Chỉ hiển thị email nếu người dùng đã nhấn Remember Me và session còn trong thời hạn tối đa 15 ngày.
    const rememberedDataStr = localStorage.getItem(this.REMEMBER_ME_STORAGE_KEY);
    if (rememberedDataStr) {
      try {
        const remembered = JSON.parse(rememberedDataStr);
        const maxAgeMs = this.MAX_SESSION_DAYS * 24 * 60 * 60 * 1000;
        if (remembered?.email && remembered?.savedAt && Date.now() - remembered.savedAt <= maxAgeMs) {
          initialEmail = remembered.email;
          initialRememberMe = true;
          this.isRemembered = true;
          this.isEmailReadonly = false;
        } else {
          localStorage.removeItem(this.REMEMBER_ME_STORAGE_KEY);
        }
      } catch {
        localStorage.removeItem(this.REMEMBER_ME_STORAGE_KEY);
      }
    }

    this.loginForm = this.fb.group({
      email: [initialEmail, [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      rememberMe: [initialRememberMe]
    });
  }

  ngAfterViewInit(): void {
    // Ngăn chặn hoàn toàn hành vi tự động điền (autofill) của trình duyệt đối với mật khẩu và tài khoản đã lưu
    if (!this.isRemembered) {
      setTimeout(() => {
        const emailEl = document.getElementById('email') as HTMLInputElement;
        const passEl = document.getElementById('password') as HTMLInputElement;
        if (emailEl) emailEl.value = '';
        if (passEl) passEl.value = '';
        this.loginForm.patchValue({ email: '', password: '' });
      }, 50);
    }

    this.initGoogleSignIn();
  }

  ngOnDestroy(): void {
    this.clearLockoutTimer();
  }

  onSubmit(): void {
    if (this.loginForm.invalid || this.isLockedOut || this.isDeactivated) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.warningMessage = '';
    this.cdr.detectChanges();

    this.authProxy.loginWithEmail(this.loginForm.value).subscribe({
      next: (res: LoginResultDto) => {
        this.isLoading = false;
        if (res.isSuccess && res.accessToken) {
          this.handleSuccessfulAuth(res.accessToken, res.expiresIn);
          this.cdr.detectChanges();
          return;
        }

        if (res.isDeactivated) {
          this.isDeactivated = true;
          this.errorMessage = res.message || 'Tài khoản đã bị vô hiệu hóa.';
          this.cdr.detectChanges();
          return;
        }

        if (res.isLockedOut && res.remainingLockoutSeconds) {
          this.isLockedOut = true;
          this.startLockoutCountdown(res.remainingLockoutSeconds);
          this.errorMessage = res.message || 'Tài khoản đang bị tạm khóa.';
          this.cdr.detectChanges();
          return;
        }

        if (res.remainingAttemptsInCycle !== undefined && res.remainingAttemptsInCycle !== null) {
          this.warningMessage = res.message || `Mật khẩu không chính xác. Bạn còn ${res.remainingAttemptsInCycle} lần thử.`;
        } else {
          this.errorMessage = res.message || 'Đăng nhập không thành công.';
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err?.error?.error?.message || 'Có lỗi xảy ra trong quá trình đăng nhập.';
        this.cdr.detectChanges();
      }
    });
  }

  private initGoogleSignIn(): void {
    if (typeof window === 'undefined') return;
    const clientId = environment.googleClientId;
    if (!clientId) return;

    let attempts = 0;
    const checkGsi = () => {
      if (typeof google !== 'undefined' && google.accounts?.id) {
        google.accounts.id.initialize({
          client_id: clientId,
          callback: (res: any) => this.onGoogleCredential(res?.credential)
        });

        const btnEl = document.getElementById('googleBtnLogin');
        if (btnEl) {
          google.accounts.id.renderButton(btnEl, {
            theme: 'outline',
            size: 'large',
            width: 320,
            text: 'signin_with',
            shape: 'rectangular',
            logo_alignment: 'left'
          });
          this.isGoogleLoaded = true;
          this.cdr.detectChanges();
        }
        google.accounts.id.prompt();
      } else if (attempts < 25) {
        attempts++;
        setTimeout(checkGsi, 200);
      }
    };
    checkGsi();
  }

  onGoogleCredential(idToken: string): void {
    if (!idToken) return;

    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    this.authProxy.loginWithGoogle({ idToken }).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.isSuccess && res.accessToken) {
          this.handleSuccessfulAuth(res.accessToken, res.expiresIn);
        } else if (res.requiresRegistration || res.message === 'UserNotRegistered') {
          if (res.registrationToken) {
            this.router.navigate(['/auth/register'], {
              queryParams: {
                token: res.registrationToken,
                email: res.email,
                name: res.prefilledName,
                fromGoogle: 'true'
              }
            });
          } else {
            this.router.navigate(['/auth/register'], {
              queryParams: {
                googleToken: idToken,
                fromGoogle: 'true'
              }
            });
          }
        } else {
          this.errorMessage = res.message || 'Đăng nhập Google thất bại.';
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err?.error?.error?.message || 'Đăng nhập Google thất bại.';
        this.cdr.detectChanges();
      }
    });
  }

  handleGoogleSignIn(): void {
    if (typeof google !== 'undefined' && google.accounts?.id) {
      google.accounts.id.prompt();
      return;
    }

    // Fallback demo account nếu Google SDK chưa tải
    this.onGoogleCredential('demo.google.user@hotel.com');
  }

  private handleSuccessfulAuth(accessToken: string, expiresIn?: number): void {
    // Lưu hoặc xóa thông tin Remember Me tuân thủ Hiến pháp Điều IX (Session tối đa 15 ngày)
    if (this.loginForm.get('rememberMe')?.value) {
      const rememberData = {
        email: this.loginForm.get('email')?.value,
        savedAt: Date.now()
      };
      localStorage.setItem(this.REMEMBER_ME_STORAGE_KEY, JSON.stringify(rememberData));
    } else {
      localStorage.removeItem(this.REMEMBER_ME_STORAGE_KEY);
    }

    // 1. Lưu token vào OAuthStorage và localStorage đồng thời
    this.oAuthStorage.setItem('access_token', accessToken);
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem('access_token', accessToken);
    }
    if (expiresIn) {
      const expiresAt = '' + (Date.now() + expiresIn * 1000);
      this.oAuthStorage.setItem('expires_at', expiresAt);
      if (typeof localStorage !== 'undefined') {
        localStorage.setItem('expires_at', expiresAt);
      }
    }

    // 2. Kích hoạt cập nhật ứng dụng tức thời (User claims, avatar, permissions) mà không reload trang
    const returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';
    this.configState.refreshAppState().subscribe({
      next: () => {
        this.router.navigateByUrl(returnUrl);
      },
      error: () => {
        this.router.navigateByUrl(returnUrl);
      }
    });
  }

  private startLockoutCountdown(seconds: number): void {
    this.lockoutRemainingSeconds = seconds;
    this.clearLockoutTimer();
    this.timerSubscription = setInterval(() => {
      this.lockoutRemainingSeconds--;
      if (this.lockoutRemainingSeconds <= 0) {
        this.isLockedOut = false;
        this.errorMessage = '';
        this.clearLockoutTimer();
      }
    }, 1000);
  }

  private clearLockoutTimer(): void {
    if (this.timerSubscription) {
      clearInterval(this.timerSubscription);
      this.timerSubscription = null;
    }
  }

  get formatRemainingTime(): string {
    const mins = Math.floor(this.lockoutRemainingSeconds / 60);
    const secs = this.lockoutRemainingSeconds % 60;
    return `${mins}:${secs < 10 ? '0' : ''}${secs}`;
  }
}

