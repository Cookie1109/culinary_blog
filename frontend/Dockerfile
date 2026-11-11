FROM node:22.23.3-bookworm-slim@sha256:43ac6c60b8f89723f746e8a92ce91abd5017e627ce1ddfe4238355d3a30b772c AS dependencies
WORKDIR /workspace/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci

FROM node:22.23.3-bookworm-slim@sha256:43ac6c60b8f89723f746e8a92ce91abd5017e627ce1ddfe4238355d3a30b772c AS build
WORKDIR /workspace
COPY --from=dependencies /workspace/frontend/node_modules frontend/node_modules
COPY frontend frontend
COPY docs/specs/phase-0/openapi.v1.yaml docs/specs/phase-0/openapi.v1.yaml
RUN cd frontend && npm run generate:api && npm run build
RUN rm -rf frontend/.next/standalone/node_modules/@img/sharp-wasm32 \
    frontend/.next/standalone/node_modules/@img/sharp-linuxmusl-x64 \
    frontend/.next/standalone/node_modules/@img/sharp-libvips-linuxmusl-x64

FROM gcr.io/distroless/nodejs22-debian13:nonroot@sha256:1b90ca047c3a1046ad3527998c747c25b6a88dde91ba8c71b98669d5813aaa21 AS runtime
ENV NODE_ENV=production
ENV PORT=3000
ENV HOSTNAME=0.0.0.0
WORKDIR /app
COPY --from=build --chown=65532:65532 /workspace/frontend/.next/standalone ./
COPY --from=build --chown=65532:65532 /workspace/frontend/.next/static ./.next/static
USER 65532:65532
EXPOSE 3000
CMD ["server.js"]
