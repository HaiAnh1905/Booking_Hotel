import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe } from '@abp/ng.core';
import { BhToastrService } from './bh-toastr.service';

@Component({
  selector: 'bh-toastr-container',
  standalone: true,
  imports: [CommonModule, LocalizationPipe],
  template: `
    <div class="bh-toastr-container" aria-live="polite" aria-atomic="true">
      @for (toast of toastrService.toasts(); track toast.id) {
        <div class="bh-toast shadow" [ngClass]="'bh-toast-' + toast.type">
          <div class="bh-toast-icon">
            @switch (toast.type) {
              @case ('success') {
                <i class="fa-solid fa-circle-check"></i>
              }
              @case ('error') {
                <i class="fa-solid fa-circle-xmark"></i>
              }
              @case ('warning') {
                <i class="fa-solid fa-triangle-exclamation"></i>
              }
              @case ('info') {
                <i class="fa-solid fa-circle-info"></i>
              }
            }
          </div>

          <div class="bh-toast-content">
            @if (toast.titleKey) {
              <div class="bh-toast-title fw-bold">
                {{ toast.titleKey | abpLocalization }}
              </div>
            }
            <div class="bh-toast-message">
              {{ toast.messageKey | abpLocalization }}
            </div>
          </div>

          <button
            type="button"
            class="bh-toast-close"
            (click)="toastrService.remove(toast.id)"
            aria-label="Close"
          >
            <i class="fa-solid fa-xmark"></i>
          </button>
        </div>
      }
    </div>
  `,
  styleUrls: ['./bh-toastr.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BhToastrContainerComponent {
  toastrService = inject(BhToastrService);
}

