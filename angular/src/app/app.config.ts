import { AuthService, CHECK_AUTHENTICATION_STATE_FN_KEY, LocalizationService, provideAbpCore, withOptions } from '@abp/ng.core';
import { provideAbpOAuth } from '@abp/ng.oauth';
import { OAuthStorage } from 'angular-oauth2-oidc';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { provideSettingManagementConfig } from '@abp/ng.setting-management/config';
import { provideFeatureManagementConfig } from '@abp/ng.feature-management';
import { provideAbpThemeShared } from '@abp/ng.theme.shared';
import { provideIdentityConfig } from '@abp/ng.identity/config';
import { provideAccountConfig } from '@abp/ng.account/config';
import { provideTenantManagementConfig } from '@abp/ng.tenant-management/config';
import { registerLocaleForEsBuild } from '@abp/ng.core/locale';
import { provideThemeLeptonX } from '@abp/ng.theme.lepton-x';
import { provideSideMenuLayout } from '@abp/ng.theme.lepton-x/layouts';
import { provideLogo, withEnvironmentOptions } from "@abp/ng.theme.shared";
import { ApplicationConfig } from '@angular/core';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { environment } from '../environments/environment';
import { APP_ROUTES } from './app.routes';
import { APP_ROUTE_PROVIDER } from './route.provider';
import { FOOTER_PROVIDER } from './footer/footer.config';
import { LAYOUT_PROVIDER } from './layout/layout.config';
import { CustomAuthService } from './auth/services/custom-auth.service';
import { AuthHeaderInterceptor } from './auth/services/auth-header.interceptor';
import { CustomLocalizationService } from './shared/services/custom-localization.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(APP_ROUTES),
    APP_ROUTE_PROVIDER,
    FOOTER_PROVIDER,
    LAYOUT_PROVIDER,
    provideAnimations(),
    provideAbpCore(
      withOptions({
        environment,
        registerLocaleFn: registerLocaleForEsBuild(),
      }),
    ),
    provideAbpOAuth(),
    {
      provide: CHECK_AUTHENTICATION_STATE_FN_KEY,
      useValue: () => () => {},
    },
    {
      provide: OAuthStorage,
      useFactory: () => (typeof window !== 'undefined' ? window.localStorage : ({} as any)),
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthHeaderInterceptor,
      multi: true,
    },
    {
      provide: AuthService,
      useClass: CustomAuthService,
    },
    {
      provide: LocalizationService,
      useClass: CustomLocalizationService,
    },
    provideIdentityConfig(),
    provideSettingManagementConfig(),
    provideFeatureManagementConfig(),
    provideThemeLeptonX(),
    provideSideMenuLayout(),
    provideLogo(withEnvironmentOptions(environment)),
    provideAccountConfig(),
    provideTenantManagementConfig(),
    provideAbpThemeShared(),
  ]
};
