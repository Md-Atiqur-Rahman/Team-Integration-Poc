# Demo script — Org-shared Teams connection

## Prerequisites (start these first, in order)

```
docker start redis                                        # or: docker run -d --name redis -p 6379:6379 redis
cd TeamsIntegration.POC/src/TeamsIntegration.Api && dotnet run --launch-profile https
cd TeamsIntegration.POC/frontend/teams-integration-ui && npx ng serve
```

Redis → Backend → Frontend, in that order (backend needs Redis up to acquire tokens; frontend needs the backend up for API calls). Backend listens on `https://localhost:7059`, frontend on `https://localhost:4200`.

## Demo user credentials

All use password `Demo@123`.

| Email | Display Name | Organization | Project | Application |
|---|---|---|---|---|
| `himel@demo.autom` | Himel | IGB2B | Ecohub | EcohubApp |
| `ratan@demo.autom` | Ratan | IGB2B | Ecohub | EcohubApp |
| `sabbir@demo.autom` | Sabbir | IGB2B | Ecap | EcapApp |
| `anik@demo.autom` | Anik | IGB2B | Ecap | EcapApp |
| `jim@demo.autom` | Jim | SwissLife | SwissLifeProject | SwissLifeApp |

---

## Part 1 — Connect Teams as Himel (first-time connection)

1. Open `https://localhost:4200`
2. Sign in: `himel@demo.autom` / `Demo@123`
3. Choose organization **IGB2B** → choose project **Ecohub**
4. On the dashboard, click **Connect Microsoft Teams** → sign in with the real Microsoft account (**Atiqur.Himel@selisegroup.com**)
5. If prompted to configure a channel, select Team **"Autom Product Team"**, Channel **"Incoming Webhook"**, and save
6. Point out: the card now shows **"Connected as Atiqur.Himel@selisegroup.com"**
7. Type a message and click **Send Message** — show the "Message sent. View in Teams" confirmation

## Part 2 — Prove the connection is org-shared, not personal (the key demo moment)

1. Open a **new browser window/incognito profile** (to make clear no cookies are shared)
2. Go to `https://localhost:4200`
3. Sign in: `ratan@demo.autom` / `Demo@123`
4. Choose organization **IGB2B** → choose project **Ecohub** (same org/project as Himel)
5. **Point out:** the dashboard *immediately* shows **"Connected as Atiqur.Himel@selisegroup.com"** — no Connect button, no Microsoft sign-in prompt for Ratan at all, and the same "Autom Product Team / Incoming Webhook" configuration is already there
6. Type a message and send it as Ratan — show it succeeds too

## Optional — Show project isolation

1. Log out / switch user to `sabbir@demo.autom` (IGB2B / **Ecap** — different project)
2. Point out this project has no saved channel configuration yet, since configuration is per `{org, project, application}` — only the org-level Teams connection is shared, not the channel setup

## Troubleshooting

If a "needs reconnecting" banner appears during the demo, click **Reconnect** as Himel and sign in again — that refreshes the org-shared connection for everyone.
