import { useMutation } from '@tanstack/react-query'

import { api } from '@/lib/api-client'
import type { ParseResult } from '@/types'

export function useParseMessage() {
  return useMutation({
    mutationFn: (text: string) =>
      api<ParseResult>('/events/parse', {
        method: 'POST',
        body: JSON.stringify({ text }),
      }),
  })
}
