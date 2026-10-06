import { CalendarImport } from '@/features/calendar-import'

export function App() {
  return (
    <main>
      <header>
        <h1>Mercurius Unified Calendar</h1>
        <p className="lead">
          Paste a weekly overview, check the events, download an .ics file.
        </p>
      </header>
      <CalendarImport initial={null} />
    </main>
  )
}
