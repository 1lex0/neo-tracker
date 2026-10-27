import { useState, type FormEvent } from 'react'
import { DATE_MESSAGES, validateDate } from '../validation/date'

interface DateSearchFormProps {
  loading: boolean
  onSearch: (date: string) => void
}

export function DateSearchForm({ loading, onSearch }: DateSearchFormProps) {
  const [value, setValue] = useState('')
  // Format and calendar errors appear after the first attempt, then update as the user types.
  const [attempted, setAttempted] = useState(false)

  const isEmpty = value.length === 0
  const validation = validateDate(value)
  const errorMessage = attempted && !validation.valid && !isEmpty ? validation.message : null

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setAttempted(true)
    if (isEmpty || loading || !validation.valid) return
    onSearch(value)
  }

  return (
    <form className="search" onSubmit={handleSubmit} noValidate>
      <label htmlFor="date-input">Дата сближения</label>
      <div className="search-row">
        <input
          id="date-input"
          type="text"
          inputMode="numeric"
          placeholder="ГГГГ-ММ-ДД"
          autoComplete="off"
          required
          value={value}
          aria-invalid={errorMessage !== null}
          aria-describedby="date-message"
          onChange={(event) => setValue(event.target.value)}
        />
        <button type="submit" disabled={isEmpty || loading}>
          {loading ? 'Загрузка…' : 'Получить данные'}
        </button>
      </div>
      <div id="date-message" aria-live="polite">
        {isEmpty && <p className="field-hint">{DATE_MESSAGES.required}</p>}
        {errorMessage && (
          <p className="field-message" role="alert">
            {errorMessage}
          </p>
        )}
      </div>
    </form>
  )
}
