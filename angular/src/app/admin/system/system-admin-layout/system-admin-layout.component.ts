import { ChangeDetectionStrategy, Component, computed, inject, signal, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService, ConfigStateService, LocalizationPipe } from '@abp/ng.core';
import { BhLanguageSwitcherComponent } from '../../../shared/components/language-switcher/bh-language-switcher.component';
import { NavigationItem } from '../../../shared/models/ui-models';

@Component({
  selector: 'bh-system-admin-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, LocalizationPipe, BhLanguageSwitcherComponent],
  templateUrl: './system-admin-layout.component.html',
  styleUrls: ['./system-admin-layout.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SystemAdminLayoutComponent {
  private authService = inject(AuthService);
  private configState = inject(ConfigStateService);
  private router = inject(Router);

  isCollapsed = signal<boolean>(false);
  isUserMenuOpen = signal<boolean>(false);
  isMobileSidebarOpen = signal<boolean>(false);

  currentUser = computed(() => this.configState.getOne('currentUser'));
  currentUserName = computed(() => {
    const user = this.currentUser();
    if (!user) return '';
    return user.name ? `${user.surName || ''} ${user.name}`.trim() : (user.userName || '');
  });
  currentUserEmail = computed(() => this.currentUser()?.email || '');

  readonly menuItems: NavigationItem[] = [
    {
      route: '/admin/dashboard',
      labelKey: 'Menu:SystemDashboard',
      icon: 'fa-solid fa-gauge-high',
    },
    {
      route: '/admin/tenants',
      labelKey: 'Permission:Tenants',
      icon: 'fa-solid fa-building-user',
    },
    {
      route: '/admin/users',
      labelKey: 'Identity:Title',
      icon: 'fa-solid fa-users-gear',
    },
    {
      route: '/admin/roles',
      labelKey: 'Identity:RoleListTitle',
      icon: 'fa-solid fa-shield-halved',
    },
  ];

  get isAuthenticated(): boolean {
    return this.authService.isAuthenticated;
  }

  toggleSidebar(): void {
    this.isCollapsed.update(v => !v);
  }

  toggleMobileSidebar(): void {
    this.isMobileSidebarOpen.update(v => !v);
  }

  toggleUserMenu(): void {
    this.isUserMenuOpen.update(v => !v);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as Element | null;
    if (!target || !target.closest) return;
    if (!target.closest('.user-dropdown-container')) {
      this.isUserMenuOpen.set(false);
    }
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.isUserMenuOpen.set(false);
  }

  logout(): void {
    this.authService.logout().subscribe();
  }

  navigateTo(route: string): void {
    this.isMobileSidebarOpen.set(false);
    this.router.navigateByUrl(route);
  }
}

