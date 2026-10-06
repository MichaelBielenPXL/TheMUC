import type { DraftEvent } from '../utils/draft'

type EventRowProps = {
  event: DraftEvent
  error: string | null
  onChange: (patch: Partial<Omit<DraftEvent, 'key'>>) => void
  onRemove: () => void
}

export function EventRow({ event, error, onChange, onRemove }: EventRowProps) {
  const name = event.title.trim() || 'untitled event'
  const errorId = `event-${event.key}-error`

  return (
    <li className="event-row">
      <input
        type="date"
        aria-label={`Date of ${name}`}
        aria-invalid={error !== null}
        aria-describedby={error ? errorId : undefined}
        value={event.date}
        onChange={(e) => onChange({ date: e.target.value })}
      />
      <input
        type="text"
        aria-label={`Title of ${name}`}
        aria-invalid={error !== null}
        aria-describedby={error ? errorId : undefined}
        value={event.title}
        onChange={(e) => onChange({ title: e.target.value })}
      />
      <input
        type="url"
        aria-label={`Link of ${name}`}
        aria-invalid={error !== null}
        aria-describedby={error ? errorId : undefined}
        placeholder="No link"
        value={event.url}
        onChange={(e) => onChange({ url: e.target.value })}
      />
      <button type="button" aria-label={`Remove ${name}`} onClick={onRemove}>
        Remove
      </button>
      {error && (
        <p id={errorId} className="error">
          {error}
        </p>
      )}
    </li>
  )
}
