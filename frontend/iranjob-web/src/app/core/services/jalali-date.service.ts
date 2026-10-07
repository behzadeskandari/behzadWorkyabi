import { Injectable } from '@angular/core';

/**
 * Persian month names (Jalali calendar) indexed 1-12.
 */
export const PERSIAN_MONTH_NAMES: string[] = [
  '', 'فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور',
  'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'
];

/**
 * Persian day-of-week names (Saturday = 0 … Friday = 6).
 */
export const PERSIAN_DAY_NAMES: string[] = [
  'شنبه', 'یکشنبه', 'دوشنبه', 'سه‌شنبه', 'چهارشنبه', 'پنجشنبه', 'جمعه'
];

const PERSIAN_DIGITS = ['۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹'];

export interface JalaliDateParts {
  year: number;
  month: number;
  day: number;
  hour: number;
  minute: number;
  second: number;
}

export interface JalaliMonthInfo {
  year: number;
  month: number;
  daysInMonth: number;
  isLeapYear: boolean;
}

@Injectable({ providedIn: 'root' })
export class JalaliDateService {

  /** Convert a native JavaScript Date (Gregorian) to Jalali parts. */
  toJalali(date: Date): JalaliDateParts {
    const j = this.gregorianToJalali(date.getFullYear(), date.getMonth() + 1, date.getDate());
    return {
      year: j[0],
      month: j[1],
      day: j[2],
      hour: date.getHours(),
      minute: date.getMinutes(),
      second: date.getSeconds()
    };
  }

  /** Convert Jalali year/month/day to a Gregorian Date (midnight local). */
  fromJalali(year: number, month: number, day: number): Date {
    const g = this.jalaliToGregorian(year, month, day);
    return new Date(g[0], g[1] - 1, g[2]);
  }

  /** Convert a Gregorian date to a Jalali date string: "1405/07/14". */
  toJalaliString(date: Date | string): string {
    const d = date instanceof Date ? date : new Date(date);
    const j = this.toJalali(d);
    const y = String(j.year).padStart(4, '0');
    const m = String(j.month).padStart(2, '0');
    const d2 = String(j.day).padStart(2, '0');
    return y + '/' + m + '/' + d2;
  }

  /** Format a date for display as a Persian date string. */
  formatDisplay(date: Date | string): string {
    const d = date instanceof Date ? date : new Date(date);
    const j = this.toJalali(d);
    return this.toPersianDigits(j.day.toString()) + " " + PERSIAN_MONTH_NAMES[j.month] + " " + this.toPersianDigits(j.year.toString());
  }

  /** Format a date/time for display as a Persian datetime string. */
  formatDisplayDateTime(date: Date | string): string {
    const d = date instanceof Date ? date : new Date(date);
    const j = this.toJalali(d);
    const datePart = this.formatDisplay(d);
    const hour = this.toPersianDigits(String(j.hour).padStart(2, '0'));
    const minute = this.toPersianDigits(String(j.minute).padStart(2, '0'));
    return datePart + "، ساعت " + hour + ":" + minute;
  }

  /** Convert ASCII digits to Persian digits. */
  toPersianDigits(str: string): string {
    return str.replace(/\d/g, d => PERSIAN_DIGITS[Number(d)]);
  }

  /** Convert Persian digits back to ASCII. */
  fromPersianDigits(str: string): string {
    const map: Record<string, string> = {};
    for (let i = 0; i < 10; i++) {
      map[PERSIAN_DIGITS[i]] = i.toString();
    }
    return str.replace(/[۰-۹]/g, ch => map[ch] ?? ch);
  }

  /** Parse a Jalali date string "YYYY/MM/DD" into a Gregorian Date. */
  parseJalaliDate(jalaliStr: string): Date | null {
    const cleaned = this.fromPersianDigits(jalaliStr.trim());
    const parts = cleaned.split('/');
    if (parts.length !== 3) return null;
    const year = Number(parts[0]);
    const month = Number(parts[1]);
    const day = Number(parts[2]);
    if (isNaN(year) || isNaN(month) || isNaN(day)) return null;
    if (month < 1 || month > 12) return null;
    if (day < 1) return null;
    if (day > this.daysInJalaliMonth(year, month)) return null;
    return this.fromJalali(year, month, day);
  }

  /** Return the number of days in a Jalali month. */
  daysInJalaliMonth(year: number, month: number): number {
    if (month < 1 || month > 12) return 0;
    if (month <= 6) return 31;
    if (month <= 11) return 30;
    return this.isJalaliLeapYear(year) ? 30 : 29;
  }

