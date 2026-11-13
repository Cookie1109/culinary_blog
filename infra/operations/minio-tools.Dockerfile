FROM --platform=$BUILDPLATFORM golang:1.27.3-alpine3.24@sha256:d200d2dd76b03a99b52651fd1325aff7dcd35422c35c01bac56571b0507f3deb AS build
ARG TARGETOS
ARG TARGETARCH
ADD https://github.com/minio/mc.git#7394ce0dd2a80935aded936b09fa12cbb3cb8096 /src
WORKDIR /src
COPY infra/operations/minio-tools/go.mod infra/operations/minio-tools/go.sum ./
RUN --mount=type=cache,target=/go/pkg/mod \
    CGO_ENABLED=0 GOOS=$TARGETOS GOARCH=$TARGETARCH go build -mod=readonly -trimpath -buildvcs=false \
    -ldflags="-s -w -X github.com/minio/mc/cmd.ReleaseTag=G06.REBUILD.20261031 -X github.com/minio/mc/cmd.CommitID=7394ce0dd2a80935aded936b09fa12cbb3cb8096" \
    -o /out/mc .

FROM alpine:3.24.3@sha256:be2656061b536c1883c4034790e21d0a09c5aaaf51968b89368a3c0371ea2c9a
RUN apk add --no-cache coreutils=9.11-r0 findutils=4.10.0-r1
COPY --from=build /out/mc /usr/local/bin/mc
COPY --from=build /src/LICENSE /usr/share/licenses/minio-mc/LICENSE
ENV HOME=/tmp MC_CONFIG_DIR=/tmp/mc
USER 1000:1000
ENTRYPOINT ["/usr/local/bin/mc"]
