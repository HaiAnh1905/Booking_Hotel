import { Injectable, inject } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

export interface TenantOrganizationProfileDto {
  id: string;
  tenantId?: string;
  organizationName: string;
  logoUrl?: string;
  headquarterAddress?: string;
  hotline?: string;
  contactEmail?: string;
  websiteUrl?: string;
  defaultCheckInTime: string;
  defaultCheckOutTime: string;
  cancellationPolicyJson?: string;
}

export interface UpdateTenantOrganizationDto {
  organizationName: string;
  logoUrl?: string;
  headquarterAddress?: string;
  hotline?: string;
  contactEmail?: string;
  websiteUrl?: string;
  defaultCheckInTime: string;
  defaultCheckOutTime: string;
  cancellationPolicyJson?: string;
}

@Injectable({
  providedIn: 'root'
})
export class TenantOrgService {
  private restService = inject(RestService);

  getProfile(): Observable<TenantOrganizationProfileDto> {
    return this.restService.request<void, TenantOrganizationProfileDto>({
      method: 'GET',
      url: '/api/app/tenant-organization/profile'
    });
  }

  updateProfile(input: UpdateTenantOrganizationDto): Observable<TenantOrganizationProfileDto> {
    return this.restService.request<UpdateTenantOrganizationDto, TenantOrganizationProfileDto>({
      method: 'PUT',
      url: '/api/app/tenant-organization/profile',
      body: input
    });
  }
}
