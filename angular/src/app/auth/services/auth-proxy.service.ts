import { Injectable, inject } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

export interface LoginWithEmailDto {
  email: string;
  password?: string;
  rememberMe?: boolean;
}

export interface LoginResultDto {
  isSuccess: boolean;
  accessToken?: string;
  tokenType?: string;
  expiresIn?: number;
  isLockedOut: boolean;
  remainingLockoutSeconds?: number;
  remainingAttemptsInCycle?: number;
  isDeactivated: boolean;
  message?: string;
  requiresRegistration?: boolean;
  registrationToken?: string;
  email?: string;
  prefilledName?: string;
}

export interface RegisterStep1InputDto {
  email: string;
  password?: string;
  confirmPassword?: string;
}

export interface RegisterStep1ResultDto {
  registrationToken: string;
  email: string;
  prefilledName?: string;
  nextStep: string;
}

export interface RegisterStep2InputDto {
  registrationToken: string;
  fullName: string;
}

export interface RegisterStep3InputDto {
  registrationToken: string;
  phoneNumber: string;
}

export interface GoogleAuthInputDto {
  idToken: string;
}

export interface ForgotPasswordInputDto {
  email: string;
  captchaToken: string;
}

export interface ResetPasswordInputDto {
  userId: string;
  resetToken: string;
  newPassword?: string;
  confirmNewPassword?: string;
}

export interface ResetPasswordInfoDto {
  email: string;
  maskedEmail: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthProxyService {
  private restService = inject(RestService);
  private apiName = 'default';

  loginWithEmail(input: LoginWithEmailDto): Observable<LoginResultDto> {
    return this.restService.request<LoginWithEmailDto, LoginResultDto>({
      method: 'POST',
      url: '/api/app/auth/login-with-email',
      body: input,
    }, { apiName: this.apiName, skipHandleError: true });
  }

  loginWithGoogle(input: GoogleAuthInputDto): Observable<LoginResultDto> {
    return this.restService.request<GoogleAuthInputDto, LoginResultDto>({
      method: 'POST',
      url: '/api/app/auth/login-with-google',
      body: input,
    }, { apiName: this.apiName, skipHandleError: true });
  }

  registerStep1(input: RegisterStep1InputDto): Observable<RegisterStep1ResultDto> {
    return this.restService.request<RegisterStep1InputDto, RegisterStep1ResultDto>({
      method: 'POST',
      url: '/api/app/auth/register-step1',
      body: input,
    }, { apiName: this.apiName, skipHandleError: true });
  }

  registerGoogleInit(input: GoogleAuthInputDto): Observable<RegisterStep1ResultDto> {
    return this.restService.request<GoogleAuthInputDto, RegisterStep1ResultDto>({
      method: 'POST',
      url: '/api/app/auth/register-google-init',
      body: input,
    }, { apiName: this.apiName, skipHandleError: true });
  }

  registerStep2(input: RegisterStep2InputDto): Observable<void> {
    return this.restService.request<RegisterStep2InputDto, void>({
      method: 'POST',
      url: '/api/app/auth/register-step2',
      body: input,
    }, { apiName: this.apiName, skipHandleError: true });
  }

  registerStep3(input: RegisterStep3InputDto): Observable<LoginResultDto> {
    return this.restService.request<RegisterStep3InputDto, LoginResultDto>({
      method: 'POST',
      url: '/api/app/auth/register-step3',
      body: input,
    }, { apiName: this.apiName, skipHandleError: true });
  }

  sendPasswordResetCode(input: ForgotPasswordInputDto): Observable<void> {
    return this.restService.request<ForgotPasswordInputDto, void>({
      method: 'POST',
      url: '/api/app/auth/send-password-reset-code',
      body: input,
    }, { apiName: this.apiName, skipHandleError: true });
  }

  resetPassword(input: ResetPasswordInputDto): Observable<void> {
    return this.restService.request<ResetPasswordInputDto, void>({
      method: 'POST',
      url: '/api/app/auth/reset-password',
      body: input,
    }, { apiName: this.apiName, skipHandleError: true });
  }

  getResetPasswordInfo(userId: string): Observable<ResetPasswordInfoDto> {
    return this.restService.request<null, ResetPasswordInfoDto>({
      method: 'GET',
      url: `/api/app/auth/reset-password-info/${userId}`,
    }, { apiName: this.apiName, skipHandleError: true });
  }

  unlockUser(userId: string): Observable<void> {
    return this.restService.request<null, void>({
      method: 'POST',
      url: `/api/app/auth/unlock-user/${userId}`,
    }, { apiName: this.apiName, skipHandleError: true });
  }
}

