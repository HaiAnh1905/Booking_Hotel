import { Injectable, inject } from '@angular/core';
import { RestService, PagedResultDto } from '@abp/ng.core';
import { Observable } from 'rxjs';

export interface AssignedHotelDto {
  hotelId: string;
  hotelName: string;
  hotelCode: string;
  roleName: string;
  isPrimary: boolean;
  isActive: boolean;
}

export interface UserRoleDto {
  roleId: string;
  roleName: string;
}

export interface UserWithAssignmentDto {
  id: string;
  userName: string;
  name?: string;
  surname?: string;
  email: string;
  phoneNumber?: string;
  isActive: boolean;
  isLockedOut: boolean;
  lockoutEnd?: string;
  shouldChangePasswordOnNextLogin: boolean;
  roleNames: string[];
  roles?: UserRoleDto[];
  assignedHotels: AssignedHotelDto[];
  creationTime: string;
  concurrencyStamp?: string;
}

export interface CreateTenantStaffDto {
  userName: string;
  email: string;
  name?: string;
  surname?: string;
  phoneNumber?: string;
  password?: string;
  roles: string[];
  managerHotelIds?: string[];
  primaryReceptionistHotelId?: string;
}

export interface UpdateTenantStaffDto {
  email: string;
  name?: string;
  surname?: string;
  phoneNumber?: string;
  password?: string;
  roles: string[];
  managerHotelIds?: string[];
  primaryReceptionistHotelId?: string;
  concurrencyStamp?: string;
}

export interface TransferBranchDto {
  fromHotelId: string;
  toHotelId: string;
  roleName: string;
  note?: string;
}

export interface UnlockUserResultDto {
  success: boolean;
  message: string;
  unlockedAt: string;
}

export interface GetUsersWithAssignmentInput {
  filter?: string;
  roleName?: string;
  hotelId?: string;
  skipCount?: number;
  maxResultCount?: number;
}

@Injectable({
  providedIn: 'root'
})
export class TenantStaffService {
  private restService = inject(RestService);

  getList(input?: GetUsersWithAssignmentInput): Observable<PagedResultDto<UserWithAssignmentDto>> {
    return this.restService.request<GetUsersWithAssignmentInput, PagedResultDto<UserWithAssignmentDto>>({
      method: 'GET',
      url: '/api/app/identity-user-management',
      params: input
    });
  }

  get(id: string): Observable<UserWithAssignmentDto> {
    return this.restService.request<void, UserWithAssignmentDto>({
      method: 'GET',
      url: `/api/app/identity-user-management/${id}`
    });
  }

  createTenantStaff(input: CreateTenantStaffDto): Observable<UserWithAssignmentDto> {
    return this.restService.request<CreateTenantStaffDto, UserWithAssignmentDto>({
      method: 'POST',
      url: '/api/app/identity-user-management/tenant-staff',
      body: input
    });
  }

  updateTenantStaff(id: string, input: UpdateTenantStaffDto): Observable<UserWithAssignmentDto> {
    return this.restService.request<UpdateTenantStaffDto, UserWithAssignmentDto>({
      method: 'PUT',
      url: `/api/app/identity-user-management/tenant-staff/${id}`,
      body: input
    });
  }

  transferBranch(id: string, input: TransferBranchDto): Observable<void> {
    return this.restService.request<TransferBranchDto, void>({
      method: 'POST',
      url: `/api/app/identity-user-management/${id}/transfer-branch`,
      body: input
    });
  }

  unlockBruteForce(id: string): Observable<UnlockUserResultDto> {
    return this.restService.request<void, UnlockUserResultDto>({
      method: 'POST',
      url: `/api/app/identity-user-management/${id}/unlock`
    });
  }

  toggleStatus(id: string): Observable<UserWithAssignmentDto> {
    return this.restService.request<void, UserWithAssignmentDto>({
      method: 'POST',
      url: `/api/app/identity-user-management/${id}/toggle-status`
    });
  }

  delete(id: string): Observable<void> {
    return this.restService.request<void, void>({
      method: 'DELETE',
      url: `/api/app/identity-user-management/${id}`
    });
  }

  getAssignableRoles(): Observable<string[]> {
    return this.restService.request<void, string[]>({
      method: 'GET',
      url: '/api/app/identity-user-management/assignable-roles'
    });
  }
}
