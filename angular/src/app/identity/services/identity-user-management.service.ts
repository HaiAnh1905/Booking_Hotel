import { Injectable, inject } from '@angular/core';
import { RestService, PagedResultDto } from '@abp/ng.core';
import { Observable } from 'rxjs';

export interface AssignedHotelDto {
  hotelId: string;
  hotelName: string;
  hotelCode: string;
  isPrimary: boolean;
  isActive: boolean;
  assignedAt: string;
  unassignedAt?: string | null;
}

export interface UserRoleDto {
  id?: string;
  name: string;
}

export interface UserWithAssignmentDto {
  id: string;
  userName: string;
  name?: string | null;
  surname?: string | null;
  email: string;
  phoneNumber?: string | null;
  isActive: boolean;
  lockoutEnd?: string | null;
  isLockedOut: boolean;
  shouldChangePasswordOnNextLogin: boolean;
  roles: UserRoleDto[];
  roleNames: string[];
  assignedHotels: AssignedHotelDto[];
  creationTime: string;
  concurrencyStamp?: string | null;
}

export interface CreateUserWithAssignmentDto {
  userName: string;
  name?: string | null;
  surname?: string | null;
  email: string;
  phoneNumber?: string | null;
  password?: string;
  roleNames: string[];
  hotelIds: string[];
  primaryHotelId?: string | null;
}

export interface UpdateUserWithAssignmentDto {
  userName?: string | null;
  name?: string | null;
  surname?: string | null;
  email: string;
  phoneNumber?: string | null;
  roleNames: string[];
  hotelIds: string[];
  primaryHotelId?: string | null;
  concurrencyStamp?: string | null;
  isActive?: boolean | null;
}

export interface UnlockUserDto {
  reason?: string | null;
}

export interface UnlockUserResultDto {
  success: boolean;
  userId: string;
  unlockedAt: string;
  message: string;
}

export interface GetUsersWithAssignmentInput {
  filter?: string;
  roleName?: string;
  hotelId?: string;
  isActive?: boolean;
  isLockedOut?: boolean;
  sorting?: string;
  skipCount?: number;
  maxResultCount?: number;
}

@Injectable({
  providedIn: 'root'
})
export class IdentityUserManagementService {
  private restService = inject(RestService);
  private apiName = 'default';

  getList(input: GetUsersWithAssignmentInput): Observable<PagedResultDto<UserWithAssignmentDto>> {
    return this.restService.request<GetUsersWithAssignmentInput, PagedResultDto<UserWithAssignmentDto>>({
      method: 'GET',
      url: '/api/app/identity-user-management',
      params: {
        filter: input.filter,
        roleName: input.roleName,
        hotelId: input.hotelId,
        isActive: input.isActive,
        isLockedOut: input.isLockedOut,
        sorting: input.sorting,
        skipCount: input.skipCount,
        maxResultCount: input.maxResultCount
      }
    }, { apiName: this.apiName, skipHandleError: true });
  }

  get(id: string): Observable<UserWithAssignmentDto> {
    return this.restService.request<void, UserWithAssignmentDto>({
      method: 'GET',
      url: `/api/app/identity-user-management/${id}`
    }, { apiName: this.apiName });
  }

  create(input: CreateUserWithAssignmentDto): Observable<UserWithAssignmentDto> {
    return this.restService.request<CreateUserWithAssignmentDto, UserWithAssignmentDto>({
      method: 'POST',
      url: '/api/app/identity-user-management',
      body: input
    }, { apiName: this.apiName });
  }

  update(id: string, input: UpdateUserWithAssignmentDto): Observable<UserWithAssignmentDto> {
    return this.restService.request<UpdateUserWithAssignmentDto, UserWithAssignmentDto>({
      method: 'PUT',
      url: `/api/app/identity-user-management/${id}`,
      body: input
    }, { apiName: this.apiName });
  }

  delete(id: string): Observable<void> {
    return this.restService.request<void, void>({
      method: 'DELETE',
      url: `/api/app/identity-user-management/${id}`
    }, { apiName: this.apiName });
  }

  unlockUser(id: string, input?: UnlockUserDto): Observable<UnlockUserResultDto> {
    return this.restService.request<UnlockUserDto | undefined, UnlockUserResultDto>({
      method: 'POST',
      url: `/api/app/identity-user-management/${id}/unlock`,
      body: input
    }, { apiName: this.apiName });
  }

  toggleStatus(id: string): Observable<UserWithAssignmentDto> {
    return this.restService.request<void, UserWithAssignmentDto>({
      method: 'POST',
      url: `/api/app/identity-user-management/${id}/toggle-status`
    }, { apiName: this.apiName });
  }

  getAssignableRoles(): Observable<string[]> {
    return this.restService.request<void, string[]>({
      method: 'GET',
      url: '/api/app/identity-user-management/assignable-roles'
    }, { apiName: this.apiName, skipHandleError: true });
  }

  getStaffByHotels(hotelIds?: string[]): Observable<UserWithAssignmentDto[]> {
    return this.restService.request<void, UserWithAssignmentDto[]>({
      method: 'GET',
      url: '/api/app/identity-user-management/staff-by-hotels',
      params: hotelIds && hotelIds.length > 0 ? { hotelIds } : {}
    }, { apiName: this.apiName });
  }
}

