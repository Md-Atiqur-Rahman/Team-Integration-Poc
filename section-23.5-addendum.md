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
