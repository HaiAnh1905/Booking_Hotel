import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe } from '@abp/ng.core';
import { StatusBadgeVariant } from '../../models/ui-models';

@Component({
  selector: 'bh-status-badge',
  standalone: true,
  imports: [CommonModule, LocalizationPipe],
  template: `
    <span
      class="bh-badge"
      [ngClass]="'bh-badge-' + variant"
      [class.bh-badge-dot]="isDot"
    >
      @if (isDot) {
        <span class="dot-indicator"></span>
      }
      @if (iconClass) {
        <i [class]="iconClass + ' me-1'"></i>
      }
      <span class="badge-text">
        @if (textKey) {
          {{ textKey | abpLocalization }}
        } @else {
          {{ text }}
        }
      </span>
    </span>
  `,
  styleUrls: ['./bh-status-badge.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BhStatusBadgeComponent {
  @Input() variant: StatusBadgeVariant = 'available';
  @Input() textKey: string = '';
  @Input() text: string = '';
  @Input() iconClass: string = '';
  @Input() isDot: boolean = false;
}
