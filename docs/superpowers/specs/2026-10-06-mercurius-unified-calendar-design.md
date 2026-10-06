# Mercurius Unified Calendar — design

Date: 2026-10-06

## Goal

Paste a weekly-overview chat message listing student events, preview the events
that were recognised, and download an `.ics` file that imports into any
calendar.

Success: the three sample messages in `muc-backend.tests/Samples/` each produce
exactly the events a human reads in them, on the right dates, with no intro,
outro or chat-header lines turned into events.

## Decisions

| Topic | Decision |
|---|---|
| Parsing | Rule-based first; LLM (Claude) only when the rules find zero events |
| Flow | Read-only preview, then download |
| ICS generation | Backend, from the event list the frontend previewed |
| Event time | All-day. Times in prose (`om 21:30`) stay in the title |
| Year | Not in the message; inferred (see below) |
| Persistence, auth | None |

## Solution layout

```
TheMUC.slnx
muc-apphost/            Aspire AppHost: runs backend + Vite frontend
muc-servicedefaults/    Standard Aspire telemetry + health checks
muc-backend/            ASP.NET Core API (.NET 10)
muc-backend.tests/      xUnit
muc-frontend/           React 19 + Vite + TypeScript
```

The AppHost starts both apps and hands the backend URL to Vite, which proxies
`/api` to it. The Anthropic API key is an Aspire secret parameter backed by
user secrets; it is never committed.

The WeatherForecast template code is removed.

## Backend

### API

`POST /api/events/parse`

```json
// request
{ "text": "Maandag 28 september\n..." }
// response
{
  "events": [{ "date": "2026-09-28", "title": "AURAfarm Schachtenfakbar - Dionysia", "url": "https://www.facebook.com/events/1660066248783093" }],
  "skippedLines": ["Het is weer tijd voor een week overzicht!"],
  "source": "rules"
}
```

`url` is null when the event has no link. `source` is `rules` or `llm`.
Empty or whitespace-only `text` returns 400. Text over 20 000 characters
returns 400 (this is a pasted chat message, and the text may be sent to a paid
API).

`POST /api/events/ics`

Request `{ "events": [...] }` in the same event shape. Response is
`text/calendar` with `Content-Disposition: attachment; filename="mercurius.ics"`.
An empty list, an invalid date, or an empty title returns 400.

### Rule parser

Processes the text line by line, trimming each line.

1. **Date line** — a line that consists *entirely* of a date opens a day.
   Recognised, case-insensitive:
   - optional weekday, Dutch or English, full or abbreviated
     (`maandag`, `ma`, `ma.`, `monday`, `mon`), optionally followed by a comma
   - day number, optionally with `e`/`ste`/`de`/`st`/`nd`/`rd`/`th`
   - month as a Dutch or English name, full or abbreviated
     (`september`, `sept`, `sep`, `sep.`), with an optional year
   - or numeric `d/m`, `d-m`, `d.m`, each with an optional `/yyyy` or `/yy`.
     Numeric dates are day-first.
   - month-first English order (`September 28`) is also accepted for named months.
2. **Inside a day**
   - a line that is a URL (`http://` or `https://`) attaches to the event above
     it; if that event already has a URL, or there is no event yet, the line is
     skipped
   - any other non-blank line starts a new event with that line as its title
   - a blank line closes the day
3. **Outside a day** — every non-blank line is reported in `skippedLines`.

Because a date line must be nothing but a date, the chat header
`Matteo — 04/10/2026, 21:11` is not a date line, and because a blank line
closes the day, trailing chatter such as `Hexion gaat sowieso aanwezig zijn…`
is skipped rather than turned into an event.

### Year inference

When the date line has no year:

- with a weekday: the year closest to today in which that day and month fall on
  that weekday, searching today's year ± 3; if none match, fall through
- without a weekday: whichever of last year, this year or next year puts the
  date closest to today

"Today" comes from an injected `TimeProvider` so tests are deterministic.

### LLM fallback

Runs only when the rule parser returns zero events and an API key is
configured. Sends the message to Claude with today's date and a structured
output schema matching the event shape; the response is validated the same way
as an `/ics` request. Model name is configuration.

No key configured, or the call fails: the endpoint returns the rule parser's
empty result with `source: "rules"`, and a failure is logged. The user sees
"no events found" rather than an error.

### ICS writer

Hand-written; all-day events need no time zone data.

- `VCALENDAR` with `VERSION:2.0`, `PRODID`, `CALSCALE:GREGORIAN`
- one `VEVENT` per event: `UID`, `DTSTAMP` (UTC), `DTSTART;VALUE=DATE`,
  `DTEND;VALUE=DATE` (the following day), `SUMMARY`, and when a URL is present
  `URL` and `DESCRIPTION`
- `UID` is a stable hash of date + title, so re-importing the same message
  updates events instead of duplicating them
- text values escape `\`, `;`, `,` and newlines; lines fold at 75 octets
  without splitting a UTF-8 character; line endings are CRLF

## Frontend

Follows reacthandbook.dev project standards: feature folder, function
declarations, `@/` import alias, local state first, a data-fetching library
for server state, ESLint + Prettier.

```
src/
  features/calendar-import/
    api/          parseMessage, downloadIcs (TanStack Query mutations)
    components/   MessageForm, EventPreview
    types/
    index.ts
  App.tsx
  main.tsx        QueryClientProvider
```

One screen:

1. Textarea with a visible label, and a Parse button (disabled while empty or
   pending).
2. After parsing: events grouped by day, each showing title and link; a count;
   a collapsed "skipped lines" list; a note when the result came from the LLM.
3. Download .ics button, which posts the previewed events and saves the
   response as a file.

States: empty, loading, error (message plus retry), and "no events found".
Corrections are made by editing the pasted text and parsing again.

The Vite template's demo content and unused assets are removed.

## Testing

Backend, xUnit:

- parser against the three sample messages (expected event lists asserted in full)
- date-line variants: abbreviations, English names, numeric forms, ordinals,
  explicit year
- lines that must not be date lines (chat header, prose containing a date)
- year inference around a year boundary, with and without weekday
- ICS writer: escaping, folding of long and non-ASCII titles, CRLF, stable UID
- endpoint validation (400 cases) and the fallback being skipped without a key

Frontend: `eslint` and `tsc -b && vite build` must pass. No component tests —
the frontend holds no parsing or calendar logic.

## Out of scope

Editing events in the preview, timed events, recurring events, storing
history, accounts, deployment.
