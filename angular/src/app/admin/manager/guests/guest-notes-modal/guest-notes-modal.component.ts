import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, OnChanges, SimpleChanges, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import { ManagerGuestService, ManagerGuestDto, InternalGuestNoteDto } from '../../../services/manager/manager-guest.service';
import { BhConfirmService } from '../../../../shared/components/confirm-modal/bh-confirm.service';

@Component({
  selector: 'bh-guest-notes-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LocalizationPipe],
  template: `
    <div class="modal-backdrop" *ngIf="isOpen && guest">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">
              <i class="fa-solid fa-note-sticky text-warning me-2"></i>
              {{ 'Manager:Guest:InternalNotes' | abpLocalization }} — {{ guest.fullName }}
            </h5>
            <button type="button" class="btn-close" (click)="close()"></button>
          </div>

          <div class="modal-body">
            <!-- Form thêm ghi chú mới -->
            <div class="add-note-card p-3 bg-light rounded-3 border mb-3">
              <h6 class="fw-bold mb-2 text-primary">
                <i class="fa-solid fa-plus-circle me-1"></i> {{ 'Manager:Guest:AddNote' | abpLocalization }}
              </h6>
              <form [formGroup]="form" (ngSubmit)="submitNote()">
                <div class="row g-2 mb-2">
                  <div class="col-md-6">
                    <label class="form-label small mb-1">{{ 'Manager:Guest:NoteCategory' | abpLocalization }}</label>
                    <select class="form-select form-select-sm" formControlName="category">
                      <option value="General">Chung (General)</option>
                      <option value="VIP">Khách VIP</option>
                      <option value="Dietary">Ẩm thực / Dị ứng (Dietary)</option>
                      <option value="Preference">Sở thích phòng (Preference)</option>
                      <option value="Warning">Cảnh báo / Lưu ý đặc biệt</option>
                    </select>
                  </div>
                  <div class="col-md-6 d-flex align-items-center mt-3">
                    <div class="form-check form-switch">
                      <input type="checkbox" class="form-check-input" id="alertSwitch" formControlName="isAlert" />
                      <label class="form-check-label small fw-bold text-danger" for="alertSwitch">
                        <i class="fa-solid fa-triangle-exclamation me-1"></i> {{ 'Manager:Guest:AlertFlag' | abpLocalization }}
                      </label>
                    </div>
                  </div>
                </div>

                <div class="mb-2">
                  <label class="form-label small mb-1">
                    {{ 'Common:Content' | abpLocalization }}
                    <span class="text-danger">*</span>
                  </label>
                  <textarea
                    class="form-control form-control-sm"
                    rows="2"
                    formControlName="noteText"
                    placeholder="Nhập nội dung ghi chú nghiệp vụ bảo mật nội bộ...">
                  </textarea>
                </div>

                <div class="d-flex justify-content-end">
                  <button type="submit" class="btn btn-primary btn-sm" [disabled]="form.invalid || isSaving()">
                    <i *ngIf="isSaving()" class="fa-solid fa-spinner fa-spin me-1"></i>
                    {{ 'Common:Save' | abpLocalization }}
                  </button>
                </div>
              </form>
            </div>

            <!-- Danh sách ghi chú đã lưu -->
            <div class="notes-timeline">
              <h6 class="fw-bold mb-2 text-muted small uppercase">
                <i class="fa-solid fa-clock-rotate-left me-1"></i> Lịch Sử Ghi Chú ({{ notes().length }})
              </h6>

              <div *ngIf="isLoading()" class="py-3 text-center text-muted">
                <i class="fa-solid fa-spinner fa-spin"></i>
              </div>

              <div *ngFor="let n of notes()" class="note-item" [class.is-alert]="n.isAlert">
                <div class="note-top">
                  <span class="badge" [ngClass]="n.isAlert ? 'bg-danger' : 'bg-secondary'">
                    {{ n.category }}
                  </span>
                  <div class="audit-meta small text-muted">
                    <i class="fa-solid fa-user-shield me-1"></i> {{ n.creatorName || 'Staff' }} • {{ n.creationTime | date:'dd/MM/yyyy HH:mm' }}
                  </div>
                  <button type="button" class="btn-delete-note" (click)="deleteNote(n)" title="Xóa ghi chú">
                    <i class="fa-solid fa-trash-can"></i>
                  </button>
                </div>
                <p class="note-text mb-0">{{ n.noteText }}</p>
              </div>

              <div *ngIf="notes().length === 0 && !isLoading()" class="text-center py-4 text-muted small">
                {{ 'Common:NoData' | abpLocalization }}
              </div>
            </div>
          </div>

          <div class="modal-footer">
            <button type="button" class="btn btn-outline-secondary" (click)="close()">
              {{ 'Common:Close' | abpLocalization }}
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .modal-backdrop {
      position: fixed;
      top: 0; left: 0; right: 0; bottom: 0;
      background: rgba(15, 23, 42, 0.6);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1050;
    }
    .modal-dialog {
      width: 100%;
      max-width: 580px;
      margin: 20px;
    }
    .modal-content {
      background: #ffffff;
      border-radius: 12px;
      overflow: hidden;
      border: 1px solid #e2e8f0;
    }
    .modal-header {
      padding: 16px 20px;
      border-bottom: 1px solid #f1f5f9;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .modal-body {
      padding: 20px;
      max-height: 75vh;
      overflow-y: auto;
    }
    .modal-footer {
      padding: 14px 20px;
      border-top: 1px solid #f1f5f9;
      display: flex;
      justify-content: flex-end;
      background: #f8fafc;
    }
    .notes-timeline {
      display: flex;
      flex-direction: column;
      gap: 10px;
    }
    .note-item {
      background: #ffffff;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      padding: 12px;
      &.is-alert {
        border-color: #fca5a5;
        background: #fffafa;
      }
      .note-top {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 6px;
      }
      .note-text {
        font-size: 0.875rem;
        color: #1e293b;
      }
      .btn-delete-note {
        border: none;
        background: transparent;
        color: #94a3b8;
        cursor: pointer;
        &:hover { color: #dc2626; }
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GuestNotesModalComponent implements OnChanges {
  private fb = inject(FormBuilder);
  private guestService = inject(ManagerGuestService);
  private confirmService = inject(BhConfirmService);

  @Input() isOpen = false;
  @Input() guest: ManagerGuestDto | null = null;
  @Input() hotelId = '';

  @Output() closed = new EventEmitter<void>();

  readonly notes = signal<InternalGuestNoteDto[]>([]);
  readonly isLoading = signal<boolean>(false);
  readonly isSaving = signal<boolean>(false);

  form = this.fb.group({
    category: ['General', [Validators.required]],
    noteText: ['', [Validators.required, Validators.maxLength(2000)]],
    isAlert: [false],
  });

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['guest'] && this.guest && this.hotelId) {
      this.loadNotes();
    }
  }

  loadNotes(): void {
    if (!this.guest || !this.hotelId) return;

    this.isLoading.set(true);
    this.guestService.getNotes(this.guest.guestId, this.hotelId).subscribe({
      next: list => {
        this.notes.set(list);
        this.isLoading.set(false);
      },
      error: () => {
        this.notes.set([]);
        this.isLoading.set(false);
      },
    });
  }

  submitNote(): void {
    if (this.form.invalid || !this.guest || !this.hotelId) return;

    this.isSaving.set(true);
    const val = this.form.value;

    this.guestService.createNote({
      hotelId: this.hotelId,
      guestId: this.guest.guestId,
      category: val.category || 'General',
      noteText: val.noteText!,
      isAlert: !!val.isAlert,
    }).subscribe({
      next: created => {
        this.notes.update(list => [created, ...list]);
        this.form.reset({ category: 'General', noteText: '', isAlert: false });
        this.isSaving.set(false);
      },
      error: () => this.isSaving.set(false),
    });
  }

  async deleteNote(n: InternalGuestNoteDto): Promise<void> {
    if (!this.hotelId) return;

    const confirmed = await this.confirmService.confirm({
      title: 'Xóa ghi chú nội bộ',
      message: 'Xác nhận xóa ghi chú nội bộ này?',
      confirmText: 'Xóa',
      cancelText: 'Hủy',
      type: 'danger',
    });
    if (!confirmed) return;

    this.guestService.deleteNote(n.id, this.hotelId).subscribe({
      next: () => {
        this.notes.update(list => list.filter(x => x.id !== n.id));
      },
    });
  }

  close(): void {
    this.closed.emit();
  }
}
