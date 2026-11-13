import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe } from '@abp/ng.core';

@Component({
  selector: 'bh-empty-state',
  standalone: true,
  imports: [CommonModule, LocalizationPipe],
  template: `
    <div class="bh-empty-state text-center py-5">
      <div class="bh-empty-icon mb-3">
        <i [class]="iconClass"></i>
      </div>
      <h5 class="bh-empty-title fw-bold mb-2">
        {{ titleKey | abpLocalization }}
      </h5>
      @if (descriptionKey) {
        <p class="bh-empty-desc text-muted mb-4 mx-auto" style="max-width: 480px;">
          {{ descriptionKey | abpLocalization }}
        </p>
      }
      @if (actionButtonKey) {
        <button
          type="button"
          class="btn btn-bh-primary btn-sm"
          (click)="action.emit()"
        >
          {{ actionButtonKey | abpLocalization }}
        </button>
      }
    </div>
  `,
  styleUrls: ['./bh-empty-state.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BhEmptyStateComponent {
  @Input() iconClass: string = 'fa-solid fa-folder-open';
  @Input() titleKey: string = 'Common:NoData';
  @Input() descriptionKey?: string;
  @Input() actionButtonKey?: string;
  @Output() action = new EventEmitter<void>();
}
