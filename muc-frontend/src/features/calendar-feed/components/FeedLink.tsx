import { useRef, useState } from 'react'

// Set at build time for the deployment, where the feed has its own public hostname.
// In development the dev server proxies the feed, so it lives next to the page.
const feedUrl: string =
  import.meta.env.VITE_CALENDAR_URL || `${window.location.origin}/calendar.ics`

// Calendar apps register this scheme, so the link opens their "subscribe" dialog.
const subscribeUrl = feedUrl.replace(/^https?:/, 'webcal:')

export function FeedLink() {
  const [copied, setCopied] = useState(false)
  const input = useRef<HTMLInputElement>(null)

  async function handleCopy() {
    try {
      await navigator.clipboard.writeText(feedUrl)
      setCopied(true)
    } catch {
      // No clipboard access: leave the address selected so it can be copied by hand.
      input.current?.select()
    }
  }

  return (
    <section className="feed" aria-labelledby="feed-heading">
      <h2 id="feed-heading">Calendar link</h2>
      <p className="muted">
        Subscribe once with this link and new events appear in your calendar on
        their own.
      </p>
      <div className="feed-row">
        <input
          ref={input}
          type="text"
          readOnly
          aria-label="Calendar link"
          value={feedUrl}
          onFocus={(event) => event.target.select()}
        />
        <button type="button" onClick={handleCopy}>
          {copied ? 'Copied' : 'Copy'}
        </button>
        <a className="button" href={subscribeUrl}>
          Subscribe
        </a>
      </div>
      <span className="visually-hidden" role="status">
        {copied ? 'Calendar link copied.' : ''}
      </span>
    </section>
  )
}
