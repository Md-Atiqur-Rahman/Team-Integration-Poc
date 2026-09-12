### 23.5 Actual-plan implementation order

Complete these as separate, small tasks:

1. **Define the Autom host-context contract.** Specify how the Teams backend
   receives and verifies the host user, organization, project, application,
   and allowed Autom roles. Never accept those values as trusted browser input.
2. **Create the production backend foundation.** Introduce the real project
   structure, typed options, stable Problem Details/error codes, safe
   structured logging, and tests while retaining the POC's validated Graph
   approach.
3. **Enforce host authorization.** Apply the approved Autom contract to every
   Teams endpoint and test cross-organization/project/application isolation.
4. **Persist Team/channel configuration.** Add MongoDB, the unique compound
   index for `{ organizationId, projectId, applicationId }`, and configuration
   read/upsert endpoints that revalidate Graph access before saving.
5. **Implement durable token management.** Replace the POC cache with a
   protected supported distributed cache; define production Data Protection
   keys and client-credential storage.
6. **Add reconnect health.** Detect interaction-required/non-renewable token
   failures, persist `needsReconnect`, prevent delivery, and return a safe
   reconnect response.
7. **Build the Angular dashboard.** Use the BFF cookie session and trusted
   host context; never expose Graph tokens to Angular.
8. **Send through saved configuration.** Accept only message content, resolve
   the saved destination by trusted host context, revalidate access, and post
   through delegated Graph access.
9. **Integrate Autom test-run notifications.** Define a trusted server-side
   caller contract and delivery semantics; do not blindly retry non-idempotent
   message posting.
10. **Harden for release.** Complete anti-forgery/CORS, throttling,
    observability redaction, tenant consent/onboarding, and end-to-end tests
    for reassignment, isolation, reconnect, and live delivery.

> **Sequencing exception (2026-09-12):** For a near-term demo, task 4
> (MongoDB Configuration) is being implemented **before** task 1 (host-context
> contract). This is a deliberate, temporary exception to the order above, not
> a correction to it. `PUT/GET /api/teams/configuration` will, for now,
> **trust** `organizationId`/`projectId`/`applicationId` values supplied
> directly by the caller (Angular/Swagger) without server-side verification
> against Autom's own authorization system. This is acceptable only because
> the demo environment has no untrusted callers. **Task 3 (Enforce host
> authorization) must be completed, and this configuration endpoint locked
> down to only accept host-verified context, before this moves anywhere
> beyond the controlled demo.** Do not treat the absence of Task 1 as
> resolved or optional — it is deferred, not skipped.

The next task is **task 4**, with the sequencing exception above in effect.
Task 1 remains a hard prerequisite before this integration handles real,
untrusted multi-tenant traffic.

### Task 4 completion record (2026-09-12)

Task 4 (MongoDB Configuration + Token Management) has been implemented in
`TeamsIntegration.POC/src/TeamsIntegration.Api/`, building on the existing
validated `TeamsController`/`TeamsGraphService`/`TeamsGraphExceptionHandler`/
`AuthController` rather than replacing them:

