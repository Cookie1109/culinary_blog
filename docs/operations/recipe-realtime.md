# Recipe lifecycle realtime

The browser connects to `/api/recipe-events` on the web server. The web server
proxies `/api/v1/recipe-events` on the API. Redis Pub/Sub distributes committed
content-change signals across API replicas. Events contain only `{}`; clients
refetch their own authorized data instead of receiving recipe or account data.

The existing mutation pipeline invalidates backend caches before the scheduler
publishes a signal. Failed writes publish nothing. Events are coalesced at most
once per second per subscriber, with heartbeat comments every 15 seconds.

Public server-rendered content uses `no-store` reads so a realtime refresh cannot
receive stale ISR HTML or data. Backend generation-aware Redis and output caches
remain enabled. This trades the previous Next.js public-page ISR cache for fresh
state on each navigation or realtime refresh. Existing revalidation callbacks
remain supported. Nginx routes `/api/recipe-events` to **web**, with buffering and
proxy caching disabled; both checked-in configurations include this route.

The browser refreshes public server-rendered pages and invalidates recipe lists,
category lists and dashboard counts. Editor forms do not receive an automatic
route refresh, preserving unsaved input and existing concurrency checks.

`EventSource` reconnects automatically. A `connected` event refetches current
state, including changes missed during disconnection; there is no event replay
or per-event delivery guarantee. Redis reconnection also prompts active streams
to resynchronize. Existing scheduled public-content refresh remains in place.

Verification:

```powershell
cd frontend
node --test tests/recipe-realtime.test.mjs
node tests/recipe-realtime.e2e.mjs
```

The live test targets the local Docker stack by default and uses the configured
local admin credentials. Override `REALTIME_TEST_URL`, `REALTIME_TEST_EMAIL`,
`REALTIME_TEST_PASSWORD` and `CHROME_PATH` when necessary. It creates a temporary
recipe and deletes that recipe in `finally`; it does not alter existing recipes.
