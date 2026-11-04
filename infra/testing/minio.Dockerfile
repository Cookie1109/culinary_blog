FROM alpine:3.23.4@sha256:5b10f432ef3da1b8d4c7eb6c487f2f5a8f096bc91145e68878dd4a5019afde11

ADD https://github.com/minio/minio/releases/download/RELEASE.2025-09-07T16-13-09Z/minio.linux-amd64.RELEASE.2025-09-07T16-13-09Z /usr/bin/minio
ADD https://github.com/minio/mc/releases/download/RELEASE.2025-08-13T08-35-41Z/mc.linux-amd64.RELEASE.2025-08-13T08-35-41Z /usr/bin/mc
RUN echo '7c5bd8512c6e966455b1d198209358b2d191c77a83ab377c4073281065fb855f  /usr/bin/minio' | sha256sum -c - \
    && echo '01f866e9c5f9b87c2b09116fa5d7c06695b106242d829a8bb32990c00312e891  /usr/bin/mc' | sha256sum -c - \
    && chmod 0555 /usr/bin/minio /usr/bin/mc \
    && mkdir /data \
    && chown 10001:10001 /data

ENV HOME=/tmp
USER 10001:10001
EXPOSE 9000
ENTRYPOINT ["/usr/bin/minio"]
CMD ["server", "/data"]
