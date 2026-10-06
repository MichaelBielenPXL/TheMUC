import type { Dispatch } from 'react'

import { type Draft, type DraftAction, validateEvent } from '../utils/draft'
import { EventRow } from './EventRow'

type EventEditorProps = {
  draft: Draft
  isSaving: boolean
  error: string | null
  dispatch: Dispatch<DraftAction>
  onSave: () => void
}

export function EventEditor({
  draft,
  isSaving,
  error,
  dispatch,
  onSave,
}: EventEditorProps) {
  const errors = new Map(
    draft.events.map((event) => [event.key, validateEvent(event)]),
  )
  const invalidCount = [...errors.values()].filter(Boolean).length
  const count = draft.events.length

  return (
    <section aria-labelledby="events-heading">
      <h2 id="events-heading">Events</h2>

      {count === 0 ? (
        <p className="muted" role="status">
          No events found. Put each date on its own line, with that day’s events
          on the lines below it.
        </p>
      ) : (
        <>
          <p className="muted" role="status">
            {count} {count === 1 ? 'event' : 'events'}
            {draft.source === 'llm' &&
              ' · read by AI because the usual format was not recognised — check them carefully'}
          </p>
          <ul className="event-list">
            {draft.events.map((event) => (
              <EventRow
                key={event.key}
                event={event}
                error={errors.get(event.key) ?? null}
                onChange={(patch) =>
                  dispatch({ type: 'changed', key: event.key, patch })
                }
                onRemove={() => dispatch({ type: 'removed', key: event.key })}
              />
            ))}
          </ul>
          <div className="actions">
            <button
              type="button"
              className="primary"
              disabled={isSaving || invalidCount > 0}
              onClick={onSave}
            >
              {isSaving ? 'Saving…' : 'Save & download .ics'}
            </button>
            {invalidCount > 0 && (
              <span className="muted">
                Fix{' '}
                {invalidCount === 1 ? 'the marked event' : 'the marked events'}{' '}
                first.
              </span>
            )}
          </div>
        </>
      )}

      {error && (
        <p className="error" role="alert">
          {error}
        </p>
      )}

      {draft.skippedLines.length > 0 && (
        <details>
          <summary>{draft.skippedLines.length} skipped lines</summary>
          <ul>
            {draft.skippedLines.map((line, index) => (
              <li key={index}>{line}</li>
            ))}
          </ul>
        </details>
      )}
    </section>
  )
}
