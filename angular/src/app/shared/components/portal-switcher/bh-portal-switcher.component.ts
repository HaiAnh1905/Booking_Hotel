import { ChangeDetectionStrategy, Component, Input, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ConfigStateService, LocalizationPipe } from '@abp/ng.core';
import { OAuthStorage } from 'angular-oauth2-oidc';
import { getUserRoles } from '../../../admin/guards/role-based-redirect.guard';

@Component({
  selector: 'bh-portal-switcher',
  standalone: true,
  imports: [CommonModule, LocalizationPipe],
  template: `
    <div class="portal-switcher-wrap" *ngIf="isDualRole()">
      <button 
        *ngIf="currentPortal === 'manager'" 
        type="button" 
        class="switcher-pill to-frontdesk" 
        (click)="switchPortal('/frontdesk/room-rack')"
        [title]="'Switcher:ToFrontdeskTooltip' | abpLocalization">
        <i class="fa-solid fa-bell-concierge"></i>
        <span>{{ 'Switcher:ToFrontdesk' | abpLocalization }}</span>
      </button>

      <button 
        *ngIf="currentPortal === 'frontdesk'" 
        type="button" 
        class="switcher-pill to-manager" 
        (click)="switchPortal('/manager/dashboard')"
        [title]="'Switcher:ToManagerTooltip' | abpLocalization">
        <span>{{ 'Switcher:ToManager' | abpLocalization }}</span>
      </button>
    </div>
  `,
  styles: [`
    .portal-switcher-wrap {
      display: inline-flex;
      align-items: center;

      .switcher-pill {
        display: inline-flex;
        align-items: center;
        gap: 8px;
        padding: 6px 14px;
        border-radius: 9999px;
        font-size: 0.825rem;
        font-weight: 700;
        cursor: pointer;
        transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
        text-decoration: none;

        &.to-frontdesk {
          background-color: #99ff99;
          color: #000000;
          border: 1px solid #4ade80;
          box-shadow: 0 2px 4px rgba(34, 197, 94, 0.2);

          &:hover {
            background-color: #86efac;
            transform: translateY(-1px);
            box-shadow: 0 4px 6px rgba(34, 197, 94, 0.3);
          }
        }

        &.to-manager {
          background-color: #000000;
          color: #00ffff;
          border: 1px solid #00ffff;
          box-shadow: 0 2px 4px rgba(0, 255, 255, 0.15);

          &:hover {
            background-color: #111827;
            color: #ffffff;
            transform: translateY(-1px);
            box-shadow: 0 4px 6px rgba(0, 255, 255, 0.25);
          }
        }
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BhPortalSwitcherComponent {
  @Input() currentPortal: 'manager' | 'frontdesk' = 'manager';

  private router = inject(Router);
  private oAuthStorage = inject(OAuthStorage);
  private configState = inject(ConfigStateService);

  readonly isDualRole = computed(() => {
    const roles = getUserRoles(this.oAuthStorage, this.configState);
    const hasManager = roles.includes('HotelManager') || roles.includes('TenantAdmin') || roles.includes('SystemAdmin') || roles.includes('admin');
    const hasReceptionist = roles.includes('Receptionist') || roles.includes('TenantAdmin') || roles.includes('SystemAdmin') || roles.includes('admin');
    return hasManager && hasReceptionist;
  });

  switchPortal(targetUrl: string): void {
    this.router.navigateByUrl(targetUrl);
  }
}

