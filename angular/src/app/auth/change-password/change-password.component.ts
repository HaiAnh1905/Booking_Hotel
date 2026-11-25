import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ConfigStateService, LocalizationPipe, LocalizationService } from '@abp/ng.core';
import { OAuthStorage } from 'angular-oauth2-oidc';
import { AuthProxyService } from '../services/auth-proxy.service';
import { passwordComplexityValidator, checkPasswordCriteria, PasswordCriteriaState } from '../validators/password-complexity.validator';
import { getDefaultUrlForRoles, getUserRoles } from '../../admin/guards/role-based-redirect.guard';

@Component({
  selector: 'bh-change-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, LocalizationPipe],
  templateUrl: './change-password.component.html',
  styleUrls: ['./change-password.component.scss']
})
export class ChangePasswordComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authProxy = inject(AuthProxyService);
  private router = inject(Router);
  private oAuthStorage = inject(OAuthStorage);
  private configState = inject(ConfigStateService);
  private localization = inject(LocalizationService);
  private cdr = inject(ChangeDetectorRef);

  form!: FormGroup;
  isLoading = false;
  isSuccess = false;
  errorMessage = '';

  get passwordCriteria(): PasswordCriteriaState {
    return checkPasswordCriteria(this.form?.get('newPassword')?.value || '');
  }

  ngOnInit(): void {
    this.form = this.fb.group({
      currentPassword: ['', [Validators.required]],
      newPassword: ['', [Validators.required, Validators.minLength(8), passwordComplexityValidator]],
      confirmNewPassword: ['', [Validators.required]]
    }, {
      validators: (group: FormGroup) => {
        const newPass = group.get('newPassword')?.value;
        const confirmPass = group.get('confirmNewPassword')?.value;
        return newPass === confirmPass ? null : { passwordMismatch: true };
      }
    });
  }

  onSubmit(): void {
    if (this.form.invalid || this.isLoading) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const input = {
      currentPassword: this.form.value.currentPassword,
      newPassword: this.form.value.newPassword,
      confirmNewPassword: this.form.value.confirmNewPassword
    };

    this.authProxy.changePassword(input).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.isSuccess = true;
        if (res.accessToken) {
          this.oAuthStorage.setItem('access_token', res.accessToken);
          if (typeof localStorage !== 'undefined') {
            localStorage.setItem('access_token', res.accessToken);
          }
        }
        this.cdr.detectChanges();

        setTimeout(() => {
          const roles = getUserRoles(this.oAuthStorage, this.configState);
          const targetUrl = getDefaultUrlForRoles(roles);
          this.router.navigateByUrl(targetUrl);
        }, 1500);
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err?.error?.error?.message || err?.message || this.localization.instant('Auth:ChangePasswordFailed');
        this.cdr.detectChanges();
      }
    });
  }
}

