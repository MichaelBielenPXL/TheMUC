import { formatDay, formatTimestamp } from '@/lib/format'
import type { ImportDetail, ImportSummary } from '@/types'

import { useDeleteImport, useImports, useOpenImport } from '../api/imports'

type ImportHistoryProps = {
  onOpen: (detail: ImportDetail) => void
}

export function ImportHistory({ onOpen }: ImportHistoryProps) {
  const imports = useImports()
  const open = useOpenImport(onOpen)
  const remove = useDeleteImport()
  const actionError = open.error ?? remove.error

  function handleDelete(item: ImportSummary) {
    if (
      window.confirm(
        `Delete the import saved ${formatTimestamp(item.createdAt)}?`,
      )
    ) {
      remove.mutate(item.id)
    }
  }

  return (
    <section aria-labelledby="history-heading">
      <h2 id="history-heading">History</h2>

      {imports.isPending && (
        <p className="muted" role="status">
          Loading…
        </p>
      )}

      {imports.isError && (
        <div role="alert">
          <p className="error">{imports.error.message}</p>
          <div className="actions">
            <button type="button" onClick={() => imports.refetch()}>
              Try again
            </button>
          </div>
        </div>
      )}

      {imports.data?.length === 0 && (
        <p className="muted">Nothing saved yet.</p>
      )}

      {imports.data && imports.data.length > 0 && (
        <div className="table-scroll">
          <table>
            <thead>
              <tr>
                <th scope="col">Saved</th>
                <th scope="col">Dates</th>
                <th scope="col">Events</th>
                <th scope="col">Actions</th>
              </tr>
            </thead>
            <tbody>
              {imports.data.map((item) => (
                <tr key={item.id}>
                  <td>{formatTimestamp(item.createdAt)}</td>
                  <td>
                    <DateRange first={item.firstDate} last={item.lastDate} />
                  </td>
                  <td>{item.eventCount}</td>
                  <td>
                    <div className="actions">
                      <a
                        className="button"
                        href={`/api/imports/${item.id}/ics`}
                      >
                        Download
                      </a>
                      <button
                        type="button"
                        disabled={open.isPending}
                        onClick={() => open.mutate(item.id)}
                      >
                        Open
                      </button>
                      <button
                        type="button"
                        disabled={remove.isPending}
                        onClick={() => handleDelete(item)}
                      >
                        Delete
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {actionError && (
        <p className="error" role="alert">
          {actionError.message}
        </p>
      )}
    </section>
  )
}

function DateRange({ first, last }: { first: string; last: string }) {
  return first === last ? (
    <>{formatDay(first)}</>
  ) : (
    <>
      {formatDay(first)} – {formatDay(last)}
    </>
  )
}
