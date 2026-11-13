import { AfterViewInit, Component, OnDestroy, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService, LocalizationPipe } from '@abp/ng.core';
import { OAuthStorage } from 'angular-oauth2-oidc';
import { environment } from '../../../environments/environment';
import { AuthProxyService } from '../services/auth-proxy.service';

declare const turnstile: any;

@Component({
  selector: 'app-auth-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, LocalizationPipe],
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.scss']
})
export class ForgotPasswordComponent implements OnInit, AfterViewInit, OnDestroy {
  private fb = inject(FormBuilder);
  private authProxy = inject(AuthProxyService);
  private router = inject(Router);
  private authService = inject(AuthService);
  private oAuthStorage = inject(OAuthStorage);
  private cdr = inject(ChangeDetectorRef);

  forgotForm!: FormGroup;
  isLoading = false;
  isSubmitted = false;
  errorMessage = '';
  captchaVerified = false;
  captchaToken = '';
  private widgetId: string | null = null;

  // Quản lý đếm ngược gửi lại email (30s)
  resendCountdown = 30;
  canResend = false;
  isResending = false;
  resendSuccessMessage = false;
  private countdownTimer: any = null;

  ngOnInit(): void {
    // Nếu người dùng đã đăng nhập thành công trước đó thì buộc chuyển hướng về trang chủ
    const token = this.oAuthStorage.getItem('access_token');
    const hasAccessToken = !!token && token !== 'undefined' && token !== 'null';
    if (this.authService.isAuthenticated || hasAccessToken) {
      this.router.navigate(['/']);
      return;
    }

    this.forgotForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]]
    });
  }

  ngAfterViewInit(): void {
    this.initTurnstile();
  }

  ngOnDestroy(): void {
    if (this.countdownTimer) {
      clearInterval(this.countdownTimer);
      this.countdownTimer = null;
    }

    if (typeof turnstile !== 'undefined' && this.widgetId) {
      try {
        turnstile.remove(this.widgetId);
      } catch {
        // Ignored
      }
    }
  }

  private initTurnstile(): void {
    if (typeof window === 'undefined') return;

    const siteKey = (environment as any).turnstileSiteKey || '1x00000000000000000000AA';
    let attempts = 0;

    const checkAndRender = () => {
      if (typeof turnstile !== 'undefined' && turnstile.render) {
        const container = document.getElementById('turnstileWidget');
        if (container && !this.widgetId) {
          try {
            this.widgetId = turnstile.render(container, {
              sitekey: siteKey,
              theme: 'light',
              callback: (token: string) => {
                this.captchaToken = token;
                this.captchaVerified = true;
                this.cdr.detectChanges();
              },
              'expired-callback': () => {
                this.captchaToken = '';
                this.captchaVerified = false;
                this.cdr.detectChanges();
              },
              'error-callback': () => {
                // Fallback token dev khi offline hoặc Turnstile error
                this.captchaToken = 'MOCK_TURNSTILE_TOKEN_VALID';
                this.captchaVerified = true;
                this.cdr.detectChanges();
              }
            });
          } catch (e) {
            console.warn('Turnstile render warning:', e);
            this.captchaToken = 'MOCK_TURNSTILE_TOKEN_VALID';
            this.captchaVerified = true;
            this.cdr.detectChanges();
          }
        }
      } else if (attempts < 25) {
        attempts++;
        setTimeout(checkAndRender, 200);
      } else {
        // Fallback dev mode nếu script không tải được
        this.captchaToken = 'MOCK_TURNSTILE_TOKEN_VALID';
        this.captchaVerified = true;
        this.cdr.detectChanges();
      }
    };

    checkAndRender();
  }

  onSubmit(): void {
    if (this.forgotForm.invalid || !this.captchaVerified) return;

    this.isLoading = true;
    this.errorMessage = '';
    this.resendSuccessMessage = false;
    this.cdr.detectChanges();

    this.authProxy.sendPasswordResetCode({
      email: this.forgotForm.value.email,
      captchaToken: this.captchaToken
    }).subscribe({
      next: () => {
        this.isLoading = false;
        this.isSubmitted = true;
        this.startResendCountdown(30);
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err?.error?.error?.message || 'Không thể gửi yêu cầu đặt lại mật khẩu. Vui lòng thử lại.';
        this.cdr.detectChanges();
      }
    });
  }

  startResendCountdown(seconds: number = 30): void {
    this.resendCountdown = seconds;
    this.canResend = false;
    if (this.countdownTimer) {
      clearInterval(this.countdownTimer);
    }
    this.countdownTimer = setInterval(() => {
      if (this.resendCountdown > 1) {
        this.resendCountdown--;
        this.cdr.detectChanges();
      } else {
        this.resendCountdown = 0;
        this.canResend = true;
        clearInterval(this.countdownTimer);
        this.countdownTimer = null;
        this.cdr.detectChanges();
      }
    }, 1000);
  }

  onResend(): void {
    if (!this.canResend || this.isResending) return;

    this.isResending = true;
    this.errorMessage = '';
    this.resendSuccessMessage = false;
    this.cdr.detectChanges();

    this.authProxy.sendPasswordResetCode({
      email: this.forgotForm.value.email,
      captchaToken: this.captchaToken || 'MOCK_TURNSTILE_TOKEN_VALID'
    }).subscribe({
      next: () => {
        this.isResending = false;
        this.resendSuccessMessage = true;
        this.startResendCountdown(30);
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isResending = false;
        this.errorMessage = err?.error?.error?.message || 'Không thể gửi lại email. Vui lòng thử lại sau.';
        this.cdr.detectChanges();
      }
    });
  }

  changeEmail(): void {
    this.isSubmitted = false;
    this.resendSuccessMessage = false;
    this.errorMessage = '';
    if (this.countdownTimer) {
      clearInterval(this.countdownTimer);
      this.countdownTimer = null;
    }
    this.cdr.detectChanges();
    setTimeout(() => this.initTurnstile(), 50);
  }
}

