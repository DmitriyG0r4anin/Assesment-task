# Build stage
FROM rust:1.89-slim AS builder

# Install build dependencies:
# - curl: fallback used by build.rs if protoc is somehow not found
# - ca-certificates: needed for HTTPS requests
# - protobuf-compiler: provides the `protoc` binary required by tonic-build
#   to compile .proto files into Rust code at build time
RUN apt-get update && \
    apt-get install -y \
      curl \
      ca-certificates \
      protobuf-compiler \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy build files
COPY Cargo.toml Cargo.lock build.rs ./
COPY proto/ proto/
COPY src/ src/

# Build the release binary.
# protoc is now available in PATH so build.rs will find it immediately
# via `which protoc` without needing to download anything.
RUN cargo build --release

# Runtime stage
FROM debian:bookworm-slim

# Install minimal runtime dependencies
RUN apt-get update && \
    apt-get install -y ca-certificates && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy binary from builder
COPY --from=builder /app/target/release/graphql-api-gateway /app/graphql-api-gateway

EXPOSE 4000

CMD ["/app/graphql-api-gateway"]
