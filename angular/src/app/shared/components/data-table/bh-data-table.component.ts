import { ChangeDetectionStrategy, Component, ContentChild, EventEmitter, Input, Output, TemplateRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe } from '@abp/ng.core';
import { BhDataTableColumn, BhPageChangeEvent, BhPaginationState } from '../../models/ui-models';
import { BhLoadingSpinnerComponent } from '../loading-spinner/bh-loading-spinner.component';
import { BhEmptyStateComponent } from '../empty-state/bh-empty-state.component';

@Component({
  selector: 'bh-data-table',
  standalone: true,
  imports: [CommonModule, LocalizationPipe, BhLoadingSpinnerComponent, BhEmptyStateComponent],
  template: `
    <div class="bh-table-wrapper position-relative">
      @if (loading) {
        <div class="bh-table-loading-overlay">
          <bh-loading-spinner [textKey]="'Common:Loading'"></bh-loading-spinner>
        </div>
      }

      <div class="table-responsive bh-table-responsive">
        <table
          class="table bh-table align-middle"
          [class.table-striped]="striped"
          [class.table-bordered]="bordered"
        >
          <thead>
            <tr>
              @for (col of columns; track col.field) {
                <th
                  [style.width]="col.width"
                  [class.text-start]="!col.align || col.align === 'left'"
                  [class.text-center]="col.align === 'center'"
                  [class.text-end]="col.align === 'right'"
                  [class.sortable]="col.sortable"
                  (click)="onSort(col)"
                >
                  <div class="d-inline-flex align-items-center gap-1">
                    <span>{{ col.headerKey | abpLocalization }}</span>
                    @if (col.sortable) {
                      <span class="sort-icon">
                        @if (pagination?.sortField === col.field) {
                          @if (pagination?.sortDirection === 'asc') {
                            <i class="fa-solid fa-arrow-up-short-wide text-primary"></i>
                          } @else {
                            <i class="fa-solid fa-arrow-down-wide-short text-primary"></i>
                          }
                        } @else {
                          <i class="fa-solid fa-sort text-muted opacity-50"></i>
                        }
                      </span>
                    }
                  </div>
                </th>
              }
            </tr>
          </thead>
          <tbody>
            @if (items && items.length > 0) {
              @for (item of items; track trackByItem(item, $index)) {
                <tr>
                  @for (col of columns; track col.field) {
                    <td
                      [class.text-start]="!col.align || col.align === 'left'"
                      [class.text-center]="col.align === 'center'"
                      [class.text-end]="col.align === 'right'"
                    >
                      @if (cellTemplate) {
                        <ng-container
                          *ngTemplateOutlet="cellTemplate; context: { $implicit: item, column: col }"
                        ></ng-container>
                      } @else {
                        {{ item[col.field] }}
                      }
                    </td>
                  }
                </tr>
              }
            } @else if (!loading) {
              <tr>
                <td [attr.colspan]="columns.length" class="text-center py-4">
                  <bh-empty-state [titleKey]="emptyTextKey"></bh-empty-state>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>

      <!-- Pagination Footer -->
      @if (pagination && pagination.totalCount > 0) {
        <div class="bh-table-pagination d-flex flex-wrap align-items-center justify-content-between p-3 gap-2">
          <div class="pagination-info text-muted small">
            <span>{{ 'Common:TotalRecords' | abpLocalization: (pagination.totalCount + '') }}</span>
          </div>

          <div class="pagination-controls d-flex align-items-center gap-1">
            <button
              type="button"
              class="btn btn-sm btn-outline-secondary"
              [disabled]="pagination.currentPage <= 1 || loading"
              (click)="goToPage(pagination.currentPage - 1)"
            >
              <i class="fa-solid fa-chevron-left"></i>
            </button>

            <span class="px-2 small text-muted">
              {{ 'Common:Page' | abpLocalization }} {{ pagination.currentPage }} {{ 'Common:Of' | abpLocalization }} {{ getTotalPages() }}
            </span>

            <button
              type="button"
              class="btn btn-sm btn-outline-secondary"
              [disabled]="pagination.currentPage >= getTotalPages() || loading"
              (click)="goToPage(pagination.currentPage + 1)"
            >
              <i class="fa-solid fa-chevron-right"></i>
            </button>
          </div>
        </div>
      }
    </div>
  `,
  styleUrls: ['./bh-data-table.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BhDataTableComponent {
  @Input() columns: BhDataTableColumn[] = [];
  @Input() items: any[] = [];
  @Input() loading: boolean = false;
  @Input() pagination: BhPaginationState | null = null;
  @Input() emptyTextKey: string = 'Common:NoData';
  @Input() striped: boolean = true;
  @Input() bordered: boolean = false;

  @ContentChild('cellTemplate') cellTemplate?: TemplateRef<any>;

  @Output() pageChange = new EventEmitter<BhPageChangeEvent>();
  @Output() sortChange = new EventEmitter<{ field: string; direction: 'asc' | 'desc' }>();

  trackByItem(item: any, index: number): any {
    return item?.id || index;
  }

  onSort(col: BhDataTableColumn) {
    if (!col.sortable) return;

    let direction: 'asc' | 'desc' = 'asc';
    if (this.pagination?.sortField === col.field) {
      direction = this.pagination.sortDirection === 'asc' ? 'desc' : 'asc';
    }

    this.sortChange.emit({
      field: col.field as string,
      direction,
    });
  }

  getTotalPages(): number {
    if (!this.pagination || this.pagination.pageSize <= 0) return 1;
    return Math.max(1, Math.ceil(this.pagination.totalCount / this.pagination.pageSize));
  }

  goToPage(page: number) {
    if (!this.pagination) return;
    const totalPages = this.getTotalPages();
    if (page < 1 || page > totalPages) return;

    this.pageChange.emit({
      page,
      pageSize: this.pagination.pageSize,
    });
  }
}
