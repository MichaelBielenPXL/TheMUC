import type { FormEvent } from 'react'

type MessageFormProps = {
  text: string
  isPending: boolean
  error: string | null
  onTextChange: (text: string) => void
  onParse: () => void
}

export function MessageForm({
  text,
  isPending,
  error,
  onTextChange,
  onParse,
}: MessageFormProps) {
  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    onParse()
  }

  return (
    <section aria-labelledby="message-heading">
      <h2 id="message-heading">Message</h2>
      <form onSubmit={handleSubmit}>
        <label htmlFor="message">Paste the weekly overview</label>
        <textarea
          id="message"
          value={text}
          maxLength={20000}
          placeholder={'Maandag 28 september\nFakbar - Dionysia\nhttps://…'}
          onChange={(event) => onTextChange(event.target.value)}
        />
        <div className="actions">
          <button
            type="submit"
            className="primary"
            disabled={isPending || text.trim() === ''}
          >
            {isPending ? 'Parsing…' : 'Parse'}
          </button>
        </div>
        {error && (
          <p className="error" role="alert">
            {error}
          </p>
        )}
      </form>
    </section>
  )
}