- **`TeamsConfigurations` MongoDB collection** (`Models/TeamsConfiguration.cs`),
  matching POC-PLAN.md Section 10.1's schema exactly, with a unique compound
  index on `{ organizationId, projectId, applicationId }`
  (`Repositories/MongoIndexInitializer.cs`, applied lazily and idempotently
  on first write rather than at host startup, so hosts that never touch this
  feature don't require a live Mongo connection).
- **`PUT /api/teams/configuration`** and **`GET /api/teams/configuration`**
  (`Controllers/TeamsConfigurationController.cs`,
  `Services/TeamsConfigurationService.cs`,
  `Repositories/MongoTeamsConfigurationRepository.cs`). Per the sequencing
  exception above, `organizationId`/`projectId`/`applicationId` are **trusted
  as given** — no host-context verification was added. Before every save, the
  selected team/channel is revalidated against Microsoft Graph by reusing the
  existing `ITeamsGraphService.GetTeamsAsync`/`GetChannelsAsync` calls (no new
  Graph endpoints); a failed revalidation returns `404` and writes nothing to
  Mongo. `GET` returns `404 configuration_not_found` when no configuration
  exists for the given key.
- **Token cache unchanged for Task 4** — still the in-memory
  `AddInMemoryTokenCaches()` at the time; no raw access/refresh tokens are
  stored in MongoDB anywhere. (Superseded by Task 5, below — the cache is now
  distributed.)
- **Graph best-practices additions**: `TeamsGraphService` now sends a unique
  GUID per Graph request in `client-request-id` (logged), and
  `TeamsGraphExceptionHandler` forwards `Retry-After` on `429` responses.
- **Tests**: `tests/TeamsIntegration.Api.Tests/` gained repository/service
  tests (Mongo2Go embedded `mongod`, no Docker dependency) covering the
  unique-index rejection, successful upsert + round-trip read, `GET`
  returning `null`/`404` when absent, and revalidation failure blocking the
  save (missing team, missing channel). All 12 tests (6 pre-existing + 6 new)
  pass; `dotnet build` is clean.

**This does not change the sequencing exception or resolve Task 1.** The
configuration endpoints still trust caller-supplied host IDs. Task 1 (host-
context contract) and Task 3 (enforce host authorization) remain required
before this handles real, untrusted multi-tenant traffic — see the exception
note above, which stays in effect unchanged.

### Task 5 completion record (2026-09-12)

Task 5 (durable token management) has been implemented in
`src/TeamsIntegration.Api/Program.cs`, replacing `AddInMemoryTokenCaches()`
with a real distributed cache — no other backend code changed:

- **`AddDistributedTokenCaches()`** (Microsoft.Identity.Web) backed by
  **Redis** via `AddStackExchangeRedisCache`, configured from the new
  `Redis` section in `appsettings.json` (`Configuration`/`InstanceName`,
  bound directly to `RedisCacheOptions` — no custom options class needed).
  Confirmed via research before implementing: both the registered
  `IDistributedCache` and MSAL's distributed token cache connect lazily on
  first actual use, not at host startup, so this does not force a live Redis
  connection for hosts/tests that never acquire a token (the existing
  `AuthenticationEndpointTests` stayed green with no Redis running during
  development).
- **Encryption at rest**: `Configure<MsalDistributedTokenCacheAdapterOptions>(o => o.Encrypt = true)`
  makes MSAL encrypt cached token blobs via ASP.NET Core Data Protection
  before they reach Redis.
- **Data Protection key ring**: `AddDataProtection().SetApplicationName("TeamsIntegration.POC").PersistKeysToFileSystem(...)`
  persists keys explicitly to `src/TeamsIntegration.Api/App_Data/dataprotection-keys/`
  (now gitignored) instead of relying on the OS-default implicit location,
  so the key ring — and therefore the ability to decrypt cached tokens —
  survives process restarts predictably.
- **Client-credential storage**: no code change — `Microsoft.Identity.Web`
  already supports certificate-based credentials via `AzureAd:ClientCertificates`
  configuration alone. Documented as the recommended production mechanism
  instead of `AzureAd:ClientSecret`.
- **Open item, not implemented**: production *external* protection of the
  Data Protection key ring (the framework already warns "No XML encryptor
  configured... may be persisted to storage in unencrypted form" when only
  `PersistKeysToFileSystem` is used). POC-PLAN.md Section 12 suggests Azure
  Key Vault, but CLAUDE.md already notes Autom does not host on Azure — so
  this needs a decision grounded in Autom's actual secret-management
  infrastructure before production, not a default Key Vault implementation.
- **Tests**: `tests/TeamsIntegration.Api.Tests/` gained
  `RedisDistributedCacheTests.cs` (Testcontainers.Redis — a real Redis
  container, round-tripping a cache value through the exact
  `AddStackExchangeRedisCache` registration used in `Program.cs`) and
  `DataProtectionTests.cs` (protect/unprotect round-trip against a temp key
  ring, plus a DI check that `MsalDistributedTokenCacheAdapterOptions.Encrypt`
  is `true`). All 16 tests (12 from Task 4 + 4 new) pass; `dotnet build` is
  clean. Docker Desktop must be running locally for the Redis-backed tests.

This task does not touch host-context authorization, the Task 4 Mongo/Graph
code, or `needsReconnect` detection (Task 6).

### Task 6 completion record (2026-09-12)

Task 6 (reconnect health) has been implemented. Detection already existed as
a side effect of the original POC — `TeamsGraphService.SendAsync` already
caught `MicrosoftIdentityWebChallengeUserException` (MSAL's signal for
revoked consent, invalid grant, disabled account, Conditional Access,
interaction-required) and threw `TeamsGraphException(401, ...)`, and `401`
is used exclusively for this case in the codebase. What this task added is
persisting that into Mongo and giving the API response a stable code:

- **`TeamsGraphException.ReauthenticationRequiredCode`** (`"reauthentication_required"`)
  — single source of truth, added as a `const` on `TeamsGraphException`.
- **`TeamsGraphExceptionHandler.HandleAsync`**: for `401` responses, sets
  `ProblemDetails.Extensions["code"] = ReauthenticationRequiredCode`, giving
  Section 13's "return `401` with `reauthentication_required`" a stable,
  parseable field. Applies to every `401` from `TeamsGraphException`
  (including `TeamsController`'s pre-existing endpoints), not just the new
  configuration path.
- **`ITeamsConfigurationRepository.MarkNeedsReconnectAsync`** /
  `MongoTeamsConfigurationRepository`: an `UpdateOneAsync` against the
  existing 3-key compound filter, setting `ConnectionStatus =
  needsReconnect`, `ConnectionFailureCode`, `ConnectionFailureDetectedAtUtc`,
  `UpdatedAtUtc`. A safe no-op (`MatchedCount == 0`) when no configuration
  exists for that key yet.
- **`TeamsConfigurationService.SaveAsync`**: the two Graph revalidation calls
  are each wrapped with `catch (TeamsGraphException ex) when (ex.StatusCode
  == 401)`, calling `MarkNeedsReconnectAsync` then rethrowing — the caller's
  `401` response is unchanged in shape, just now also persisted and coded.
  `404` (team/channel not found — Task 4's existing revalidation-failure
  path) is untouched.
- **Recovery required no new code**: `SaveAsync`'s existing successful-save
  path already unconditionally sets `ConnectionStatus = Active` and nulls
  the failure fields on every successful save, so a user who reconnects and
  re-saves naturally clears `needsReconnect`.

**Honest scope boundary — read before building on this.** The only place in
the codebase today that both calls Graph with the user's delegated token
*and* knows which `{organizationId, projectId, applicationId}` configuration
it's acting on is `TeamsConfigurationService.SaveAsync` (`PUT
/api/teams/configuration`). `TeamsController`'s `GetTeams`/`GetChannels`/
`SendMessage` have no host context at all — replacing them is Task 8 ("send
through saved configuration"), not built yet — so a Graph failure there
cannot be attributed to a saved configuration and isn't wired into this
mechanism. "Prevent delivery" from the Task 6 description is therefore only
partially realized: this task delivers the detect-and-persist mechanism and
the `connectionStatus` contract, but there is no send-through-saved-config
code path yet for it to actually block. **Task 8 must check
`connectionStatus` before posting once it exists.** `connectionAlertedAtUtc`
is intentionally left `null` — there's no dashboard (Task 7) or Autom-
notification integration (Task 9) yet to alert anyone, so stamping a
timestamp with nothing behind it would be misleading.

- **Tests**: extended `FakeTeamsGraphService` with a settable
  `ExceptionToThrow`; added two `TeamsConfigurationServiceTests` (marks an
  existing configuration `needsReconnect` and rethrows; saves nothing on a
  first-time save that fails the same way), one
  `TeamsConfigurationRepositoryTests` (`MarkNeedsReconnectAsync` no-ops
  safely on a missing key), and a new `TeamsGraphExceptionHandlerTests.cs`
  (constructs a `DefaultHttpContext` directly to verify the `code` extension
  is present for `401` and absent for other statuses). All 21 tests (16 from
  Tasks 4/5 + 5 new) pass; `dotnet build` is clean.

### Task 7 completion record (2026-09-12)

Task 7 (Angular dashboard) has been implemented at
`TeamsIntegration.POC/frontend/teams-integration-ui/` (Angular 22,
standalone components, no Router — a single dashboard view per Section 9 —
no Angular Material/CDK, zoneless by CLI default). Per the two choices made
before implementing: host context (`organizationId`/`projectId`/
`applicationId`) comes from **URL query params with an environment-config
default** (`readHostContextFromUrl()` in `teams-dashboard.store.ts`), and
**Send Message was deferred to Task 8** — not built here, since its real
endpoint (send through the saved configuration) doesn't exist yet.

**Required backend fixes** (small, additive, justified by POC-PLAN.md
Section 8's already-documented target contract — not new scope):

- `AuthController.GetSession` was `[Authorize]`, so an anonymous call
  produced a `302` redirect to the Microsoft login page — unusable for a
  `fetch()`-based SPA. Changed to `[AllowAnonymous]` with a manual
  `User.Identity?.IsAuthenticated` check, always returning `200
  { isAuthenticated, isTeamsConnected, displayName }`. `isTeamsConnected` is
  currently just an alias for `isAuthenticated` (no separate Graph-token
  probe exists to distinguish them — documented simplification).
  **`AuthenticationEndpointTests.Session_redirects_anonymous_users_to_sign_in`
  was rewritten** to `Session_returns_not_authenticated_for_anonymous_users`
  (asserts `200`, not `302`) — the old test validated the POC's
  pre-Section-8-contract behavior, not a decision worth preserving.
- `AuthController.Connect` now accepts an optional `returnUrl` query
  parameter (so the host-context query string survives the OAuth round
  trip) validated by origin against the new `Frontend:BaseUrl` config value
  (`Configuration/FrontendOptions.cs`) before use — no arbitrary
  caller-supplied redirect target is trusted (open-redirect guard).
- **CORS**: `AddCors`/`UseCors` added, restricted to the single configured
  `Frontend:BaseUrl` origin with `AllowCredentials()` (required for the
  session cookie to flow cross-origin in local dev — Angular on
  `localhost:4200`, API on `localhost:7059`). No wildcard origin.
  `appsettings.json` gained `"Frontend": { "BaseUrl": "http://localhost:4200" }`,
  reused for both the CORS origin and the default OAuth return URL.
  **Post-implementation fix (2026-09-13)**: this value is now
  `https://localhost:4200`, and `angular.json`'s `serve` target defaults to
  `"ssl": true`. You found in real testing that a successful sign-in still
  showed "Not Connected" — root cause was that the session cookie is
  `Secure` + `SameSite=Lax`, and browsers classify cookie "site" by scheme
  as well as domain, so `http://localhost:4200` and `https://localhost:7059`
  were cross-site despite both being "localhost": the cookie set correctly
  during the OAuth top-level-navigation callback, but was withheld on
  Angular's subsequent `fetch()` to `/api/auth/session`. Serving Angular
  over HTTPS too (same scheme, same registrable domain — port doesn't
  matter for "site") fixed it without weakening `SameSite` to `None`.

**Frontend structure**: `core/api-client/teams-api.client.ts` (HttpClient
wrapper + `parseApiError`, which normalizes both existing backend error
shapes — `{code, message}` and `ProblemDetails` with `Extensions["code"]` —
since both serialize as flat JSON with `code` at the top level);
`core/interceptors/` (`withCredentialsInterceptor`,
`reauthenticationInterceptor` — the latter injects the store directly and
flips a shared `needsReconnect` signal on any `401
reauthentication_required`, from *any* endpoint, not just the configuration
save); `features/teams-dashboard/teams-dashboard.store.ts` (signal-based
state, injected directly by each feature component rather than
prop-drilled — reasonable for a 3-component single-feature app); `connection-card.ts`
and `channel-configuration.ts` (each inject the store directly, matching the
Section 9 state table minus the Send-Message-only rows).

**Tests**: Angular CLI 22's default scaffold turned out to be
**Vitest**-based (`@angular/build:unit-test`), not Karma — confirmed at
scaffold time, no separate tooling decision needed. 12 tests across 4 files
(`app.spec.ts`, `teams-dashboard.store.spec.ts`,
`channel-configuration.spec.ts`, `connection-card.spec.ts`) — covering
store data flow, the Save-button disable rule, connected/reconnect
rendering, `Connect` triggering `window.location.assign` (not an XHR — the
whole point of the BFF design), a live automated check that a full
connected session load never writes to `localStorage`/`sessionStorage`, and
the `reauthentication_required` interceptor path. Two test-authoring
gotchas resolved during implementation (both fixed, not worked around):
`vi.spyOn(window.location, 'assign')` fails in this jsdom version
("Cannot redefine property") — fixed via `vi.stubGlobal('location', {...})`;
flushing multiple sequential `HttpTestingController` requests inside one
`Promise.all`-driven flow needs a microtask-queue drain
(`await new Promise(resolve => setTimeout(resolve, 0))`) between flushes,
since a promise continuation after an `await` doesn't resume synchronously
with `httpMock.flush()`.

**npm install note**: the initial `ng new` + npm install failed with a
known npm/arborist bug (`Cannot read properties of null (reading
'edgesOut')`) triggered by Vitest 4's optional peer dependencies on this
npm version (10.9.8); worked around with `npm install --legacy-peer-deps`.

All 21 backend tests and 12 frontend tests pass; `dotnet build` and
`ng build` are both clean. Manual interactive Entra sign-in verification
(real browser OAuth flow) was left for you to run, since it isn't
automatable from here.

### Task 8 completion record (2026-09-13)

Task 8 (send through saved configuration) has been implemented. Per your
two choices: host context is passed as **query params** on `POST
/api/teams/messages` (body stays `{content}` only, matching POC-PLAN.md
Section 8's documented shape exactly), and the old raw
`{teamId, channelId, content}` `POST /api/teams/messages` action was
**replaced in place** in `TeamsController.cs` (removed entirely, along with
its now-orphaned `GraphInput.Validate(SendChannelMessageRequest)` — replaced
by a lighter `GraphInput.ValidateMessageContent(string?)`).

- **New `Services/TeamsMessageService.cs`**, mirroring
  `TeamsConfigurationService`'s existing shape: checks
  `configuration.ConnectionStatus == needsReconnect` first (the literal
  instruction Task 6 left behind — no wasted Graph call when already known
  broken), then **always** revalidates the saved team/channel against Graph
  before sending (not only when the cached status looks stale — Section 8:
  "validate/refresh... before posting"), reusing
  `ITeamsGraphService.GetTeamsAsync`/`GetChannelsAsync`/`SendMessageAsync`
  verbatim — no new Graph calls. A revalidation failure (team/channel gone)
  returns `409 stale_configuration` (new `TeamsGraphException.StaleConfigurationCode`,
  same `Extensions["code"]` pattern Task 6 established for `401`) without
  touching Mongo. A `401` from Graph during revalidation or the send itself
  calls `MarkNeedsReconnectAsync` (Task 6's existing repository method,
  reused verbatim) before rethrowing.
- **Known gap, flagged not papered over**: `TeamsConfiguration.ConnectionStatus`
  is still only `active`/`needsReconnect` (Task 4's schema) — there's no
  third enum value for "resource gone but connection otherwise fine," so the
  `409 stale_configuration` case is reported to the caller but not persisted
  to Mongo. Adding a schema value was judged out of this task's scope.
- **New `Controllers/TeamsMessagesController.cs`** (route
  `api/teams/messages`) validates host-context IDs and content, calls the
  **existing** `ITeamsConfigurationService.GetActiveAsync` (Task 4) for the
  `404 configuration_not_found` case — no new lookup logic, full reuse.
- **Frontend**: `send-message.ts`/`.html`/`.css` (same
  inject-the-store-directly pattern as the other dashboard components),
  wired into `teams-dashboard.html` alongside `channel-configuration`. Store
  gained `messageContent`/`sendInProgress`/`lastSentMessage` signals and a
  `sendMessage()` method. A live `401 reauthentication_required` from this
  endpoint is already caught by Task 7's global `reauthenticationInterceptor`
  with zero new wiring — confirms that interceptor being global (not scoped
  to the configuration save flow) was the right call.
- **Tests**: `TeamsMessageServiceTests.cs` (5 tests: success; blocked with
  zero Graph calls when `needsReconnect`; `409` when the team is gone;
  `409` when the channel is gone; `401` + Mongo flips to `needsReconnect`
  during revalidation) using the same `MongoFixture`/`FakeTeamsGraphService`
  pattern as Tasks 4/6 — `FakeTeamsGraphService` extended with a settable
  `SendMessageResponse` and a `GetTeamsCallCount` counter.
  `GraphInputTests.cs`'s 3 tests were rewritten against the new
  `ValidateMessageContent` (the method they tested no longer exists, since
  its only caller was removed per your answer — not a regression).
  `send-message.spec.ts` (4 tests): button disable rules, textarea-clears-
  and-shows-link on success. All 26 backend tests (21 + 5 new) and 16
  frontend tests (12 + 4 new) pass; `dotnet build` and `ng build` are both
  clean.

Manual verification (real Teams channel, real Entra session) — sending a
message and confirming a `409` when the saved destination becomes
inaccessible — was left for you to run.
