import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe } from '@abp/ng.core';

@Component({
  selector: 'bh-loading-spinner',
  standalone: true,
  imports: [CommonModule, LocalizationPipe],
  template: `
    <div
      class="bh-spinner-wrapper"
      [class.fullscreen]="isFullscreen"
      [ngClass]="'variant-' + colorVariant"
    >
      <div class="bh-spinner" role="status">
        <span class="visually-hidden">Loading...</span>
      </div>
      @if (textKey) {
        <div class="bh-spinner-text mt-2">
          {{ textKey | abpLocalization }}
        </div>
      }
    </div>
  `,
  styleUrls: ['./bh-loading-spinner.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BhLoadingSpinnerComponent {
  @Input() textKey: string = 'Common:Loading';
  @Input() isFullscreen: boolean = false;
  @Input() colorVariant: 'red' | 'accent' | 'default' = 'red';
}

