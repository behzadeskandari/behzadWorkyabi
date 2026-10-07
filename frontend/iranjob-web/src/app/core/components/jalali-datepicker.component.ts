import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { JalaliDateService, PERSIAN_MONTH_NAMES, PERSIAN_DAY_NAMES } from '../services/jalali-date.service';

@Component({
  selector: 'app-jalali-datepicker',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="jalali-datepicker" [class.focused]="isOpen">
      <div class="jalali-datepicker__input-wrapper">
        <input type="text" class="jalali-datepicker__input"
               [value]="displayValue" [placeholder]="placeholder"
               [disabled]="disabled" (focus)="open()" (click)="open()"
               (keydown)="onKeydown($event)" (input)="onInputChange($event)"
               autocomplete="off" readonly />
        <button type="button" class="jalali-datepicker__calendar-btn" [disabled]="disabled" (click)="toggle()">
          📅
        </button>
      </div>

      <div *ngIf="isOpen" class="jalali-datepicker__calendar" (click)="$event.stopPropagation()">
        <div class="jalali-datepicker__header">
          <button type="button" class="jalali-datepicker__nav" (click)="prevMonth()" aria-label="ماه قبل">‹</button>
          <span class="jalali-datepicker__month-year">{{ PERSIAN_MONTH_NAMES[currentMonth] }} {{ toPersian(currentYear) }}</span>
          <button type="button" class="jalali-datepicker__nav" (click)="nextMonth()" aria-label="ماه بعد">›</button>
        </div>

        <div class="jalali-datepicker__weekdays">
          <span *ngFor="let day of weekdayNames" class="jalali-datepicker__weekday">{{ day }}</span>
        </div>

        <div class="jalali-datepicker__days">
          <button *ngFor="let day of days" type="button" class="jalali-datepicker__day"
                  [class.selected]="day.isSelected" [class.other-month]="day.isOtherMonth"
                  [class.today]="day.isToday" [disabled]="day.isOtherMonth" (click)="selectDay(day)">
            {{ toPersian(day.day) }}
          </button>
        </div>

        <div class="jalali-datepicker__footer">
          <button type="button" class="jalali-datepicker__today-btn" (click)="goToday()">امروز</button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .jalali-datepicker { position: relative; direction: rtl; font-family: inherit; }
    .jalali-datepicker__input-wrapper {
      display: flex; align-items: stretch;
      border: 1px solid #aebdc2; border-radius: 6px; background: #fff; overflow: hidden;
    }
    .jalali-datepicker__input {
      flex: 1; border: none; outline: none; padding: 0.55rem 0.7rem; font: inherit;
      font-size: 0.95rem; background: transparent; direction: ltr; text-align: left;
    }
    .jalali-datepicker__calendar-btn {
      display: flex; align-items: center; justify-content: center; width: 2.5rem;
      border: none; background: #f1f5f4; cursor: pointer; font-size: 1.1rem; transition: background 0.2s;
    }
    .jalali-datepicker__calendar-btn:hover:not(:disabled) { background: #e9f5f2; }
    .jalali-datepicker__calendar-btn:disabled { opacity: 0.5; cursor: not-allowed; }
    .jalali-datepicker__calendar {
      position: absolute; top: 100%; left: 0; right: 0; z-index: 1000;
      background: #fff; border: 1px solid #aebdc2; border-radius: 8px;
      box-shadow: 0 10px 30px rgba(0,0,0,0.15); padding: 0.5rem; margin-top: 0.25rem;
    }
    .jalali-datepicker__header { display: flex; align-items: center; justify-content: space-between; padding: 0.5rem 0.25rem; }
    .jalali-datepicker__month-year { font-weight: 600; font-size: 1rem; }
    .jalali-datepicker__nav {
      background: #f1f5f4; border: none; border-radius: 4px; width: 2rem; height: 2rem;
      font-size: 1.1rem; cursor: pointer; display: flex; align-items: center; justify-content: center;
    }
    .jalali-datepicker__nav:hover { background: #e9f5f2; }
    .jalali-datepicker__weekdays { display: grid; grid-template-columns: repeat(7, 1fr); gap: 0.25rem; padding: 0.25rem 0; }
    .jalali-datepicker__weekday { text-align: center; font-size: 0.75rem; font-weight: 600; color: #6b7d7d; padding: 0.2rem 0; }
    .jalali-datepicker__days { display: grid; grid-template-columns: repeat(7, 1fr); gap: 0.2rem; }
    .jalali-datepicker__day {
      aspect-ratio: 1/1; border: none; background: transparent; border-radius: 4px; font-size: 0.85rem;
      cursor: pointer; transition: all 0.15s; padding: 0; display: flex; align-items: center; justify-content: center;
    }
    .jalali-datepicker__day:hover:not(:disabled):not(.other-month) { background: #e9f5f2; }
    .jalali-datepicker__day.selected { background: #087c79; color: white; }
    .jalali-datepicker__day.other-month { visibility: hidden; }
    .jalali-datepicker__day.today:not(.selected) { border: 1px solid #087c79; color: #087c79; }
    .jalali-datepicker__day:disabled { cursor: default; visibility: hidden; }
    .jalali-datepicker__footer { padding: 0.25rem 0 0; text-align: center; }
    .jalali-datepicker__today-btn { background: #f1f5f4; border: none; border-radius: 4px; padding: 0.25rem 0.6rem; font-size: 0.8rem; cursor: pointer; }
    .jalali-datepicker__today-btn:hover { background: #e9f5f2; }
    @media (max-width: 480px) {
      .jalali-datepicker__calendar { max-height: 300px; overflow-y: auto; }
      .jalali-datepicker__day { font-size: 0.75rem; }
    }
  `]
})
export class JalaliDatepickerComponent {
  @Input() value: string | null = null;
  @Input() placeholder = 'مثال: ۱۴۰۵/۰۷/۱۴';
  @Input() disabled = false;
  @Output() readonly valueChange = new EventEmitter<string | null>();

  private readonly jalaliService = inject(JalaliDateService);

  PERSIAN_MONTH_NAMES = PERSIAN_MONTH_NAMES;
  weekdayNames = PERSIAN_DAY_NAMES;

  isOpen = false;
  currentYear: number;
  currentMonth: number;
  days: Array<{ day: number; isOtherMonth: boolean; isSelected: boolean; isToday: boolean }> = [];

  constructor() {
    const today = this.jalaliService.today();
    this.currentYear = today.year;
    this.currentMonth = today.month;
  }

  get displayValue(): string {
    if (this.value) {
      return this.jalaliService.fromPersianDigits(this.value);
    }
    return '';
  }

  open(): void {
    if (this.disabled) return;
    this.isOpen = true;
    this.renderDays();
  }

  toggle(): void {
    if (this.disabled) return;
    this.isOpen = !this.isOpen;
    if (this.isOpen) this.renderDays();
  }

  close(): void {
    this.isOpen = false;
  }

  prevMonth(): void {
    this.currentMonth--;
    if (this.currentMonth < 1) {
      this.currentMonth = 12;
      this.currentYear--;
    }
    this.renderDays();
  }

  nextMonth(): void {
    this.currentMonth++;
    if (this.currentMonth > 12) {
      this.currentMonth = 1;
      this.currentYear++;
    }
    this.renderDays();
  }

  goToday(): void {
    const today = this.jalaliService.today();
    this.currentYear = today.year;
    this.currentMonth = today.month;
    this.renderDays();
  }

  selectDay(day: { day: number; isOtherMonth: boolean; isSelected: boolean; isToday: boolean }): void {
    if (day.isOtherMonth) return;
    const yStr = this.toPersian(this.currentYear);
    const mStr = this.toPersian(this.currentMonth).padStart(2, '۰');
    const dStr = this.toPersian(day.day).padStart(2, '۰');
    const persianFormatted = `${yStr}/${mStr}/${dStr}`;
    this.value = persianFormatted;
    this.valueChange.emit(persianFormatted);
    this.close();
  }

        onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      this.close();
    }
  }

  onInputChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const val = this.jalaliService.fromPersianDigits(input.value.trim());
    if (val && /^\d{4}\/\d{2}\/\d{2}$/.test(val)) {
      this.value = input.value;
      this.valueChange.emit(input.value);
    }
  }

  toPersian(num: number): string {
    return this.jalaliService.toPersianDigits(num.toString());
  }

    private renderDays(): void {
    const today = this.jalaliService.today();
    const daysInMonth = this.jalaliService.daysInJalaliMonth(this.currentYear, this.currentMonth);
    const gregDate = this.jalaliService.fromJalali(this.currentYear, this.currentMonth, 1);
    // Convert Gregorian getDay() (0=Sun) to Jalali day-of-week (0=Sat)
    const firstDay = (gregDate.getDay() + 1) % 7;

    this.days = [];
    for (let i = 0; i < firstDay; i++) {
      this.days.push({ day: 0, isOtherMonth: true, isSelected: false, isToday: false });
    }
    for (let day = 1; day <= daysInMonth; day++) {
      const isToday = today.year === this.currentYear && today.month === this.currentMonth && today.day === day;
      this.days.push({ day, isOtherMonth: false, isSelected: false, isToday });
    }
    const totalSlots = Math.ceil((firstDay + daysInMonth) / 7) * 7;
    const remaining = totalSlots - (firstDay + daysInMonth);
    for (let i = 1; i <= remaining; i++) {
      this.days.push({ day: i, isOtherMonth: true, isSelected: false, isToday: false });
    }
  }
}
