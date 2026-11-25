import { Injectable, inject } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

export interface RatePlanDto {
  id: string;
  name: string;
  code: string;
  description: string;
  isRefundable: boolean;
  includesBreakfast: boolean;
  cancellationCutoffHours: number;
  defaultPriceAdjustmentPercent: number;
  creationTime?: string;
}

export interface CreateUpdateRatePlanDto {
  name: string;
  code: string;
  description: string;
  isRefundable: boolean;
  includesBreakfast: boolean;
  cancellationCutoffHours: number;
  defaultPriceAdjustmentPercent: number;
}

@Injectable({
  providedIn: 'root'
})
export class TenantRatePlanService {
  private rest = inject(RestService);
  private readonly baseUrl = '/api/app/tenant/rate-plans';

  getList(): Observable<RatePlanDto[]> {
    return this.rest.request<void, RatePlanDto[]>({
      method: 'GET',
      url: this.baseUrl,
    });
  }

  get(id: string): Observable<RatePlanDto> {
    return this.rest.request<void, RatePlanDto>({
      method: 'GET',
      url: `${this.baseUrl}/${id}`,
    });
  }

  create(input: CreateUpdateRatePlanDto): Observable<RatePlanDto> {
    return this.rest.request<CreateUpdateRatePlanDto, RatePlanDto>({
      method: 'POST',
      url: this.baseUrl,
      body: input,
    });
  }

  update(id: string, input: CreateUpdateRatePlanDto): Observable<RatePlanDto> {
    return this.rest.request<CreateUpdateRatePlanDto, RatePlanDto>({
      method: 'PUT',
      url: `${this.baseUrl}/${id}`,
      body: input,
    });
  }

  delete(id: string): Observable<void> {
    return this.rest.request<void, void>({
      method: 'DELETE',
      url: `${this.baseUrl}/${id}`,
    });
  }
}
