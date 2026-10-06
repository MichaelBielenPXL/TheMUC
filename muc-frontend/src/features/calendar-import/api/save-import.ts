import { useMutation, useQueryClient } from '@tanstack/react-query'

import { api } from '@/lib/api-client'
import { importsQueryKey } from '@/lib/query-client'
import type { CalendarEvent, ParseSource, SavedImport } from '@/types'

type SaveImportRequest = {
  sourceText: string
  source: ParseSource
  events: CalendarEvent[]
}

export function useSaveImport() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: SaveImportRequest) =>
      api<SavedImport>('/imports', {
        method: 'POST',
        body: JSON.stringify(request),
      }),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: importsQueryKey }),
  })
}
