import type { CalendarEvent, ParseSource } from '@/types'

/** An event while it is being edited: every field is a string an input can hold. */
export type DraftEvent = {
  key: number
  date: string
  title: string
  url: string
}

export type Draft = {
  /** The text these events were parsed from, saved with the import. */
  sourceText: string
  source: ParseSource
  skippedLines: string[]
  events: DraftEvent[]
}

export type DraftAction =
  | { type: 'loaded'; draft: Draft }
  | {
      type: 'changed'
      key: number
      patch: Partial<Omit<DraftEvent, 'key'>>
    }
  | { type: 'removed'; key: number }

const MAX_TITLE_LENGTH = 500

let nextKey = 0

export function toDraft(
  sourceText: string,
  source: ParseSource,
  skippedLines: string[],
  events: CalendarEvent[],
): Draft {
  return {
    sourceText,
    source,
    skippedLines,
    events: events
      .toSorted((a, b) => a.date.localeCompare(b.date))
      .map((event) => ({
        key: nextKey++,
        date: event.date,
        title: event.title,
        url: event.url ?? '',
      })),
  }
}

export function draftReducer(
  draft: Draft | null,
  action: DraftAction,
): Draft | null {
  if (action.type === 'loaded') return action.draft
  if (draft === null) return null

  switch (action.type) {
    case 'changed':
      return {
        ...draft,
        events: draft.events.map((event) =>
          event.key === action.key ? { ...event, ...action.patch } : event,
        ),
      }
    case 'removed':
      return {
        ...draft,
        events: draft.events.filter((event) => event.key !== action.key),
      }
  }
}

/** Returns what is wrong with the event, or null when it can be saved. */
export function validateEvent(event: DraftEvent): string | null {
  if (event.date === '') return 'Pick a date.'
  if (event.title.trim() === '') return 'Give the event a title.'
  if (event.title.length > MAX_TITLE_LENGTH) {
    return `A title can be at most ${MAX_TITLE_LENGTH} characters.`
  }
  if (event.url.trim() !== '' && !isWebUrl(event.url.trim())) {
    return 'A link must start with http:// or https://.'
  }
  return null
}

export function toCalendarEvents(events: DraftEvent[]): CalendarEvent[] {
  return events.map((event) => ({
    date: event.date,
    title: event.title.trim(),
    url: event.url.trim() === '' ? null : event.url.trim(),
  }))
}

function isWebUrl(value: string): boolean {
  if (/\s/.test(value)) return false
  try {
    const { protocol } = new URL(value)
    return protocol === 'http:' || protocol === 'https:'
  } catch {
    return false
  }
}
