import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  HostListener,
  Input,
  OnChanges,
  OnDestroy,
  Output,
  SimpleChanges
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe } from '@abp/ng.core';
import { BhModalConfig } from '../../models/ui-models';

@Component({
  selector: 'bh-modal',
  standalone: true,
  imports: [CommonModule, LocalizationPipe],
  template: `
    @if (isOpen) {
      <div class="bh-modal-backdrop" (click)="onBackdropClick($event)">
        <div
          class="bh-modal-dialog"
          [ngClass]="'modal-' + (config.size || 'md')"
          role="dialog"
          aria-modal="true"
          (click)="$event.stopPropagation()"
        >
          <div class="bh-modal-content shadow-lg">
            <!-- Modal Header -->
            <div class="bh-modal-header">
              <div class="d-flex align-items-center">
                @if (config.iconClass) {
                  <i [class]="config.iconClass + ' bh-modal-title-icon me-2'"></i>
                }
                <h5 class="bh-modal-title mb-0">
                  {{ config.titleKey | abpLocalization }}
                </h5>
              </div>
              <button
                type="button"
                class="btn-close"
                aria-label="Close"
                (click)="onCancel()"
              ></button>
            </div>

            <!-- Modal Body -->
            <div class="bh-modal-body">
              @if (config.messageKey) {
                <p class="mb-3">{{ config.messageKey | abpLocalization }}</p>
              }
              <ng-content></ng-content>
            </div>

            <!-- Modal Footer -->
            <div class="bh-modal-footer">
              @if (!config.hideCancelButton) {
                <button
                  type="button"
                  class="btn btn-outline-secondary"
                  [disabled]="busy"
                  (click)="onCancel()"
                >
                  {{ (config.cancelTextKey || 'Common:Cancel') | abpLocalization }}
                </button>
              }

              <button
                type="button"
                class="btn"
                [ngClass]="getConfirmButtonClass()"
                [disabled]="busy"
                (click)="onConfirm()"
              >
                @if (busy) {
                  <span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>
                }
                {{ (config.confirmTextKey || 'Common:Confirm') | abpLocalization }}
              </button>
            </div>
          </div>
        </div>
      </div>
    }
  `,
  styleUrls: ['./bh-modal.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BhModalComponent implements OnChanges, OnDestroy {
  private static openModalCount = 0;
  private wasOpen = false;

  @Input() isOpen: boolean = false;
  @Input() config: BhModalConfig = { titleKey: 'Common:Confirm' };
  @Input() busy: boolean = false;

  @Output() confirm = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();
  @Output() isOpenChange = new EventEmitter<boolean>();

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen']) {
      this.syncBodyScrollLock();
    }
  }

  ngOnDestroy(): void {
    if (this.wasOpen) {
      this.wasOpen = false;
      BhModalComponent.openModalCount = Math.max(0, BhModalComponent.openModalCount - 1);
      if (BhModalComponent.openModalCount === 0 && typeof document !== 'undefined') {
        document.body.classList.remove('bh-modal-open');
      }
    }
  }

  private syncBodyScrollLock(): void {
    if (typeof document === 'undefined') return;

    if (this.isOpen && !this.wasOpen) {
      this.wasOpen = true;
      BhModalComponent.openModalCount++;
      if (BhModalComponent.openModalCount === 1) {
        document.body.classList.add('bh-modal-open');
      }
    } else if (!this.isOpen && this.wasOpen) {
      this.wasOpen = false;
      BhModalComponent.openModalCount = Math.max(0, BhModalComponent.openModalCount - 1);
      if (BhModalComponent.openModalCount === 0) {
        document.body.classList.remove('bh-modal-open');
      }
    }
  }

  @HostListener('document:keydown.escape')
  handleEscape() {
    if (this.isOpen && !this.busy) {
      this.onCancel();
    }
  }

  onBackdropClick(event: MouseEvent) {
    if (this.busy) return;

    // Không đóng modal nếu người dùng click hoặc kéo thanh cuộn của backdrop (vùng cuộn bên phải)
    const target = event.target as HTMLElement;
    if (target && event.clientX >= target.clientWidth) {
      return;
    }

    this.onCancel();
  }

  onCancel() {
    this.isOpen = false;
    this.syncBodyScrollLock();
    this.isOpenChange.emit(false);
    this.cancel.emit();
  }

  onConfirm() {
    this.confirm.emit();
  }

  getConfirmButtonClass(): string {
    const variant = this.config.confirmVariant || 'primary';
    switch (variant) {
      case 'danger':
        return 'btn-danger';
      case 'warning':
        return 'btn-warning';
      case 'accent':
        return 'btn-bh-accent';
      case 'primary':
      default:
        return 'btn-bh-primary';
    }
  }
}

