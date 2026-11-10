FROM postgres@sha256:123b17998ccaf47e53b502e8c6a1e79afd8f579c6f3eec83c563da026f957395

# This dedicated backup/restore image always runs without root; gosu is unused.
RUN rm /usr/local/bin/gosu
USER postgres
