# Mercurius Unified Calendar — design

Date: 2026-10-06

## Amendment — 2026-10-09: shared calendar feed

This replaces the download flow. Where the sections below still say
"download", this amendment wins.

- **One feed instead of files.** `GET /calendar.ics` on the backend returns
  every event of every saved import, in date order, as an inline
  `text/calendar` response named "Mercurius Unified Calendar". An event saved
  more than once (same date and title) appears once. People subscribe to it
  once; nothing is downloaded from the app.
- **Removed:** `GET /api/imports/{id}/ics`, the "Save & download .ics" button
  and the Download action in the history table. The button is now **Add to
  calendar**; after saving, the page confirms how many events were added.
- **History is the calendar's contents.** Deleting an import removes its events
  from the feed.
- **The link is at the top of the page**, with Copy and a `webcal://` Subscribe
  link. Its address is a build-time setting (`CALENDAR_URL`); in development it
  is the page's own origin.
- **Deployment.** Two compose services on one network: `backend` (not
  published) and `frontend`. The Cloudflare Tunnels run on another machine in
  the local network, so the frontend's two ports are published on the network:
  - 8642: the app. The tunnel pointing here has a Cloudflare Access policy.
  - 8643: hostname `mucalendar.michaelbielen.be`, no Access. This nginx port
    serves only `/calendar.ics` (also at `/`) and answers 404 to everything
    else, so the app and API are not reachable through the public tunnel.
  - Accepted consequence: anyone on the local network can use the app on 8642
    without Access. `MUC_BIND=127.0.0.1` takes the ports off the network.
- **Deploy tool.** The AppHost has a `deploy` resource that is started by hand
  from the Aspire dashboard. It runs `deploy.cmd`, which sends the last commit
  to `/home/rbgh_admin/docker/themuc` on `rbgh_admin@192.168.0.156` over SSH
  and runs `docker compose up --detach --build` there. An optional `.env` in
  that folder stays on the server and is never overwritten.
- **Considered and dropped:** app-level accounts with a GitHub Pages frontend.

## Goal

Paste a weekly-overview chat message listing student events, review and correct
the events that were recognised, and download an `.ics` file that imports into
any calendar. Earlier imports stay available in a history table.

Success: the three sample messages in `muc-backend.tests/Samples/` each produce
exactly the events a human reads in them, on the right dates, with no intro,
outro or chat-header lines turned into events.

## Decisions

| Topic | Decision |
|---|---|
| Parsing | Rule-based first; LLM (Claude) only when the rules find zero events |
| Flow | Editable preview, then download |
| ICS generation | Backend, from the saved (edited) event list |
| Event time | All-day. Times in prose (`om 21:30`) stay in the title |
| Year | Not in the message; inferred (see below) |
| History | One SQLite table via EF Core |
| Accounts | None. History is shared by everyone who can reach the app |
| Development | Aspire AppHost |
| Deployment | Hand-written `compose.yaml`, reached through a Cloudflare Tunnel with Cloudflare Access |

## Solution layout

```
TheMUC.slnx
compose.yaml
.env.example
muc-apphost/            Aspire AppHost: runs backend + Vite frontend
muc-servicedefaults/    Standard Aspire telemetry + health checks
muc-backend/            ASP.NET Core API (.NET 10), EF Core + SQLite
muc-backend.tests/      xUnit
muc-frontend/           React 19 + Vite + TypeScript
```

In development the AppHost starts both apps and hands the backend URL to Vite,
which proxies `/api` to it. The Anthropic API key is optional, so it is not an
Aspire parameter (those block startup until filled in): in development it is a
user secret of the backend project, in deployment an environment variable. It
is never committed.

The WeatherForecast template code is removed.

## Backend

### Event shape

```json
{ "date": "2026-09-28", "title": "AURAfarm Schachtenfakbar - Dionysia", "url": "https://www.facebook.com/events/1660066248783093" }
```

`url` is null when the event has no link. An event is valid when `date` is a
real ISO date, `title` is non-blank and at most 500 characters, and `url` is
null or an absolute `http`/`https` URL.

### API

| Method and path | Purpose |
|---|---|
| `POST /api/events/parse` | Parse text into events. Stores nothing |
| `POST /api/imports` | Save an import (the edited events) to history |
| `GET /api/imports` | History list, newest first, at most 100 |
| `GET /api/imports/{id}` | One import, including source text and events |
| `GET /api/imports/{id}/ics` | The import as an `.ics` download |
| `DELETE /api/imports/{id}` | Remove an import from history |

`POST /api/events/parse` — request `{ "text": "..." }`, response:

```json
{
  "events": [ /* event shape */ ],
  "skippedLines": ["Het is weer tijd voor een week overzicht!"],
  "source": "rules"
}
```

`source` is `rules` or `llm`. Blank `text`, or text over 20 000 characters,
returns 400. The endpoint is rate limited with one global fixed window
(30 requests a minute by default, configurable),
because it can trigger a paid API call. The limit is global rather than per
client IP: behind the tunnel every request arrives from the same address.

`POST /api/imports` — request `{ "sourceText": "...", "source": "rules", "events": [...] }`.
Returns 201 with `{ "id", "createdAt", "eventCount" }`. An empty event list, any
invalid event, or more than 200 events returns 400.

`GET /api/imports` — rows of `{ "id", "createdAt", "eventCount", "firstDate", "lastDate", "source" }`.

