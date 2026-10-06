import { useReducer, useState } from 'react'

import type { ImportDetail } from '@/types'

import { useParseMessage } from '../api/parse-message'
import { useSaveImport } from '../api/save-import'
import { draftReducer, toCalendarEvents, toDraft } from '../utils/draft'
import { EventEditor } from './EventEditor'
import { MessageForm } from './MessageForm'

type CalendarImportProps = {
  /** An import opened from history, or null to start empty. */
  initial: ImportDetail | null
}

export function CalendarImport({ initial }: CalendarImportProps) {
  const [text, setText] = useState(initial?.sourceText ?? '')
  const [draft, dispatch] = useReducer(draftReducer, initial, (opened) =>
    opened
      ? toDraft(opened.sourceText, opened.source, [], opened.events)
      : null,
  )
  const parse = useParseMessage()
  const save = useSaveImport()

  function handleParse() {
    save.reset()
    parse.mutate(text, {
      onSuccess: (result) =>
        dispatch({
          type: 'loaded',
          draft: toDraft(
            text,
            result.source,
            result.skippedLines,
            result.events,
          ),
        }),
    })
  }

  function handleSave() {
    if (draft === null) return

    save.mutate(
      {
        sourceText: draft.sourceText,
        source: draft.source,
        events: toCalendarEvents(draft.events),
      },
      {
        // The response is an attachment, so the browser downloads it and stays on this page.
        onSuccess: (saved) =>
          window.location.assign(`/api/imports/${saved.id}/ics`),
      },
    )
  }

  return (
    <>
      <MessageForm
        text={text}
        isPending={parse.isPending}
        error={parse.error?.message ?? null}
        onTextChange={setText}
        onParse={handleParse}
      />
      {draft && (
        <EventEditor
          draft={draft}
          isSaving={save.isPending}
          error={save.error?.message ?? null}
          dispatch={dispatch}
          onSave={handleSave}
        />
      )}
    </>
  )
}
