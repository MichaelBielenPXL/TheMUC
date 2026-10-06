export class ApiError extends Error {
  status: number

  constructor(message: string, status: number) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

export async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`/api${path}`, {
    ...init,
    headers: init?.body ? { 'Content-Type': 'application/json' } : undefined,
  })

  if (!response.ok) {
    throw new ApiError(await readError(response), response.status)
  }

  return response.status === 204 ? (undefined as T) : response.json()
}

async function readError(response: Response): Promise<string> {
  if (response.status === 429) {
    return 'Too many requests. Wait a minute and try again.'
  }

  try {
    // The backend answers with RFC 7807 problem details.
    const problem = await response.json()
    if (typeof problem.detail === 'string') return problem.detail
  } catch {
    // Not JSON: fall through to the generic message.
  }

  return `Something went wrong (${response.status}).`
}
