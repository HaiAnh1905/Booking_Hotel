import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  ElementRef,
  EventEmitter,
  HostListener,
  Input,
  OnChanges,
  OnDestroy,
  Output,
  SimpleChanges,
  ViewChild,
  forwardRef,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';

export interface SelectOption {
  value: any;
  label: string;
  sublabel?: string;
  icon?: string;
  disabled?: boolean;
}

function removeVietnameseTones(str: string): string {
  if (!str) return '';
  str = str.replace(/à|á|ạ|ả|ã|â|ầ|ấ|ậ|ẩ|ẫ|ă|ằ|ắ|ặ|ẳ|ẵ/g, 'a');
  str = str.replace(/è|é|ẹ|ẻ|ẽ|ê|ề|ế|ệ|ể|ễ/g, 'e');
  str = str.replace(/ì|í|ị|ỉ|ĩ/g, 'i');
  str = str.replace(/ò|ó|ọ|ỏ|õ|ô|ồ|ố|ộ|ổ|ỗ|ơ|ờ|ớ|ợ|ở|ỡ/g, 'o');
  str = str.replace(/ù|ú|ụ|ủ|ũ|ư|ừ|ứ|ự|ử|ữ/g, 'u');
  str = str.replace(/ỳ|ý|ỵ|ỷ|ỹ/g, 'y');
  str = str.replace(/đ/g, 'd');
  str = str.replace(/À|Á|Ạ|Ả|Ã|Â|Ầ|Ấ|Ậ|Ẩ|Ẫ|Ă|Ằ|Ắ|Ặ|Ẳ|Ẵ/g, 'A');
  str = str.replace(/È|É|Ẹ|Ẻ|Ẽ|Ê|Ề|Ế|Ệ|Ể|Ễ/g, 'E');
  str = str.replace(/Ì|Í|Ị|Ỉ|Ĩ/g, 'I');
  str = str.replace(/Ò|Ó|Ỏ|Õ|Ô|Ồ|Ố|Ộ|Ổ|Ỗ|Ơ|Ờ|Ớ|Ợ|Ở|Ỡ/g, 'O');
  str = str.replace(/Ù|Ú|Ụ|Ủ|Ũ|Ư|Ừ|Ứ|Ự|Ử|Ữ/g, 'U');
  str = str.replace(/Ỳ|Ý|Ỵ|Ỷ|Ỹ/g, 'Y');
  str = str.replace(/Đ/g, 'D');
  return str.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
}