  /** Build month information for a Jalali calendar month view. */
  getMonthInfo(year: number, month: number): JalaliMonthInfo {
    return {
      year,
      month,
      daysInMonth: this.daysInJalaliMonth(year, month),
      isLeapYear: this.isJalaliLeapYear(year)
    };
  }

  /** Whether the given Jalali year is a leap year (33-year cycle). */
  isJalaliLeapYear(year: number): boolean {
    const r = (year - 1) % 33;
    return r === 0 || r === 4 || r === 8 || r === 12 || r === 16 || r === 21 || r === 25 || r === 29;
  }

  /** Get today's date in Jalali parts. */
  today(): JalaliDateParts {
    return this.toJalali(new Date());
  }

  // ---- Core calendar conversion algorithms ----

  private static readonly JALALI_EPOCH = 1948320;
  private static readonly CYCLE_DAYS = 12053;

  private jalaliYearStartDays(jy: number): number {
    const cycles = Math.floor((jy - 1) / 33);
    let days = cycles * JalaliDateService.CYCLE_DAYS;
    const remaining = (jy - 1) % 33;
    for (let y = 0; y < remaining; y++) {
      const absYear = cycles * 33 + y + 1;
      days += this.isJalaliLeapYear(absYear) ? 366 : 365;
    }
    return days;
  }

  private jalaliToGregorian(jy: number, jm: number, jd: number): [number, number, number] {
    const monthLengths = this.monthLengthsForYear(jy);
    let dayOfYear = jd;
    for (let i = 1; i < jm; i++) {
      dayOfYear += monthLengths[i - 1];
    }
    const daysSinceEpoch = this.jalaliYearStartDays(jy) + dayOfYear - 1;
    const jdn = JalaliDateService.JALALI_EPOCH + daysSinceEpoch;
    return this.jdnToGregorian(jdn);
  }

  private gregorianToJalali(gy: number, gm: number, gd: number): [number, number, number] {
    const jdn = this.gregorianToJDN(gy, gm, gd);
    return this.jdnToJalali(jdn);
  }

  private gregorianToJDN(gy: number, gm: number, gd: number): number {
    const a = Math.floor((14 - gm) / 12);
    const y = gy + 4800 - a;
    const m = gm + 12 * a - 3;
    return gd + Math.floor((153 * m + 2) / 5) + 365 * y
      + Math.floor(y / 4) - Math.floor(y / 100) + Math.floor(y / 400) - 32045;
  }

  private jdnToGregorian(jdn: number): [number, number, number] {
    const a = jdn + 32044;
    const b = Math.floor((4 * a + 3) / 146097);
    const c = a - Math.floor((146097 * b) / 4);
    const d = Math.floor((4 * c + 3) / 1461);
    const e = c - Math.floor((1461 * d) / 4);
    const m = Math.floor((5 * e + 2) / 153);
    const day = e - Math.floor((153 * m + 2) / 5) + 1;
    const month = m + 3 - 12 * Math.floor(m / 10);
    const year = 100 * b + d - 4800 + Math.floor(m / 10);
    return [year, month, day];
  }

  private jdnToJalali(jdn: number): [number, number, number] {
    const daysSinceEpoch = jdn - JalaliDateService.JALALI_EPOCH;
    const cycles = Math.floor(daysSinceEpoch / JalaliDateService.CYCLE_DAYS);
    let jy = cycles * 33 + 1;
    let remainingDays = daysSinceEpoch - cycles * JalaliDateService.CYCLE_DAYS;

    while (true) {
      const yearDays = this.isJalaliLeapYear(jy) ? 366 : 365;
      if (remainingDays < yearDays) break;
      remainingDays -= yearDays;
      jy++;
    }

    const monthLengths = this.monthLengthsForYear(jy);
    let jm = 1;
    while (jm <= 12 && remainingDays >= monthLengths[jm - 1]) {
      remainingDays -= monthLengths[jm - 1];
      jm++;
    }
    const jd = remainingDays + 1;
    return [jy, jm, jd];
  }

  private monthLengthsForYear(jy: number): number[] {
    const base = [31, 31, 31, 31, 31, 31, 30, 30, 30, 30, 30, 29];
    if (this.isJalaliLeapYear(jy)) {
      base[11] = 30;
    }
    return base;
  }
}

