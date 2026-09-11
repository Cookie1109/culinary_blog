FROM node:20.19.5-bookworm-slim@sha256:9e70124bd00f47dd023e349cd587132ae61892acc0e47ed641416c3e18f401c3 AS dependencies
WORKDIR /workspace/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci

FROM node:20.19.5-bookworm-slim@sha256:9e70124bd00f47dd023e349cd587132ae61892acc0e47ed641416c3e18f401c3 AS build
WORKDIR /workspace
COPY --from=dependencies /workspace/frontend/node_modules frontend/node_modules
COPY frontend frontend
COPY docs/specs/phase-0/openapi.v1.yaml docs/specs/phase-0/openapi.v1.yaml
RUN cd frontend && npm run generate:api && npm run build

FROM node:20.19.5-bookworm-slim@sha256:9e70124bd00f47dd023e349cd587132ae61892acc0e47ed641416c3e18f401c3 AS runtime
ENV NODE_ENV=production
ENV PORT=3000
ENV HOSTNAME=0.0.0.0
WORKDIR /app
RUN groupadd --system --gid 1001 nodejs \
    && useradd --system --uid 1001 --gid nodejs nextjs
COPY --from=build --chown=nextjs:nodejs /workspace/frontend/.next/standalone ./
COPY --from=build --chown=nextjs:nodejs /workspace/frontend/.next/static ./.next/static
USER nextjs
EXPOSE 3000
CMD ["node", "server.js"]
