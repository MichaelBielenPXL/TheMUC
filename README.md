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

`compose.yaml` runs four containers on one Docker network:

| Service | What it is | Reached from outside through |
|---|---|---|
| `backend` | the API and the SQLite database (volume `muc-data`) | nothing directly |
| `frontend` | nginx: the app on port 80, the calendar feed alone on port 8081 | the two tunnels |
| `tunnel-app` | Cloudflare Tunnel for the app | your app hostname |
| `tunnel-calendar` | Cloudflare Tunnel for the feed | `mucalendar.michaelbielen.be` |

The stack lives on the server in `/home/rbgh_admin/docker/themuc`.

### One-time setup

1. In the Cloudflare Zero Trust dashboard create two tunnels and copy their tokens.
   - App tunnel: public hostname → `http://frontend:80`. Put a Cloudflare
     Access policy on this hostname; the app has no accounts of its own.
   - Calendar tunnel: `mucalendar.michaelbielen.be` → `http://frontend:8081`.
     No Access policy: calendar apps cannot log in. This port serves only the
     feed, so the app and its API stay unreachable through it.
2. SSH key login from this machine to the server, so no password is asked.
   The key the deploy tool uses is set in `muc-apphost/appsettings.json` under
   `Deploy:IdentityFile` (`~/.ssh/id_ed25519_pxlgaming`); leave it empty to use
   your default key.

3. On the server, create the deploy folder and its `.env`: copy `.env.example`
   from this repository and fill in the two tokens (and the API key, if wanted).

   ```bash
   mkdir -p /home/rbgh_admin/docker/themuc
   nano /home/rbgh_admin/docker/themuc/.env
   ```

   `rbgh_admin` must be allowed to run Docker
   (`sudo usermod -aG docker rbgh_admin`, then log in again).

### Deploying from the Aspire dashboard

Commit what you want to deploy, start the AppHost, find the `deploy` resource
in the dashboard and press Start. It runs `deploy.cmd`, which:

1. sends the files of the last commit to `/home/rbgh_admin/docker/themuc` on
   the server — compose file, Dockerfiles and source, never `.env`;
2. runs `docker compose --profile tunnel up --detach --build --remove-orphans`
   in that folder.

The output appears in the resource's console log, and the resource finishes
when the stack is up. Uncommitted changes are not deployed. The server address
and folder are set in `muc-apphost/appsettings.json` under `Deploy`.

The same thing by hand, from the repository root:

```powershell
.\deploy.cmd rbgh_admin@192.168.0.156 /home/rbgh_admin/docker/themuc ~/.ssh/id_ed25519_pxlgaming
```

### Running it on the server itself

After a first deploy the folder holds everything compose needs:

```bash
cd /home/rbgh_admin/docker/themuc
docker compose --profile tunnel up -d --build
```

Without `--profile tunnel` nothing is exposed; the app is on
`http://127.0.0.1:8642` and the feed on `http://127.0.0.1:8643/calendar.ics`,
which is useful for checking a build.

The calendar is stored in the `muc-data` volume. `docker compose down` keeps
it; `docker compose down -v` deletes it.
