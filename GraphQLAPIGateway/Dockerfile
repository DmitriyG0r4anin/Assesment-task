# Build
FROM rust:1.89-slim AS builder

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
      ca-certificates \
      curl \
      protobuf-compiler \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY Cargo.toml Cargo.lock build.rs ./
COPY proto/ proto/
COPY src/ src/

RUN cargo build --release --locked

# Runtime
FROM debian:bookworm-slim

RUN apt-get update && \
    apt-get install -y --no-install-recommends ca-certificates && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app

RUN useradd --create-home --shell /usr/sbin/nologin appuser

COPY --from=builder /app/target/release/graphql-api-gateway /app/graphql-api-gateway
RUN chown appuser:appuser /app/graphql-api-gateway

USER appuser

EXPOSE 4000

CMD ["/app/graphql-api-gateway"]
