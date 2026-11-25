import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';

function parseJwt(token: string): any {
  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );
    return JSON.parse(jsonPayload);
  } catch {
    return null;
  }
}

@Injectable({
  providedIn: 'root'
})
export class AuthHeaderInterceptor implements HttpInterceptor {
  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    if (typeof localStorage !== 'undefined') {
      const token = localStorage.getItem('access_token');
      if (token && token !== 'undefined' && token !== 'null') {
        const headers: { [key: string]: string } = {};

        if (!req.headers.has('Authorization')) {
          headers['Authorization'] = `Bearer ${token}`;
        }

        if (!req.headers.has('__tenant')) {
          const payload = parseJwt(token);
          let tenantId = payload?.tenantid || payload?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/tenantid'] || payload?.TenantId;
          if (Array.isArray(tenantId)) {
            tenantId = tenantId[0];
          }
          if (tenantId) {
            headers['__tenant'] = tenantId;
          }
        }

        req = req.clone({ setHeaders: headers });
      }
    }
    return next.handle(req);
  }
}

