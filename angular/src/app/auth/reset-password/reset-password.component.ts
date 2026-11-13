import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AuthService, LocalizationPipe } from '@abp/ng.core';
import { OAuthStorage } from 'angular-oauth2-oidc';
import { AuthProxyService } from '../services/auth-proxy.service';
import { passwordComplexityValidator, checkPasswordCriteria, PasswordCriteriaState } from '../validators/password-complexity.validator';

@Component({
  selector: 'app-auth-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, LocalizationPipe],
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.scss']
})
export class ResetPasswordComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authProxy = inject(AuthProxyService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private authService = inject(AuthService);
  private oAuthStorage = inject(OAuthStorage);
  private cdr = inject(ChangeDetectorRef);

  resetForm!: FormGroup;
  userId = '';
  resetToken = '';
  maskedEmail = '';
  isLoading = false;
  isSuccess = false;
  errorMessage = '';

  get passwordCriteria(): PasswordCriteriaState {
    return checkPasswordCriteria(this.resetForm?.get('newPassword')?.value || '');
  }

  ngOnInit(): void {
    // Nếu người dùng đã đăng nhập thành công trước đó thì buộc chuyển hướng về trang chủ
    const token = this.oAuthStorage.getItem('access_token');
    const hasAccessToken = !!token && token !== 'undefined' && token !== 'null';
    if (this.authService.isAuthenticated || hasAccessToken) {
      this.router.navigate(['/']);
      return;
    }

    this.route.queryParams.subscribe(params => {
      this.userId = params['userId'] || '';
      this.resetToken = params['token'] || '';
      const emailParam = params['email'] || '';
      if (emailParam) {
        this.maskedEmail = this.maskEmail(emailParam);
      } else if (this.userId) {
        this.fetchResetPasswordInfo(this.userId);
      }
    });

    this.resetForm = this.fb.group({
      newPassword: ['', [Validators.required, passwordComplexityValidator]],
      confirmNewPassword: ['', [Validators.required]]
    }, { validators: this.passwordMatchValidator });
  }

  maskEmail(email: string): string {
    if (!email) return '';
    const name = email.split('@')[0];
    const prefix = name.length >= 3 ? name.substring(0, 3) : name;
    return `${prefix}***`;
  }

  fetchResetPasswordInfo(userId: string): void {
    this.authProxy.getResetPasswordInfo(userId).subscribe({
      next: (res) => {
        if (res?.maskedEmail) {
          this.maskedEmail = res.maskedEmail;
        } else if (res?.email) {
          this.maskedEmail = this.maskEmail(res.email);
        }
        this.cdr.detectChanges();
      },
      error: () => {
        // Ignored, fallback silently
      }
    });
  }

  passwordMatchValidator(form: FormGroup) {
    const password = form.get('newPassword')?.value;
    const confirm = form.get('confirmNewPassword')?.value;
    return password === confirm ? null : { passwordMismatch: true };
  }

  onSubmit(): void {
    if (this.resetForm.invalid || !this.userId || !this.resetToken) {
      if (!this.userId || !this.resetToken) {
        this.errorMessage = 'Liên kết đặt lại mật khẩu không hợp lệ hoặc thiếu thông tin xác thực.';
      }
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    this.authProxy.resetPassword({
      userId: this.userId,
      resetToken: this.resetToken,
      newPassword: this.resetForm.value.newPassword,
      confirmNewPassword: this.resetForm.value.confirmNewPassword
    }).subscribe({
      next: () => {
        this.isLoading = false;
        this.isSuccess = true;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err?.error?.error?.message || 'Không thể đặt lại mật khẩu. Liên kết có thể đã hết hạn.';
        this.cdr.detectChanges();
      }
    });
  }
}

