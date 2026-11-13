import { ChangeDetectionStrategy, Component } from '@angular/core';
import { DynamicLayoutComponent } from '@abp/ng.core';
import { LoaderBarComponent } from '@abp/ng.theme.shared';
import { BhToastrContainerComponent } from './shared/components/toastr/bh-toastr-container.component';

@Component({
  selector: 'app-root',
  template: `
    <abp-loader-bar />
    <abp-dynamic-layout />
    <bh-toastr-container />
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  imports: [LoaderBarComponent, DynamicLayoutComponent, BhToastrContainerComponent],
})
export class AppComponent {}
