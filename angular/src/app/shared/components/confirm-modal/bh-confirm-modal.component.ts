import { ChangeDetectionStrategy, Component, HostListener, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BhConfirmService } from './bh-confirm.service';

@Component({
  selector: 'bh-confirm-modal',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (confirmService.state(); as modal) {
      <div class="bh-confirm-backdrop" (click)="onBackdropClick()">
        <div
          class="bh-confirm-dialog"
          [ngClass]="'type-' + modal.type"
          role="alertdialog"
          aria-modal="true"
          [attr.aria-labelledby]="'bh-confirm-title'"
          [attr.aria-describedby]="'bh-confirm-desc'"
          (click)="$event.stopPropagation()"
        >
          <button
            type="button"
            class="bh-confirm-close"
            aria-label="Close"
            (click)="confirmService.reject()"
          >
            <i class="fa-solid fa-xmark"></i>
          </button>

          <div class="bh-confirm-body">
            <div class="bh-confirm-icon-wrap" [ngClass]="'icon-' + modal.type">
              <i [class]="modal.iconClass"></i>
            </div>

            <div class="bh-confirm-text">
              <h5 id="bh-confirm-title" class="bh-confirm-title">{{ modal.title }}</h5>
              <p id="bh-confirm-desc" class="bh-confirm-message">{{ modal.message }}</p>
            </div>
          </div>

          <div class="bh-confirm-footer">
            <button
              type="button"
              class="bh-btn bh-btn-cancel"
              (click)="confirmService.reject()"
            >
              {{ modal.cancelText }}
            </button>
            <button
              type="button"
              class="bh-btn bh-btn-confirm"
              [ngClass]="'btn-' + modal.type"
              (click)="confirmService.accept()"
            >
              {{ modal.confirmText }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
  styleUrls: ['./bh-confirm-modal.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BhConfirmModalComponent {
  readonly confirmService = inject(BhConfirmService);

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.confirmService.state()) {
      this.confirmService.reject();
    }
  }

  onBackdropClick(): void {
    this.confirmService.reject();
  }
}
