import { useRef, useState } from 'react'
import { AsteroidApiError, fetchAsteroids, fetchLatestCached } from './api/asteroids'
import { AsteroidList } from './components/AsteroidList'
import { DateSearchForm } from './components/DateSearchForm'
import type { DailyResultResponse } from './models/asteroid'

type ViewState =
  | { kind: 'idle' }
  | { kind: 'loading'; date: string }
  | { kind: 'result'; date: string; data: DailyResultResponse }
  | { kind: 'error'; date: string; message: string }
  | {
      kind: 'unavailable'
      date: string
      cachedDate: string | null
      loadingCache: boolean
      cacheError: string | null
    }

const timestampFormat = new Intl.DateTimeFormat('ru-RU', {
  dateStyle: 'long',
  timeStyle: 'medium',
  timeZoneName: 'short',
})

function App() {
  const [state, setState] = useState<ViewState>({ kind: 'idle' })
  const pending = useRef<AbortController | null>(null)

  function startRequest() {
    pending.current?.abort()
    const controller = new AbortController()
    pending.current = controller
    return controller
  }

  async function search(date: string) {
    const controller = startRequest()
    setState({ kind: 'loading', date })

    try {
      const data = await fetchAsteroids(date, controller.signal)
      setState({ kind: 'result', date, data })
    } catch (error) {
      if (controller.signal.aborted) return
      if (error instanceof AsteroidApiError && error.code === 'upstream_unavailable') {
        // Cached data is only offered here; it is fetched once the user asks for it.
        setState({
          kind: 'unavailable',
          date,
          cachedDate: error.cachedFallbackAvailable ? error.cachedDate : null,
          loadingCache: false,
          cacheError: null,
        })
        return
      }
      setState({ kind: 'error', date, message: describeError(error) })
    }
  }

  async function showCached(date: string, cachedDate: string | null) {
    const controller = startRequest()
    setState({ kind: 'unavailable', date, cachedDate, loadingCache: true, cacheError: null })

    try {
      const data = await fetchLatestCached(date, controller.signal)
      setState({ kind: 'result', date, data })
    } catch (error) {
      if (controller.signal.aborted) return
      const cacheGone = error instanceof AsteroidApiError && error.code === 'cached_results_unavailable'
      setState({
        kind: 'unavailable',
        date,
        cachedDate: cacheGone ? null : cachedDate,
        loadingCache: false,
        cacheError: describeError(error),
      })
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
        {state.kind === 'unavailable' && (
          <div className="notice notice-error" role="alert">
            <strong>NASA NeoWs API сейчас недоступен — данные за {state.date} не получены.</strong>
            <p>Проверьте подключение к сети и повторите запрос позже.</p>
            {state.cacheError && <p>{state.cacheError}</p>}
            {state.cachedDate ? (
              <>
                <p>
                  В локальном кэше есть сохранённые данные за <strong>{state.cachedDate}</strong>. Это не ответ на ваш
                  запрос, а последний успешно полученный непустой день.
                </p>
                <button
                  type="button"
                  disabled={state.loadingCache}
                  onClick={() => showCached(state.date, state.cachedDate)}
                >
                  {state.loadingCache ? 'Загрузка…' : `Показать сохранённые данные за ${state.cachedDate}`}
                </button>
              </>
            ) : (
              <p>Сохранённых непустых результатов в кэше нет.</p>
            )}
          </div>
        )}
        {state.kind === 'result' && <ResultView requestedDate={state.date} data={state.data} />}
      </section>
    </main>
  )
}

function ResultView({ requestedDate, data }: { requestedDate: string; data: DailyResultResponse }) {
  if (data.asteroids.length === 0 || data.displayedDate === null) {
    return (
      <div className="notice notice-warning" role="status">
        <strong>За {requestedDate} астероиды не найдены.</strong>
        <p>Сохранённых непустых результатов в кэше тоже нет.</p>
      </div>
    )
  }

  return (
    <>
      {data.isFallback && <FallbackNotice requestedDate={requestedDate} data={data} />}
      <h2>
        {data.isFallback ? 'Сохранённые данные за ' : 'Астероиды за '}
        {data.displayedDate}
      </h2>
      <AsteroidList asteroids={data.asteroids} />
      <SourceFooter data={data} />
    </>
  )
}

function FallbackNotice({ requestedDate, data }: { requestedDate: string; data: DailyResultResponse }) {
  const reason =
    data.fallbackReason === 'no-results'
      ? `За запрошенную дату ${requestedDate} NASA не вернул ни одного астероида.`
      : `NASA NeoWs API недоступен, поэтому данные за ${requestedDate} не получены.`

  return (
    <div className="notice notice-warning" role="status">
      <strong>
        Внимание: показаны данные за {data.displayedDate}, а не за {requestedDate}.
      </strong>
      <p>
        {reason} Ниже — последний успешно полученный непустой день из локального кэша. Это не свежий ответ.
      </p>
    </div>
  )
}

function SourceFooter({ data }: { data: DailyResultResponse }) {
  return (
    <footer className="source">
      Источник: <strong>{data.source}</strong>
      {data.isFallback && ' (из локального кэша)'} · Время ответа:{' '}
      <time dateTime={data.receivedAtUtc}>{timestampFormat.format(new Date(data.receivedAtUtc))}</time>
    </footer>
  )
}

function describeError(error: unknown): string {
  if (error instanceof AsteroidApiError) {
    switch (error.code) {
      case 'invalid_date':
        return 'Введите существующую дату в формате ГГГГ-ММ-ДД.'
      case 'cached_results_unavailable':
        return 'Сохранённые данные больше недоступны.'
      case 'network_error':
        return 'Сервер приложения недоступен. Проверьте подключение и повторите попытку.'
      default:
        return error.message
    }
  }
  return 'Произошла непредвиденная ошибка. Повторите попытку.'
}

export default App
