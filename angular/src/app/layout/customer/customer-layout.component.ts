import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService, ConfigStateService, LocalizationPipe } from '@abp/ng.core';
import { OAuthStorage } from 'angular-oauth2-oidc';
import { getDefaultUrlForRoles, getUserRoles } from '../../admin/guards/role-based-redirect.guard';
import { BhLanguageSwitcherComponent } from '../../shared/components/language-switcher/bh-language-switcher.component';

@Component({
  selector: 'bh-customer-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, LocalizationPipe, BhLanguageSwitcherComponent],
  templateUrl: './customer-layout.component.html',
  styleUrls: ['./customer-layout.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CustomerLayoutComponent {
  private authService = inject(AuthService);
  private oAuthStorage = inject(OAuthStorage);
  private configState = inject(ConfigStateService);

  currentYear = new Date().getFullYear();
  isMobileMenuOpen = signal<boolean>(false);
  isUserMenuOpen = signal<boolean>(false);

  get isAuthenticated(): boolean {
    return this.authService.isAuthenticated;
  }

  get currentUser(): any {
    return this.configState.getOne('currentUser');
  }

  get userDisplayName(): string {
    const user = this.currentUser;
    return user?.name || user?.userName || user?.email || '';
  }

  get userRoles(): string[] {
    if (!this.isAuthenticated) return [];
    return getUserRoles(this.oAuthStorage, this.configState);
  }

  /**
   * Chỉ cho phép truy cập mục Quản trị khi người dùng đăng nhập và có vai trò quản lý (không phải Customer thuần túy)
   */
  get hasManagementRole(): boolean {
    if (!this.isAuthenticated) return false;
    const roles = this.userRoles;
    return roles.some(r =>
      ['SystemAdmin', 'admin', 'TenantAdmin', 'HotelManager', 'Receptionist'].includes(r)
    );
  }

  /**
   * Phân luồng chính xác về Dashboard phù hợp với vai trò của nhân sự
   */
  get managementPortalUrl(): string {
    return getDefaultUrlForRoles(this.userRoles);
  }

  toggleMobileMenu() {
    this.isMobileMenuOpen.update(v => !v);
  }

  toggleUserMenu() {
    this.isUserMenuOpen.update(v => !v);
  }

  login() {
    this.authService.navigateToLogin();
  }

  logout() {
    this.authService.logout().subscribe();
  }
}
