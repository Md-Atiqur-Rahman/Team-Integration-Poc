# Microsoft Teams + Graph Backend POC Implementation Plan

## Purpose

This document preserves the implementation context for the isolated
`TeamsIntegration.POC` backend. It proves Microsoft Entra ID organizational
sign-in and delegated Microsoft Graph access only. It is not the production
Teams integration architecture.

The validated flow is:

```text
Swagger
  -> Connect Microsoft
  -> Microsoft organizational login and consent
  -> Secure application session
  -> Get joined Teams
  -> Get channels for a Team
  -> Send message to a selected channel
```

Success means that Microsoft Graph returns `201 Created` and the message
appears in the selected Microsoft Teams channel.

## Scope

### In scope

- ASP.NET Core Web API on .NET 10.
- Swagger UI hosted by the API.
- Microsoft Entra ID organizational multi-tenant authentication.
- OAuth 2.0/OpenID Connect authorization code flow, with PKCE provided by
  Microsoft Identity Web.
- Server-side delegated access-token acquisition.
- Delegated Graph operations:
  - `GET /me/joinedTeams`
  - `GET /teams/{team-id}/channels`
  - `POST /teams/{team-id}/channels/{channel-id}/messages`
- In-memory token cache for local POC use.
- Minimal validation, safe API responses, and targeted automated tests.

### Explicitly out of scope

- MongoDB, Redis, durable/distributed token cache, and configuration
  persistence.
- Host user, organization, project, and application context.
- Repository pattern, CQRS, MediatR, background jobs, notifications, bots,
  Bot Framework, webhooks, Angular, test-run integration, and business
  isolation.
- Application permissions, client-credentials flow, and message migration.
- Personal Microsoft accounts.

## Current implementation

The POC is located at:

```text
TeamsIntegration.POC/
├── src/TeamsIntegration.Api/
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   └── TeamsController.cs
│   ├── Models/GraphModels.cs
│   ├── Services/
│   │   ├── TeamsGraphExceptionHandler.cs
│   │   └── TeamsGraphService.cs
│   ├── Program.cs
│   └── appsettings.json
├── tests/TeamsIntegration.Api.Tests/
├── README.md
└── TeamsIntegration.POC.slnx
```

The API uses `Microsoft.Identity.Web` to handle sign-in and acquire a
delegated token for Graph. The browser receives only a secure, HttpOnly
application-session cookie; Graph access tokens, refresh-token material,
authorization codes, and client secrets never appear in Swagger responses or
application code logs.

The in-memory token cache intentionally means that an API restart requires the
user to connect again.

## API contract

| Endpoint | Purpose |
|---|---|
| `GET /api/auth/connect` | Starts an Entra sign-in challenge. Open directly in a browser tab; it redirects to Swagger after successful authentication. |
| `GET /api/auth/session` | Returns authenticated-session status and optional display name, without token data. |
| `POST /api/auth/logout` | Removes only the local application cookie session. |
| `GET /api/teams` | Returns Teams joined by the authenticated user using `/me/joinedTeams`. |
| `GET /api/teams/{teamId}/channels` | Returns accessible channels for the selected Team and follows Graph paging links. |
| `POST /api/teams/messages` | Sends a plain-text message to IDs supplied in the request body. |

Message request body:

```json
{
  "teamId": "<team-id>",
  "channelId": "<channel-id>",
  "content": "Teams Graph POC test message"
}
```

The endpoint validates non-empty content, limits it to 4,000 characters, and
does not report success unless Graph returns `201 Created`.

## Authentication and security decisions

| Area | Decision |
|---|---|
| Authority | `https://login.microsoftonline.com/organizations/v2.0` through `TenantId: organizations`; no customer tenant is hard-coded. |
| Supported users | Accounts in any Entra organizational directory; personal accounts excluded. |
| App platform | **Web** confidential client; Swagger is not an OAuth token client. |
| Callback | `https://localhost:7059/api/auth/callback` for local development, registered exactly in Entra. |
| Session | Secure, HttpOnly, SameSite=Lax authentication cookie. |
| Token cache | Server-side, in-memory Microsoft Identity Web/MSAL cache. |
| Graph scopes | Delegated `Team.ReadBasic.All`, `Channel.ReadBasic.All`, and `ChannelMessage.Send`. OIDC scopes are `openid`, `profile`, and `offline_access`. `User.Read` is not requested. |
| Logging | Do not log Authorization headers, session cookies, client secrets, authorization codes, access tokens, refresh tokens, or message contents. |

No application permission is valid for normal live channel posting in this POC.
`Teamwork.Migrate.All` must not be added because it is for migration/import
scenarios, not normal messages.

## Entra App Registration configuration

Create a separate registration with:

1. **Supported account types:** **Accounts in any organizational directory
   (Any Entra ID tenant - Multitenant)**.
2. **Platform:** **Web**.
3. **Redirect URI:** exactly
   `https://localhost:7059/api/auth/callback`—without a trailing period or
   slash variation.
4. **Client secret:** local-development secret stored only in .NET User
   Secrets. Do not put the secret value in source control.
5. **Delegated Microsoft Graph permissions:** `Team.ReadBasic.All`,
   `Channel.ReadBasic.All`, and `ChannelMessage.Send`.
6. **Consent:** the target tenant administrator must approve the requested
   permissions when the tenant policy requires it.

Set local secrets:

```powershell
dotnet user-secrets --project .\src\TeamsIntegration.Api set "AzureAd:ClientId" "<application-client-id>"
dotnet user-secrets --project .\src\TeamsIntegration.Api set "AzureAd:ClientSecret" "<client-secret-value>"
```

The Entra client ID is non-secret but is intentionally kept out of the
committed POC configuration; `appsettings.json` contains a placeholder.

## Manual Swagger verification

1. Run the API:

   ```powershell
   dotnet run --project .\src\TeamsIntegration.Api --launch-profile https
   ```

2. Open `https://localhost:7059/swagger`.
3. Open `https://localhost:7059/api/auth/connect` in the same browser profile.
   Do not invoke the connect endpoint as a Swagger AJAX request.
4. Complete organizational Microsoft login and consent. The browser returns to
   Swagger with the application session cookie.
5. Call `GET /api/auth/session`.
6. Call `GET /api/teams`, then copy a returned Team ID.
7. Call `GET /api/teams/{teamId}/channels`, then copy a returned Channel ID.
8. Call `POST /api/teams/messages` with the Team ID, Channel ID, and a clearly
   identifiable test message.
9. Confirm a `201 Created` response and check that the message exists in the
   selected Teams channel.

## Validation completed

The solution was built and tested with:

```powershell
dotnet build
dotnet test
```

The focused test suite verifies request validation and expected anonymous
session challenge behavior. Live Graph verification was completed manually
after the Entra administrator approved the delegated-permission request.

## Future implementation guidance

If this POC becomes a production feature, design those requirements separately
instead of expanding this project without a new boundary. Future work may need:

- a protected distributed token cache and key management;
- tenant-aware issuer and identity validation;
- trusted host-application authorization and organization/project/application
  context;
- persisted Team/channel configuration and reconnect state;
- anti-forgery/CORS design if a separately hosted frontend is introduced;
- Graph error mapping, throttling behavior, observability redaction, and
  durable delivery semantics;
- a deliberate decision about delegated-user versus future bot/application
  identity requirements.

Do not assume the POC's in-memory session/token design, direct
Team/channel-in-request model, or manual Swagger flow is sufficient for that
future architecture.
