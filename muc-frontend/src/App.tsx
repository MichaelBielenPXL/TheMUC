import { useState } from 'react'

import { CalendarImport } from '@/features/calendar-import'
import { ImportHistory } from '@/features/import-history'
import type { ImportDetail } from '@/types'

export function App() {
  const [opened, setOpened] = useState<ImportDetail | null>(null)
  // Bumped on every Open so the editor remounts even when the same import is opened twice.
  const [openCount, setOpenCount] = useState(0)

  function handleOpen(detail: ImportDetail) {
    setOpened(detail)
    setOpenCount((count) => count + 1)
    window.scrollTo({ top: 0 })
  }

  return (
    <main>
      <header>
        <h1>Mercurius Unified Calendar</h1>
        <p className="lead">
          Paste a weekly overview, check the events, download an .ics file.
        </p>
      </header>
      <CalendarImport key={openCount} initial={opened} />
      <ImportHistory onOpen={handleOpen} />
    </main>
  )
}
