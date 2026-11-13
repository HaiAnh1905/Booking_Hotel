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
    name: 'Booking Hotel',
  },
  googleClientId: '320980084490-6m666292h4ioh7vlvd126t240og4ujsc.apps.googleusercontent.com',
  turnstileSiteKey: '1x00000000000000000000AA',
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
