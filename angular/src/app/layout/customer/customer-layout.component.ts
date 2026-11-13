import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService, LocalizationPipe } from '@abp/ng.core';
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

  currentYear = new Date().getFullYear();
  isMobileMenuOpen = signal<boolean>(false);
  isUserMenuOpen = signal<boolean>(false);

  get isAuthenticated(): boolean {
    return this.authService.isAuthenticated;
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
