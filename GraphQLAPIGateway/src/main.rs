// Top-level modules in this crate. Think of these like separate C# files/namespaces.
// `graphql` contains the GraphQL schema and resolvers.
// `grpc_client` contains a small wrapper around the tonic (Rust gRPC) generated client.
mod graphql;
mod grpc_client;

use std::env;

// These are imports from external crates (libraries).
// - `async_graphql` provides GraphQL types and schema builder (similar in purpose to GraphQL.NET).
// - `async_graphql_axum` contains helpers to integrate async-graphql with Axum (the web framework).
// - `axum` is the web framework used here (similar to ASP.NET Core routing + middleware).
// - `tower_http::cors` is middleware for handling CORS (like the CORS middleware in ASP.NET Core).
use async_graphql::{EmptyMutation, EmptySubscription, Schema};
use async_graphql_axum::{GraphQLRequest, GraphQLResponse};
use axum::{extract::State, response::Html, routing::get, Router};
use tower_http::cors::{Any, CorsLayer};

use graphql::schema::QueryRoot;
use grpc_client::GrpcClient;

// Type alias to make the application Schema type easier to read.
// This is similar to creating a concrete `ISchema` type in .NET that embeds the Query/Mutation/Subscription types.
type AppSchema = Schema<QueryRoot, EmptyMutation, EmptySubscription>;

// HTTP handler for GraphQL POST requests.
// - `State(schema): State<AppSchema>` extracts an application-scoped value (the GraphQL `Schema`) from Axum's state.
//   This is similar to injecting a service from the ASP.NET Core DI container into a controller action.
// - `req: GraphQLRequest` is the incoming GraphQL request payload.
// The handler executes the request against the schema and converts the result into an HTTP response.
async fn graphql_handler(
    State(schema): State<AppSchema>,
    req: GraphQLRequest,
) -> GraphQLResponse {
    // `execute` is async and returns a GraphQL response; `.into()` converts it into the axum-friendly wrapper.
    schema.execute(req.into_inner()).await.into()
}

// Simple handler that returns the GraphiQL UI (an in-browser GraphQL playground).
// This is useful when you want to browse and run queries during development.
async fn graphiql_handler() -> Html<String> {
    Html(
        async_graphql::http::GraphiQLSource::build()
            .endpoint("/graphql")
            .finish(),
    )
}

// The application entrypoint.
// Notes for a .NET developer:
// - `#[tokio::main] async fn main()` is equivalent to `static async Task Main(string[] args)` in C#.
//   Tokio is the async runtime (like the ThreadPool + async/await infrastructure in .NET).
// - Axum plus `tokio::net::TcpListener` is used directly here to start an HTTP server.
//   In .NET you'd typically use Kestrel or ASP.NET Core - the concept is similar: bind a listener and serve a router.
#[tokio::main]
async fn main() {
    // Load variables from `.env` when present.
    // Environment variables already set by the host still take precedence.
    let _ = dotenvy::dotenv();

    let host = env::var("HOST").unwrap_or_else(|_| "localhost".to_string());
    let port = env::var("PORT")
        .ok()
        .and_then(|value| value.parse::<u16>().ok())
        .unwrap_or(4000);

    // Read the gRPC endpoint from an environment variable, with a default fallback.
    // Equivalent to `Environment.GetEnvironmentVariable("GRPC_ENDPOINT") ?? "http://localhost:8090"`.
    let grpc_endpoint =
        env::var("GRPC_ENDPOINT").unwrap_or_else(|_| "http://localhost:8090".to_string());

    println!("Connecting to gRPC endpoint: {}", grpc_endpoint);

    // Connect to the gRPC service. `GrpcClient::connect` returns a client wrapper around the generated tonic client.
    // This is similar to creating a `Channel` and a typed gRPC client in C# (e.g. new MyService.MyServiceClient(channel)).
    let grpc_client = GrpcClient::connect(grpc_endpoint)
        .await
        .expect("Failed to connect to gRPC service");

    // Build the GraphQL schema and inject the `grpc_client` as shared data accessible to resolvers.
    // In .NET this is comparable to registering a service in DI and then resolving it inside resolvers/controllers.
    let schema = Schema::build(QueryRoot, EmptyMutation, EmptySubscription)
        .data(grpc_client)
        .finish();

    // Configure permissive CORS for demo/dev (allow any origin/method/header).
    // In production you would tighten these rules (like configuring allowed origins in ASP.NET Core CORS options).
    let cors = CorsLayer::new()
        .allow_origin(Any)
        .allow_methods(Any)
        .allow_headers(Any);

    // Build the HTTP router:
    // - `GET /graphiql` serves the interactive playground
    // - `POST /graphql` is the GraphQL endpoint handled by `graphql_handler`
    // - `with_state(schema)` makes `schema` available via `State<AppSchema>` extractor (DI-like)
    // - `.layer(cors)` attaches the CORS middleware to all routes
    //
    // This is analogous to mapping endpoints in ASP.NET Core and adding middleware via `app.UseCors()` etc.
    let app = Router::new()
        .route("/graphql", get(graphiql_handler).post(graphql_handler))
        .route("/graphiql", get(graphiql_handler))
        .with_state(schema)
        .layer(cors);

    // Bind a TCP listener using HOST and PORT from environment.
    let bind_address = format!("{}:{}", host, port);
    let listener = tokio::net::TcpListener::bind(&bind_address)
        .await
        .unwrap_or_else(|_| panic!("Failed to bind to {}", bind_address));

    println!("GraphQL API Gateway running on http://{}", bind_address);
    println!("  POST /graphql  - GraphQL endpoint");
    println!("  GET  /graphiql - GraphQL playground");

    // Start serving requests. `axum::serve(listener, app)` is the runtime loop that accepts connections and routes them.
    // Like `host.RunAsync()` in ASP.NET Core, this call only returns on error or shutdown.
    axum::serve(listener, app)
        .await
        .expect("Server error");
}
