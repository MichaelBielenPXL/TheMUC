import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'

import { api } from '@/lib/api-client'
import { importsQueryKey } from '@/lib/query-client'
import type { ImportDetail, ImportSummary } from '@/types'

export function useImports() {
  return useQuery({
    queryKey: importsQueryKey,
    queryFn: () => api<ImportSummary[]>('/imports'),
  })
}

export function useOpenImport(onOpen: (detail: ImportDetail) => void) {
  return useMutation({
    mutationFn: (id: number) => api<ImportDetail>(`/imports/${id}`),
    onSuccess: onOpen,
  })
}

export function useDeleteImport() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: number) =>
      api<void>(`/imports/${id}`, { method: 'DELETE' }),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: importsQueryKey }),
  })
}