`GET /api/imports/{id}/ics` — `text/calendar` with
`Content-Disposition: attachment; filename="mercurius-<firstDate>.ics"`.
Unknown ids return 404 on all `{id}` routes.

### History storage

One table, `Imports`:

| Column | Type | Notes |
|---|---|---|
| `Id` | integer, key | |
| `CreatedAt` | UTC timestamp | |
| `SourceText` | text | the pasted message |
| `Source` | text | `rules` or `llm` |
| `EventsJson` | text | the saved events, as JSON |
| `EventCount`, `FirstDate`, `LastDate` | integer, date, date | copied from the events so the history list need not load them |

Events are stored as JSON in the row rather than in a second table: they are
only ever read and written together with their import. EF Core migrations are
applied at startup. The database file path comes from configuration.

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
   - ISO `yyyy-mm-dd`

   Chat decoration around the date (`**bold**`, `# heading`, a trailing `:`) is
   ignored.
2. **Inside a day**
   - a line that is a URL (`http://` or `https://`) attaches to the event above
     it; if that event already has a URL, or there is no event yet, the line is
     skipped
   - any other non-blank line starts a new event with that line as its title
   - a blank line closes the day
3. **Outside a day** — every non-blank line is reported in `skippedLines`.

An event repeated with the same date and title — two overlapping messages
pasted together — is returned once.

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
output schema matching the event shape; returned events are validated against
the event rules and invalid ones dropped. The model is configuration and
defaults to `claude-opus-5-5` at low effort.

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
  updates events instead of duplicating them. Editing an event's date or title
  therefore makes it a new calendar entry
- text values escape `\`, `;`, `,` and newlines; lines fold at 75 octets
  without splitting a UTF-8 character; line endings are CRLF

## Frontend

Follows reacthandbook.dev project standards: feature folders, function
declarations, `@/` import alias, local state first, a data-fetching library
for server state, ESLint + Prettier.

```
src/
  features/
    calendar-import/
      api/          parseMessage, saveImport
      components/   MessageForm, EventEditor, EventRow
      types/
      index.ts
    import-history/
      api/          useImports, getImport, deleteImport
      components/   HistoryTable
      index.ts
  lib/              api client (fetch wrapper), query client
  App.tsx
  main.tsx          QueryClientProvider
```

One screen, three parts:

1. **Message** — a labelled textarea and a Parse button (disabled while empty
   or pending).
2. **Events** — after parsing, an editable list sorted by date. Each row has a
   date input, a title input, a link input and a Remove button. Invalid rows
   are marked and block saving. Also shown: the event count, a collapsed
   "skipped lines" list, and a note when the result came from the LLM.
   A **Save & download .ics** button saves the import, then downloads the file
   from `/api/imports/{id}/ics`.
3. **History** — a table of earlier imports: saved at, date range, event
   count, and per row Download, Open and Delete. Open loads that import's text
   and events back into parts 1 and 2; saving again creates a new history row.
   Delete asks for confirmation.

Edited events live in local component state (`useReducer`); the history list
and the mutations go through TanStack Query, which refreshes the table after a
save or delete. Adding an event that the parser missed is done by fixing the
text and parsing again — there is no "add row".

States covered: empty, loading, error (message plus retry), "no events found",
and empty history.

The Vite template's demo content and unused assets are removed.

## Deployment

`compose.yaml` at the repository root, three services:

- **backend** — built from `muc-backend/Dockerfile` with the repository root as
  build context (it references `muc-servicedefaults`). A named volume holds the
  SQLite file. Reads `ANTHROPIC_API_KEY` from `.env`; leaving it empty disables
  the LLM fallback. Not published to the host.
- **frontend** — multi-stage image: Node builds the static site, nginx serves
  it and proxies `/api` to the backend. Published on `127.0.0.1` only, for
  checking the deployment from the host itself.
- **cloudflared** — the Cloudflare Tunnel connector, pointing the tunnel's
  public hostname at the frontend service. Reads `TUNNEL_TOKEN` from `.env`.

`.env.example` documents the variables; `.env` is git-ignored. Everything
inside the compose network is plain HTTP; Cloudflare terminates TLS.

There are no accounts in the app. Access control is Cloudflare Access on the
tunnel's hostname, configured in the Cloudflare dashboard — that policy is not
part of this repository. Because no port is published beyond loopback, the
tunnel is the only way in. The rate limit on `/parse` only caps API spend; it
is not access control.

Aspire is the development orchestrator only. The compose file is written by
hand rather than generated by Aspire, so it stays readable and does not depend
on Aspire's publisher.

## Testing

Backend, xUnit:

- parser against the three sample messages (expected event lists asserted in full)
- date-line variants: abbreviations, English names, numeric forms, ordinals,
  explicit year
- lines that must not be date lines (chat header, prose containing a date)
- year inference around a year boundary, with and without weekday
- ICS writer: escaping, folding of long and non-ASCII titles, CRLF, stable UID
- event validation, and the 400 cases of `parse` and `imports`
- imports round trip against a temporary SQLite file per test: save, list, get,
  ics, delete, 404
- the fallback being skipped without a key

Frontend: `eslint` and `tsc -b && vite build` must pass. No component tests.

Deployment: `docker compose up --build` serves the app on loopback, and an
import survives `docker compose down` followed by `up`. The tunnel itself is
verified by hand with a real token.

## Out of scope

Accounts and access control, timed events, recurring events, adding events by
hand, editing a saved import in place.