@Component({
  selector: 'bh-searchable-select',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div
      class="bh-select-container"
      [class.is-open]="isOpen"
      [class.drop-up]="isDropUp"
      [class.is-disabled]="disabled"
      [class.has-value]="hasValue"
    >
      <!-- Trigger Box -->
      <div
        class="bh-select-trigger"
        tabindex="0"
        (click)="toggleDropdown($event)"
        (keydown.enter)="toggleDropdown($event)"
        (keydown.space)="toggleDropdown($event)"
      >
        <div class="trigger-content">
          @if (displayIcon) {
            <i [class]="displayIcon + ' trigger-icon me-2'"></i>
          }
          @if (displayLabel) {
            <span class="selected-label">{{ displayLabel }}</span>
            @if (displaySublabel) {
              <span class="selected-sublabel ms-1 text-muted">({{ displaySublabel }})</span>
            }
          } @else {
            <span class="placeholder-text">{{ placeholder }}</span>
          }
        </div>

        <div class="trigger-actions">
          @if (clearable && hasValue && !disabled) {
            <button
              type="button"
              class="clear-btn"
              (click)="onClear($event)"
              [title]="'Xóa lựa chọn'"
            >
              <i class="fa-solid fa-xmark"></i>
            </button>
          }
          <span class="chevron-icon" [class.rotate]="isOpen">
            <i class="fa-solid fa-chevron-down"></i>
          </span>
        </div>
      </div>

      <!-- Dropdown Menu -->
      @if (isOpen) {
        <div class="bh-select-menu shadow-lg">
          <!-- Search Header -->
          @if (searchable) {
            <div class="menu-search-header">
              <div class="search-box-wrap">
                <i class="fa-solid fa-magnifying-glass search-icon"></i>
                <input
                  #searchInput
                  type="text"
                  class="search-input"
                  [placeholder]="searchPlaceholder"
                  [(ngModel)]="searchText"
                  (input)="onSearchChange()"
                  autocomplete="off"
                />
                @if (searchText) {
                  <button type="button" class="clear-search-btn" (click)="clearSearch()">
                    <i class="fa-solid fa-xmark"></i>
                  </button>
                }
              </div>
            </div>
          }

          <!-- Options List -->
          <div class="menu-options-list" (mousedown)="$event.preventDefault()">
            <!-- Lựa chọn để chọn trống khi clearable = true -->
            @if (showClearOptionInMenu) {
              <div
                class="option-item clear-option-item"
                [class.selected]="!hasValue"
                (click)="onSelectClearOption()"
              >
                <div class="option-label-wrap">
                  <i class="fa-regular fa-circle me-2 text-muted"></i>
                  <span class="option-label text-muted fst-italic">{{ resolvedClearOptionLabel }}</span>
                </div>
                @if (!hasValue) {
                  <i class="fa-solid fa-check check-icon"></i>
                }
              </div>
            }

            @for (opt of filteredOptions; track (opt.value !== '' && opt.value !== null && opt.value !== undefined ? opt.value : ('empty_' + $index))) {
              <div
                class="option-item"
                [class.selected]="isOptionSelected(opt)"
                [class.disabled]="opt.disabled"
                (click)="onSelectOption(opt)"
              >
                <div class="option-label-wrap">
                  @if (opt.icon) {
                    <i [class]="opt.icon + ' me-2 text-muted'"></i>
                  }
                  <span class="option-label">{{ opt.label }}</span>
                  @if (opt.sublabel) {
                    <span class="option-sublabel text-muted">({{ opt.sublabel }})</span>
                  }
                </div>
                @if (isOptionSelected(opt)) {
                  <i class="fa-solid fa-check check-icon"></i>
                }
              </div>
            }

            @if (filteredOptions.length === 0 && !showClearOptionInMenu) {
              <div class="no-options-found">
                <i class="fa-solid fa-circle-question me-1 text-muted"></i>
                <span>{{ emptyText }}</span>
              </div>
            }
          </div>
        </div>
      }
    </div>
  `,
  styleUrls: ['./bh-searchable-select.component.scss'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => BhSearchableSelectComponent),
      multi: true
    }
  ],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BhSearchableSelectComponent implements ControlValueAccessor, OnChanges, OnDestroy {
  private elementRef = inject(ElementRef);
  private cdr = inject(ChangeDetectorRef);

  // Global singleton tracking the currently open dropdown to ensure only one is open at a time
  private static activeInstance: BhSearchableSelectComponent | null = null;

  @ViewChild('searchInput') searchInputElement?: ElementRef<HTMLInputElement>;

  @Input() options: SelectOption[] = [];
  @Input() placeholder = '--- Vui lòng chọn ---';
  @Input() searchPlaceholder = 'Tìm kiếm nhanh...';
  @Input() emptyText = 'Không tìm thấy kết quả phù hợp';
  @Input() clearable = true;
  @Input() clearOptionLabel?: string;
  @Input() disabled = false;
  @Input() searchable = true;
  @Input() placement: 'auto' | 'bottom' | 'top' = 'auto';

  @Output() selectionChange = new EventEmitter<any>();

  isOpen = false;
  isDropUp = false;
  searchText = '';
  value: any = null;

  filteredOptions: SelectOption[] = [];

  onChange: (val: any) => void = () => {};
  onTouched: () => void = () => {};

  get resolvedClearOptionLabel(): string {
    return this.clearOptionLabel || '--- Để trống / Không chọn ---';
  }

  get showClearOptionInMenu(): boolean {
    if (!this.clearable && !this.clearOptionLabel) return false;
    const hasEmptyInOptions = this.options.some((o) => o.value === '' || o.value === null);
    if (hasEmptyInOptions) return false;

    if (this.searchText.trim()) {
      const q = removeVietnameseTones(this.searchText.trim().toLowerCase());
      const labelClean = removeVietnameseTones(this.resolvedClearOptionLabel.toLowerCase());
      return labelClean.includes(q) || 'de trong khong chon chua phan cong'.includes(q);
    }
    return true;
  }

  onSelectClearOption(): void {
    this.value = '';
    this.onChange('');
    this.selectionChange.emit('');
    this.closeDropdown();
  }

  get hasValue(): boolean {
    return this.value !== null && this.value !== undefined && this.value !== '';
  }

  get selectedOption(): SelectOption | undefined {
    if (this.value === null || this.value === undefined) {
      return undefined;
    }

    if (this.value === '') {
      return this.options.find(o => o.value === '');
    }

    // 1. Khớp chính xác giá trị
    const exact = this.options.find(o => o.value === this.value);
    if (exact) return exact;

    // 2. Khớp chuỗi không phân biệt hoa thường
    if (typeof this.value === 'string') {
      const strVal = this.value.trim().toLowerCase();
      const caseMatch = this.options.find(o => String(o.value).trim().toLowerCase() === strVal);
      if (caseMatch) return caseMatch;

      // 3. Khớp thông minh bỏ tiền tố Tỉnh / Thành phố / Tp / TP. (ví dụ "Tp. Hồ Chí Minh" vs "Thành phố Hồ Chí Minh")
      const cleanVal = removeVietnameseTones(strVal)
        .replace(/^(thanh pho|tp\.|tp|tinh)\s*/i, '')
        .trim();

      if (cleanVal.length >= 2) {
        const fuzzyMatch = this.options.find(o => {
          const cleanOpt = removeVietnameseTones(String(o.value || o.label))
            .replace(/^(thanh pho|tp\.|tp|tinh)\s*/i, '')
            .trim();
          return cleanOpt === cleanVal || cleanOpt.includes(cleanVal) || cleanVal.includes(cleanOpt);
        });
        if (fuzzyMatch) return fuzzyMatch;
      }
    }

    return undefined;
  }

  get displayLabel(): string {
    if (this.selectedOption) {
      return this.selectedOption.label;
    }
    // Nếu có giá trị nhưng chưa khớp danh mục (ví dụ dữ liệu cũ hoặc đang tải API), vẫn hiển thị giá trị thật
    if (this.hasValue) {
      return String(this.value);
    }
    return '';
  }

  get displaySublabel(): string | undefined {
    return this.selectedOption?.sublabel;
  }

  get displayIcon(): string | undefined {
    return this.selectedOption?.icon;
  }

  isOptionSelected(opt: SelectOption): boolean {
    if (!this.hasValue) {
      return opt.value === '' || opt.value === null;
    }
    if (opt.value === this.value) return true;
    return this.selectedOption?.value === opt.value;
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['options']) {
      this.filterOptions();
      this.cdr.markForCheck();
    }
  }

  ngOnDestroy(): void {
    this.closeDropdown();
  }

  writeValue(val: any): void {
    this.value = val;
    this.cdr.markForCheck();
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  setDisabledState?(isDisabled: boolean): void {
    this.disabled = isDisabled;
    this.cdr.markForCheck();
  }

  toggleDropdown(event?: Event): void {
    if (this.disabled) return;

    if (this.isOpen) {
      this.closeDropdown();
    } else {
      this.openDropdown();
    }
  }

  openDropdown(): void {
    // Đóng bất kỳ dropdown nào khác đang mở trên toàn hệ thống ngay lập tức
    if (BhSearchableSelectComponent.activeInstance && BhSearchableSelectComponent.activeInstance !== this) {
      BhSearchableSelectComponent.activeInstance.closeDropdown();
    }
    BhSearchableSelectComponent.activeInstance = this;

    this.isOpen = true;
    this.searchText = '';
    this.calculatePlacement();
    this.filterOptions();

    // Lắng nghe tương tác bên ngoài ở Capture phase để đóng trước khi bất kỳ stopPropagation nào can thiệp
    document.addEventListener('pointerdown', this.onGlobalPointerDown, true);
    document.addEventListener('mousedown', this.onGlobalPointerDown, true);

    if (this.searchable) {
      setTimeout(() => {
        this.searchInputElement?.nativeElement.focus();
      }, 50);
    }

    this.cdr.detectChanges();
  }

  closeDropdown(): void {
    if (!this.isOpen) return;

    if (BhSearchableSelectComponent.activeInstance === this) {
      BhSearchableSelectComponent.activeInstance = null;
    }

    this.isOpen = false;
    document.removeEventListener('pointerdown', this.onGlobalPointerDown, true);
    document.removeEventListener('mousedown', this.onGlobalPointerDown, true);
    this.onTouched();
    this.cdr.detectChanges();
  }

  private onGlobalPointerDown = (event: Event): void => {
    // Nếu tương tác chuột/chạm diễn ra bên ngoài container dropdown này -> đóng ngay lập tức
    if (this.isOpen && event.target && !this.elementRef.nativeElement.contains(event.target as Node)) {
      this.closeDropdown();
    }
  };

  private calculatePlacement(): void {
    if (this.placement === 'top') {
      this.isDropUp = true;
      return;
    }
    if (this.placement === 'bottom') {
      this.isDropUp = false;
      return;
    }

    const rect = this.elementRef.nativeElement.getBoundingClientRect();
    const spaceBelow = window.innerHeight - rect.bottom;
    const spaceAbove = rect.top;

    // Dropdown menu takes ~280px. Open upwards if space below is limited
    this.isDropUp = spaceBelow < 280 && spaceAbove > spaceBelow;
  }

  onSelectOption(option: SelectOption): void {
    if (option.disabled) return;

    this.value = option.value;
    this.onChange(this.value);
    this.selectionChange.emit(this.value);
    this.closeDropdown();
  }

  onClear(event: Event): void {
    event.stopPropagation();
    this.value = null;
    this.onChange(null);
    this.selectionChange.emit(null);
    this.cdr.markForCheck();
  }

  onSearchChange(): void {
    this.filterOptions();
  }

  clearSearch(): void {
    this.searchText = '';
    this.filterOptions();
    this.searchInputElement?.nativeElement.focus();
    this.cdr.markForCheck();
  }

  private filterOptions(): void {
    if (!this.searchText.trim()) {
      this.filteredOptions = [...this.options];
      return;
    }

    const rawQuery = this.searchText.trim().toLowerCase();
    const cleanQuery = removeVietnameseTones(rawQuery);

    this.filteredOptions = this.options.filter(opt => {
      const rawLabel = opt.label.toLowerCase();
      const cleanLabel = removeVietnameseTones(opt.label);
      const rawSub = (opt.sublabel || '').toLowerCase();
      const cleanSub = removeVietnameseTones(opt.sublabel || '');

      return (
        rawLabel.includes(rawQuery) ||
        cleanLabel.includes(cleanQuery) ||
        rawSub.includes(rawQuery) ||
        cleanSub.includes(cleanQuery)
      );
    });
  }

  @HostListener('keydown.escape')
  onEscape(): void {
    this.closeDropdown();
  }

  @HostListener('focusout', ['$event'])
  onFocusOut(event: FocusEvent): void {
    if (this.isOpen) {
      setTimeout(() => {
        if (!this.elementRef.nativeElement.contains(document.activeElement)) {
          this.closeDropdown();
        }
      }, 0);
    }
  }
}
