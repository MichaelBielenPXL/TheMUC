# Mercurius Unified Calendar

Paste a weekly-overview message, check the events, download an `.ics` file.

## Development

Requires the .NET 10 SDK and Node 22.

```bash
cd muc-frontend && npm install && cd ..
dotnet run --project muc-apphost
```

Open the `frontend` URL from the Aspire dashboard.

The AI fallback, used only when the normal parser finds no events, needs an
Anthropic API key:

```bash
dotnet user-secrets set "Anthropic:ApiKey" "<key>" --project muc-backend
```

Tests: `dotnet test TheMUC.slnx`. Frontend checks: `npm run lint && npm run build` in `muc-frontend`.

## Deployment

```bash
cp .env.example .env    # fill in ANTHROPIC_API_KEY and TUNNEL_TOKEN
docker compose --profile tunnel up -d --build
```

In the Cloudflare Zero Trust dashboard, point the tunnel's public hostname at
`http://frontend:80` and put a Cloudflare Access policy on that hostname. The
app has no accounts of its own: Access is the only access control.

Without `--profile tunnel` the app is reachable only on
`http://127.0.0.1:8080` on the host, which is useful for checking a build.

History is stored in the `muc-data` volume. `docker compose down` keeps it;
`docker compose down -v` deletes it.
