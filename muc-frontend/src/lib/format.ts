const dayFormat = new Intl.DateTimeFormat('nl-BE', {
  weekday: 'short',
  day: 'numeric',
  month: 'short',
  year: 'numeric',
})

const timestampFormat = new Intl.DateTimeFormat('nl-BE', {
  dateStyle: 'medium',
  timeStyle: 'short',
})

/** Formats an ISO date (yyyy-MM-dd) without shifting it across a time zone. */
export function formatDay(isoDate: string): string {
  return dayFormat.format(new Date(`${isoDate}T00:00:00`))
}

export function formatTimestamp(iso: string): string {
  return timestampFormat.format(new Date(iso))
}
