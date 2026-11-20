# Public ISR simulation

The local `culinary-public` member uses the repository's OpenAPI contract and synthetic DTOs. It stands in for this repository's API only; it is not evidence that the API or storage implementation passes. No owning external service is involved.

- Runtime: Node 22.23.3 and the installed Postman CLI 1.76.0. `frontend/scripts/check-isr.mjs` starts/stops the simulation and a standalone Next production process on allocated ports. Run it from `frontend` using the pinned Node runtime; omit `--skip-build` after source changes.
- The fixture control POST `/__fixture/revision` changes shared state under `culinary.public.revision`. The recipes/category GETs reflect that revision. Unknown resources return 404; calls outside this fixture return 501.
- G04 adds `/api/v1/recipes/isr-old-slug` -> HTTP301 with a relative current-slug Location. The current recipe has a long title, a short description, and empty non-null Instructions to exercise the current SEO/data contract. The API implementation's slug/privacy rules are tested independently with PostgreSQL integration tests.
- Run results and which member served requests are recorded in the G04 package report after the production run; the NDJSON log stays under ignored `tmp/g04` because request logs can contain headers/bodies.


G04 verified run: `tmp/g04/isr-release/evidence.json` records40 HTTP checks,0 failures; `simulation.log` request events identify routeKey `culinary`, served by this simulation's `culinary-public/default` function member. Calls include the historical-slug301 and current recipe200. No calls were satisfied by an external API. Real API/storage behavior is covered separately by the G04 integration suite and six real-stack groups. See `docs/specs/srs-completion-phases/reports/package-G04-result.md` and its source manifest for artifact scope/hashes.
