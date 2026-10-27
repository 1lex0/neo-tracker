// Client-side mirror of NeoTracker.Core.Validation.SelectedDateValidator. It only spares
// the user a round trip; the backend validator remains authoritative.

export type DateValidationError = 'required' | 'format' | 'impossible'

export type DateValidationResult = { valid: true } | { valid: false; error: DateValidationError; message: string }

const EXACT_DATE = /^([0-9]{4})-([0-9]{2})-([0-9]{2})$/

export const DATE_MESSAGES: Record<DateValidationError, string> = {
  required: 'Введите дату — это обязательное поле.',
  format: 'Неверный формат. Используйте ГГГГ-ММ-ДД, например 2026-10-27.',
  impossible: 'Такой даты не существует. Проверьте месяц и число (например, 30 февраля не бывает).',
}

export function validateDate(input: string): DateValidationResult {
  if (input.trim().length === 0) return invalid('required')

  const match = EXACT_DATE.exec(input)
  if (!match) return invalid('format')

  const [year, month, day] = match.slice(1).map(Number)
  if (year < 1 || month < 1 || month > 12 || day < 1 || day > daysInMonth(year, month)) return invalid('impossible')

  return { valid: true }
}

function daysInMonth(year: number, month: number): number {
  if (month === 2) return isLeapYear(year) ? 29 : 28
  return [4, 6, 9, 11].includes(month) ? 30 : 31
}

function isLeapYear(year: number): boolean {
  return (year % 4 === 0 && year % 100 !== 0) || year % 400 === 0
}

function invalid(error: DateValidationError): DateValidationResult {
  return { valid: false, error, message: DATE_MESSAGES[error] }
}
