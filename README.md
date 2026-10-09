# Mercurius Unified Calendar

Paste a weekly-overview message, check the events, add them to a shared
calendar that anyone can subscribe to with one link.

## Development

Requires the .NET 10 SDK and Node 22. Open `TheMUC.slnx` (not a project folder)
and start `muc-apphost`, or from the repository root:

```bash
cd muc-frontend && npm install && cd ..
dotnet run --project muc-apphost
```

Open the `frontend` URL from the Aspire dashboard. Starting only the frontend
(`npm run dev`) does not start the backend.

The AI fallback, used only when the normal parser finds no events, needs an
Anthropic API key:

```bash
dotnet user-secrets set "Anthropic:ApiKey" "<key>" --project muc-backend
```

Tests: `dotnet test TheMUC.slnx`. Frontend checks: `npm run lint && npm run build` in `muc-frontend`.

## Deployment

`compose.yaml` runs two containers on one Docker network, on the server
`192.168.0.156` in `/home/rbgh_admin/docker/themuc`:

| Service | What it is | Published on the server |
|---|---|---|
| `backend` | the API and the SQLite database (volume `muc-data`) | nothing |
| `frontend` | nginx | port 8642: the app · port 8643: the calendar feed and nothing else |

The Cloudflare Tunnels run on another machine in the network and point at
those two ports:

| Tunnel hostname | Points to | Cloudflare Access |
|---|---|---|
| the app's hostname | `http://192.168.0.156:8642` | **yes** — the app has no accounts of its own |
| `mucalendar.michaelbielen.be` | `http://192.168.0.156:8643` | no — calendar apps cannot log in |

Port 8643 answers only `/calendar.ics` (also at `/`), so the app and its API
are not reachable through the public calendar hostname.

Both ports are open to the local network: anyone on it can use the app
directly on port 8642, without Access.

### Deploying from the Aspire dashboard

Commit what you want to deploy, start the AppHost, find the `deploy` resource
in the dashboard and press Start. It runs `deploy.cmd`, which:

1. sends the files of the last commit to `/home/rbgh_admin/docker/themuc` on
   the server — compose file, Dockerfiles and source, never `.env`;
2. runs `docker compose up --detach --build --remove-orphans` in that folder.

The output appears in the resource's console log, and the resource finishes
when the stack is up. Uncommitted changes are not deployed.

The server address, folder and SSH key are set in
`muc-apphost/appsettings.json` under `Deploy`. The key must log in without a
password prompt, and `rbgh_admin` must be allowed to run Docker.

The same thing by hand, from the repository root:

```powershell
.\deploy.cmd rbgh_admin@192.168.0.156 /home/rbgh_admin/docker/themuc ~/.ssh/id_ed25519_pxlgaming
```

### Settings

None are required. To change one, create `.env` in the deploy folder on the
server (see `.env.example`): the Anthropic API key for the AI fallback, the
calendar address shown in the app, the ports, or `MUC_BIND=127.0.0.1` to take
the ports off the network.

### On the server itself

```bash
cd /home/rbgh_admin/docker/themuc
docker compose up -d --build
```

The calendar is stored in the `muc-data` volume. `docker compose down` keeps
it; `docker compose down -v` deletes it.
