import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe } from '@abp/ng.core';

export interface SystemRoleDefinition {
  name: string;
  nameKey: string;
  descriptionKey: string;
  icon: string;
  scope: 'Host' | 'Tenant' | 'Both';
  badgeClass: string;
  isStatic: boolean;
  permissionsCount: number;
}

@Component({
  selector: 'bh-role-list',
  standalone: true,
  imports: [CommonModule, LocalizationPipe],
  templateUrl: './role-list.component.html',
  styleUrls: ['./role-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RoleListComponent {
  readonly standardRoles: SystemRoleDefinition[] = [
    {
      name: 'SystemAdmin',
      nameKey: 'Role:SystemAdmin',
      descriptionKey: 'Identity:DescSystemAdmin',
      icon: 'fa-solid fa-crown',
      scope: 'Host',
      badgeClass: 'badge-host',
      isStatic: true,
      permissionsCount: 48
    },
    {
      name: 'TenantAdmin',
      nameKey: 'Role:TenantAdmin',
      descriptionKey: 'Identity:DescTenantAdmin',
      icon: 'fa-solid fa-building-user',
      scope: 'Tenant',
      badgeClass: 'badge-tenant',
      isStatic: true,
      permissionsCount: 36
    },
    {
      name: 'HotelManager',
      nameKey: 'Role:HotelManager',
      descriptionKey: 'Identity:DescHotelManager',
      icon: 'fa-solid fa-user-tie',
      scope: 'Tenant',
      badgeClass: 'badge-manager',
      isStatic: true,
      permissionsCount: 22
    },
    {
      name: 'Receptionist',
      nameKey: 'Role:Receptionist',
      descriptionKey: 'Identity:DescReceptionist',
      icon: 'fa-solid fa-bell-concierge',
      scope: 'Tenant',
      badgeClass: 'badge-receptionist',
      isStatic: true,
      permissionsCount: 16
    },
    {
      name: 'Customer',
      nameKey: 'Role:Customer',
      descriptionKey: 'Identity:DescCustomer',
      icon: 'fa-solid fa-user',
      scope: 'Both',
      badgeClass: 'badge-customer',
      isStatic: true,
      permissionsCount: 8
    }
  ];
}
