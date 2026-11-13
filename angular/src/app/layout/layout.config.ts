import { inject, provideAppInitializer } from '@angular/core';
import { ReplaceableComponentsService } from '@abp/ng.core';
import { eThemeLeptonXComponents } from '@abp/ng.theme.lepton-x';
import { ManagementLayoutComponent } from './management/management-layout.component';

function initLayout() {
  const replaceableComponents = inject(ReplaceableComponentsService);
  replaceableComponents.add({
    key: eThemeLeptonXComponents.ApplicationLayout,
    component: ManagementLayoutComponent,
  });
}

export const LAYOUT_PROVIDER = [
  provideAppInitializer(() => {
    initLayout();
  }),
];
