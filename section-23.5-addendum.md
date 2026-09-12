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
