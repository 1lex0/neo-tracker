import { useState, type FormEvent } from 'react'

interface DateSearchFormProps {
  loading: boolean
  onSearch: (date: string) => void
}

export function DateSearchForm({ loading, onSearch }: DateSearchFormProps) {
  const [value, setValue] = useState('')
  const isEmpty = value.length === 0

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (isEmpty || loading) return
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
          value={value}
          onChange={(event) => setValue(event.target.value)}
        />
        <button type="submit" disabled={isEmpty || loading}>
          {loading ? 'Загрузка…' : 'Получить данные'}
        </button>
      </div>
    </form>
  )
}
