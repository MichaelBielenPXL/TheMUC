export type CalendarEvent = {
  /** ISO date, yyyy-MM-dd */
  date: string
  title: string
  url: string | null
}

export type ParseSource = 'rules' | 'llm'

export type ParseResult = {
  events: CalendarEvent[]
  skippedLines: string[]
  source: ParseSource
}

export type SavedImport = {
  id: number
  createdAt: string
  eventCount: number
}

export type ImportSummary = {
  id: number
  createdAt: string
  eventCount: number
  firstDate: string
  lastDate: string
  source: ParseSource
}

export type ImportDetail = {
  id: number
  createdAt: string
  source: ParseSource
  sourceText: string
  events: CalendarEvent[]
}
