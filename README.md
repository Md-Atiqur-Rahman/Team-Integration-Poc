# Teams Integration POC

Small ASP.NET Core Web API that proves an organizational Microsoft Entra ID user can obtain a server-side delegated Microsoft Graph token, list their joined Teams and accessible channels, and send a channel message.

## Scope

The POC contains no database, frontend, bot, webhook, durable token cache, host-application context, or production integration architecture. Its token cache is in memory, so restarting the API requires the user to connect again.

## Entra app registration

1. Create an App Registration with **Accounts in any organizational directory**.
2. Add a **Web** platform redirect URI: `https://localhost:7059/api/auth/callback`.
3. Create a development client secret. Do not put it in source control.
4. Add Microsoft Graph **delegated** permissions only: `Team.ReadBasic.All`, `Channel.ReadBasic.All`, and `ChannelMessage.Send`. `User.Read` is optional and is not requested by this implementation.
5. Obtain the target tenant administrator's consent when required by its policy.

The authority is `https://login.microsoftonline.com/organizations/v2.0`; no customer tenant ID is configured. Microsoft Identity Web performs authorization-code flow with state, nonce, PKCE, and OIDC scopes (`openid`, `profile`, `offline_access`).

## Local configuration

Set the client ID and secret outside source control:

```powershell
dotnet user-secrets --project .\src\TeamsIntegration.Api set "AzureAd:ClientId" "<application-client-id>"
dotnet user-secrets --project .\src\TeamsIntegration.Api set "AzureAd:ClientSecret" "<client-secret>"
```

Trust the development certificate if necessary:

```powershell
dotnet dev-certs https --trust
```

Run the API:

```powershell
dotnet run --project .\src\TeamsIntegration.Api --launch-profile https
```

## Manual Swagger test

1. Open `https://localhost:7059/swagger`.
2. Open `https://localhost:7059/api/auth/connect` in a browser tab. Complete organizational Microsoft login and consent. The callback returns to Swagger.
3. In Swagger, call `GET /api/auth/session`; it returns the authenticated session status. Swagger automatically sends the same-origin HttpOnly session cookie.
4. Call `GET /api/teams` and copy a Team ID.
5. Call `GET /api/teams/{teamId}/channels` using that ID and copy a Channel ID.
6. Call `POST /api/teams/messages` with:

   ```json
   {
     "teamId": "<team-id>",
     "channelId": "<channel-id>",
     "content": "Teams Graph POC message"
   }
   ```

7. Confirm the API returns `201 Created`, then verify the message appears in that Teams channel.

The access token, client secret, authorization code, and session cookie are never returned by the API or logged by application code.
