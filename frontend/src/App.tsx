import { useRef, useState } from 'react'
import { AsteroidApiError, fetchAsteroids } from './api/asteroids'
import { AsteroidList } from './components/AsteroidList'
import { DateSearchForm } from './components/DateSearchForm'
import type { DailyResultResponse } from './models/asteroid'

type ViewState =
  | { kind: 'idle' }
  | { kind: 'loading'; date: string }
  | { kind: 'result'; data: DailyResultResponse }
  | { kind: 'error'; date: string; message: string }

const timestampFormat = new Intl.DateTimeFormat('ru-RU', {
  dateStyle: 'long',
  timeStyle: 'medium',
  timeZoneName: 'short',
})

function App() {
  const [state, setState] = useState<ViewState>({ kind: 'idle' })
  const pending = useRef<AbortController | null>(null)

  async function search(date: string) {
    pending.current?.abort()
    const controller = new AbortController()
    pending.current = controller
    setState({ kind: 'loading', date })

    try {
      const data = await fetchAsteroids(date, controller.signal)
      setState({ kind: 'result', data })
    } catch (error) {
      if (controller.signal.aborted) return
      setState({ kind: 'error', date, message: describeError(error) })
    }
  }

  return (
    <main className="app">
      <header className="app-header">
        <h1>NASA NEO Tracker</h1>
        <p className="subtitle">Астероиды, сближающиеся с Землёй в выбранную дату</p>
      </header>

      <DateSearchForm loading={state.kind === 'loading'} onSearch={search} />

      <section className="results" aria-live="polite">
        {state.kind === 'loading' && <p className="status">Запрашиваем данные за {state.date}…</p>}
        {state.kind === 'error' && (
          <div className="notice notice-error" role="alert">
            <strong>Не удалось получить данные за {state.date}.</strong>
            <p>{state.message}</p>
          </div>
        )}
        {state.kind === 'result' && <ResultView data={state.data} />}
      </section>
    </main>
  )
}

function ResultView({ data }: { data: DailyResultResponse }) {
  if (data.asteroids.length === 0 || data.displayedDate === null) {
    return (
      <div className="notice notice-warning" role="status">
        <strong>За {data.requestedDate} астероиды не найдены.</strong>
        <p>Сохранённых непустых результатов в кэше тоже нет.</p>
      </div>
    )
  }

  return (
    <>
      <h2>Астероиды за {data.displayedDate}</h2>
      <AsteroidList asteroids={data.asteroids} />
      <SourceFooter data={data} />
    </>
  )
}

function SourceFooter({ data }: { data: DailyResultResponse }) {
  return (
    <footer className="source">
      Источник: <strong>{data.source}</strong> · Время ответа:{' '}
      <time dateTime={data.receivedAtUtc}>{timestampFormat.format(new Date(data.receivedAtUtc))}</time>
    </footer>
  )
}

function describeError(error: unknown): string {
  if (error instanceof AsteroidApiError) {
    switch (error.code) {
      case 'invalid_date':
        return 'Введите существующую дату в формате ГГГГ-ММ-ДД.'
      case 'network_error':
        return 'Сервер приложения недоступен. Проверьте подключение и повторите попытку.'
      default:
        return error.message
    }
  }
  return 'Произошла непредвиденная ошибка. Повторите попытку.'
}

export default App
