import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  Output,
  computed,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import {
  DailyPricingCellDto,
  MonthlyPricingCalendarDto,
} from '../../../../services/manager/manager-rate.service';
import {
  BhSearchableSelectComponent,
  SelectOption,
} from '../../../../../shared/components/searchable-select/bh-searchable-select.component';

export interface CalendarSlot {
  isPadding: boolean;
  cell?: DailyPricingCellDto;
}

@Component({
  selector: 'bh-pricing-calendar',
  standalone: true,
  imports: [CommonModule, FormsModule, LocalizationPipe, BhSearchableSelectComponent],
  templateUrl: './pricing-calendar.component.html',
  styleUrls: ['./pricing-calendar.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PricingCalendarComponent {
  private readonly calendarSignal = signal<MonthlyPricingCalendarDto | null>(null);

  @Input() set calendar(value: MonthlyPricingCalendarDto | null) {
    this.calendarSignal.set(value);
  }
  get calendar(): MonthlyPricingCalendarDto | null {
    return this.calendarSignal();
  }

  @Input() year = new Date().getFullYear();
  @Input() month = new Date().getMonth() + 1;
  @Input() isLoading = false;

  @Output() monthChange = new EventEmitter<{ year: number; month: number }>();
  @Output() daySelected = new EventEmitter<DailyPricingCellDto>();

  readonly weekdayKeys = [
    'Manager:Rates:DayMon',
    'Manager:Rates:DayTue',
    'Manager:Rates:DayWed',
    'Manager:Rates:DayThu',
    'Manager:Rates:DayFri',
    'Manager:Rates:DaySat',
    'Manager:Rates:DaySun',
  ];

  readonly monthOptions: SelectOption[] = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12].map(m => ({
    value: m,
    label: m < 10 ? `0${m}` : `${m}`,
    icon: 'fa-regular fa-calendar',
  }));

  readonly yearOptions = computed<SelectOption[]>(() => {
    const current = new Date().getFullYear();
    return [current - 1, current, current + 1, current + 2].map(y => ({
      value: y,
      label: `${y}`,
    }));
  });

  readonly calendarSlots = computed<CalendarSlot[]>(() => {
    const cal = this.calendarSignal();
    if (!cal || !cal.days || cal.days.length === 0) return [];

    const firstDate = new Date(cal.year, cal.month - 1, 1);
    // Monday-first index (Mon=0 .. Sun=6)
    const leadingPadding = (firstDate.getDay() + 6) % 7;

    const slots: CalendarSlot[] = [];
    for (let i = 0; i < leadingPadding; i++) {
      slots.push({ isPadding: true });
    }
    for (const cell of cal.days) {
      slots.push({ isPadding: false, cell });
    }
    while (slots.length % 7 !== 0) {
      slots.push({ isPadding: true });
    }
    return slots;
  });

  goPrevMonth(): void {
    let m = this.month - 1;
    let y = this.year;
    if (m < 1) {
      m = 12;
      y -= 1;
    }
    this.monthChange.emit({ year: y, month: m });
  }

  goNextMonth(): void {
    let m = this.month + 1;
    let y = this.year;
    if (m > 12) {
      m = 1;
      y += 1;
    }
    this.monthChange.emit({ year: y, month: m });
  }

  goToday(): void {
    const now = new Date();
    this.monthChange.emit({ year: now.getFullYear(), month: now.getMonth() + 1 });
  }

  onMonthSelect(monthValue: number): void {
    if (!monthValue) return;
    this.monthChange.emit({ year: this.year, month: Number(monthValue) });
  }

  onYearSelect(yearValue: number): void {
    if (!yearValue) return;
    this.monthChange.emit({ year: Number(yearValue), month: this.month });
  }

  onCellClick(cell: DailyPricingCellDto): void {
    this.daySelected.emit(cell);
  }

  getDayNumber(isoDate: string): number {
    const parts = isoDate.substring(0, 10).split('-');
    return Number(parts[2] || 0);
  }
}
