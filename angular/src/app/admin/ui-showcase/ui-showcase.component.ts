import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe } from '@abp/ng.core';
import { BhDataTableComponent } from '../../shared/components/data-table/bh-data-table.component';
import { BhModalComponent } from '../../shared/components/modal/bh-modal.component';
import { BhStatusBadgeComponent } from '../../shared/components/status-badge/bh-status-badge.component';
import { BhLoadingSpinnerComponent } from '../../shared/components/loading-spinner/bh-loading-spinner.component';
import { BhEmptyStateComponent } from '../../shared/components/empty-state/bh-empty-state.component';
import { BhToastrService } from '../../shared/components/toastr/bh-toastr.service';
import { BhDataTableColumn, BhModalConfig, BhPaginationState } from '../../shared/models/ui-models';

interface SampleAccount {
  id: string;
  fullName: string;
  email: string;
  role: string;
  status: 'active' | 'locked' | 'inactive';
}

@Component({
  selector: 'bh-ui-showcase',
  standalone: true,
  imports: [
    CommonModule,
    LocalizationPipe,
    BhDataTableComponent,
    BhModalComponent,
    BhStatusBadgeComponent,
    BhLoadingSpinnerComponent,
    BhEmptyStateComponent,
  ],
  templateUrl: './ui-showcase.component.html',
  styleUrls: ['./ui-showcase.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UiShowcaseComponent {
  private toastr = inject(BhToastrService);

  // Modal demo state
  isModalOpen = signal<boolean>(false);
  isModalBusy = signal<boolean>(false);

  modalConfig: BhModalConfig = {
    titleKey: 'Admin:SampleModal',
    messageKey: 'Admin:ModalConfirmWarning',
    size: 'md',
    confirmVariant: 'danger',
    confirmTextKey: 'Common:Delete',
    cancelTextKey: 'Common:Cancel',
    iconClass: 'fa-solid fa-triangle-exclamation',
  };

  // Spinner demo state
  isSpinnerActive = signal<boolean>(false);

  // DataTable demo state
  columns: BhDataTableColumn<SampleAccount>[] = [
    { field: 'id', headerKey: 'Mã', width: '90px' },
    { field: 'fullName', headerKey: 'Auth:FullName', sortable: true },
    { field: 'email', headerKey: 'Auth:Email', sortable: true },
    { field: 'role', headerKey: 'Vai trò' },
    { field: 'status', headerKey: 'Common:Status', align: 'center', width: '130px' },
    { field: 'actions', headerKey: 'Common:Actions', align: 'right', width: '120px' },
  ];

  sampleData = signal<SampleAccount[]>([
    { id: 'USR-01', fullName: 'Quản Trị Viên Hệ Thống', email: 'admin@bookinghotel.com', role: 'System Admin', status: 'active' },
    { id: 'USR-02', fullName: 'Trần Thị Giám Đốc', email: 'manager@vinpearl.com', role: 'Hotel Manager', status: 'active' },
    { id: 'USR-03', fullName: 'Lê Văn Lễ Tân', email: 'reception@hanoiplaza.com', role: 'Receptionist', status: 'active' },
    { id: 'USR-04', fullName: 'Nguyễn Khách Hàng', email: 'customer@gmail.com', role: 'Customer', status: 'active' },
    { id: 'USR-05', fullName: 'Tài Khoản Tạm Khóa', email: 'locked_user@domain.com', role: 'Customer', status: 'locked' },
  ]);

  paginationState: BhPaginationState = {
    currentPage: 1,
    pageSize: 5,
    totalCount: 5,
    sortField: 'fullName',
    sortDirection: 'asc',
  };

  triggerToast(type: 'success' | 'error' | 'warning' | 'info') {
    switch (type) {
      case 'success':
        this.toastr.success('Toastr:OperationSuccessful', 'Toastr:Success');
        break;
      case 'error':
        this.toastr.error('Toastr:OperationFailed', 'Toastr:Error');
        break;
      case 'warning':
        this.toastr.warning('Cảnh báo: Kiểm tra kỹ thông số phòng trước khi xác nhận.', 'Toastr:Warning');
        break;
      case 'info':
        this.toastr.info('Hệ thống đang đồng bộ dữ liệu song ngữ.', 'Toastr:Info');
        break;
    }
  }

  openModal() {
    this.isModalOpen.set(true);
  }

  handleModalConfirm() {
    this.isModalBusy.set(true);
    setTimeout(() => {
      this.isModalBusy.set(false);
      this.isModalOpen.set(false);
      this.toastr.success('Thao tác xác nhận hoàn tất thành công!');
    }, 1000);
  }

  toggleSpinner() {
    this.isSpinnerActive.set(true);
    setTimeout(() => {
      this.isSpinnerActive.set(false);
    }, 1500);
  }
}
