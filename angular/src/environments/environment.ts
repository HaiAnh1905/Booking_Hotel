import { Environment } from '@abp/ng.core';

const baseUrl = 'http://localhost:4200';

const oAuthConfig = {
  issuer: 'https://localhost:44386/',
  redirectUri: baseUrl,
  clientId: 'BookingHotel_App',
  responseType: 'code',
  scope: 'offline_access BookingHotel',
  requireHttps: true,
};

export const environment = {
  production: false,
  application: {
    baseUrl,
    name: 'BookingHotel',
  },
  oAuthConfig,
  apis: {
    default: {
      url: 'https://localhost:44386',
      rootNamespace: 'BookingHotel',
    },
    AbpAccountPublic: {
      url: oAuthConfig.issuer,
      rootNamespace: 'AbpAccountPublic',
    },
  },
} as Environment;
