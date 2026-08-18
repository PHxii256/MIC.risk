# MIC Risk Management Dashboard Plan

## Summary

Build a full role-based React dashboard for employees and risk-department admins, sourced from the provided OpenAPI contract. Establish a generated, validated API layer; add secure persistent sessions through rotating refresh tokens; and keep UI components presentational while domain hooks own side effects, caching, mutation state, and error handling.

## Platform and API Foundation

- Convert the Vite starter into the agreed frontend stack: Tailwind CSS, shadcn/ui, React Router, TanStack Query, React Hook Form, Zod, and the Hook Form Zod resolver.
- Add `@` path aliases and copy the supplied OpenAPI file into the frontend repository as the versioned API-generation source.
- Configure Orval to generate `src/api/generated/types.ts` and React Query endpoint hooks in `src/api/generated/endpoints.ts`; generation uses a custom typed fetch mutator, never raw requests from UI components.
- The custom client reads only `VITE_API_BASE_URL`, attaches the short-lived bearer token, forwards React Query’s abort signal, normalizes error responses into a typed application error, and parses unknown JSON against endpoint-specific Zod response schemas before returning it.
- Add a domain-model mapping layer between generated API types and UI models. It will normalize nullable values, dates, numeric strings, enum-like status values, and API error payloads before data reaches components.
- Configure `QueryClient` defaults appropriate for the dashboard: retry only transient failures, do not retry 401/403/404 or validation errors, use stable query keys, and invalidate narrowly after mutations.

## Authentication and Persistent Session API

- Extend the backend login response with `employeeId`, `roles`, an access token, and its expiry time. This removes the need to infer the current employee by enumerating users or decode JWT claims for UI authorization.
- Change JWT access tokens to a short lifetime (15 minutes) and add:
  - `POST /api/account/refresh` to issue a replacement access token and rotate the refresh token.
  - `POST /api/account/logout` to revoke the current refresh-token family and clear its cookie.
- Persist refresh tokens server-side as SHA-256 hashes with user ID, expiry, creation/revocation/replacement metadata, and a token-family identifier. Never store the plaintext token.
- Send the refresh token only in a `Secure`, `HttpOnly`, `SameSite=Strict`, path-scoped cookie. The app and API will be deployed on the same site; development CORS will explicitly allow the Vite origin with credentials.
- Rotate refresh tokens on every refresh, reject expired/revoked tokens, revoke the entire family when reuse is detected, and revoke active refresh sessions when an employee is deactivated.
- Keep the access token in frontend memory and restore a session on application start through the refresh endpoint. On 401, run one serialized refresh attempt, replay the original request once, then clear session state and redirect to login if refresh fails.
- Update the OpenAPI source and regenerate types/hooks after these backend contract changes.

## Dashboard, Routes, and Forms

- Create an application shell with responsive navigation and routes gated by role:
  - Employee: login, personal dashboard, submit risk report, my reports/detail/history, learning resources, quizzes/surveys, change password.
  - Admin: analytics dashboard, all reports/detail/triage, auditor evaluation, report status history, corrective actions, resource management/upload, employee and department management, risk taxonomy management, engagement analytics.
- Use the generated `use<EndpointName>` hooks through domain hooks such as `useRiskReports`, `useReportWorkflow`, `useResources`, and `useEmployees`. Presentational pages receive typed view models, commands, and explicit `idle/loading/success/error` state only.
- Place route-level React error boundaries around authenticated application sections and focused boundaries around complex analytics/report-detail areas. Provide safe recovery actions without exposing transport or server details.
- Match each list, table, detail, card grid, and form with layout-equivalent shadcn Skeleton loading states. Render empty, loading, forbidden, not-found, validation-error, and retry states distinctly.
- Implement all create/update forms with React Hook Form and Zod schemas derived from backend constraints:
  - Required text fields, email/password rules, positive identifiers, valid dates, risk ratings 1–5, valid risk statuses/categories, nullable optional fields, and file type/size checks before upload.
  - Keep server validation messages mapped into field or form errors without trusting them as client-side validation.
- Apply optimistic updates only where the action is immediately reversible and locally deterministic: report status changes, resource-engagement/quiz state, resource patches, employee active toggles, and risk-action updates. Snapshot the relevant cache, update it optimistically, roll back on failure, reconcile with the validated server response, and invalidate dependent dashboard queries.
- Use non-optimistic submit flows for account creation, report creation, file uploads, evaluations, and destructive deletes; disable duplicate submission and expose progress/error feedback.

## Reliability, Security, and Performance

- Generated queries use AbortSignal forwarding so navigation, filter changes, and unmounts cancel in-flight requests. Domain mutation hooks use a per-operation AbortController and prevent stale mutation results from overwriting newer UI state.
- Debounce report/resource search and filter inputs; include filters and pagination in query keys; preserve previous paginated data while the next page loads.
- Ensure uploads use `FormData`, have client-side file preflight checks, show cancellation/progress state, and never set a manual multipart content type.
- Keep authorization enforced by the backend; frontend role checks only control navigation and avoid presenting unavailable actions.
- Store no API keys or fixed API URLs in source. Provide documented `.env.example` values for the Vite API base URL only.

## Test Plan

- Unit-test Zod parsers, API-to-domain mappers, error normalization, JWT/session state behavior, and query-key factories.
- Test generated-client integration with mocked OpenAPI responses, including malformed payload rejection, abort propagation, 400/401/403/404/500 handling, and one-time refresh/replay behavior.
- Component-test all three UI states, route error-boundary recovery, role-based navigation, validation feedback, optimistic success/rollback, pagination, and disabled/repeated submissions.
- Backend-test login, refresh rotation, refresh-token reuse detection, logout, expiry, deactivated-account rejection, and cookie/CORS attributes.
- Run type checking, linting, production build, and end-to-end employee/admin critical workflows against the generated contract.

## Assumptions

- The implementation covers every current OpenAPI endpoint and uses the supplied `v1.json` as the generation source.
- The frontend and API share the same production site, allowing a `SameSite=Strict` refresh cookie.
- The generated API files are owned by the generator and are not edited manually; custom behavior lives in the fetch, validation, mapper, and domain-hook layers.
