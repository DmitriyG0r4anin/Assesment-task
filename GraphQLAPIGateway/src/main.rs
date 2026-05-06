mod graphql;
mod grpc_client;

use std::env;

use async_graphql::{EmptyMutation, EmptySubscription, Schema};
use async_graphql_axum::{GraphQLRequest, GraphQLResponse};
use axum::{extract::State, response::Html, routing::get, Router};
use tower_http::cors::{Any, CorsLayer};
use tower_http::trace::TraceLayer;
use tracing_subscriber::{fmt, prelude::*, EnvFilter};

use graphql::schema::QueryRoot;
use grpc_client::GrpcClient;

type AppSchema = Schema<QueryRoot, EmptyMutation, EmptySubscription>;

async fn graphql_handler(State(schema): State<AppSchema>, req: GraphQLRequest) -> GraphQLResponse {
    let req_inner = req.into_inner();
    tracing::debug!(operation_name = ?req_inner.operation_name, "executing GraphQL request");
    let response = schema.execute(req_inner).await;
    if !response.errors.is_empty() {
        for err in &response.errors {
            tracing::warn!(message = %err.message, path = ?err.path, "GraphQL field error");
        }
    }
    response.into()
}

async fn graphiql_handler() -> Html<String> {
    Html(
        async_graphql::http::GraphiQLSource::build()
            .endpoint("/graphql")
            .finish(),
    )
}

#[tokio::main]
async fn main() {
    let _ = dotenvy::dotenv();

    let filter = EnvFilter::try_from_default_env()
        .unwrap_or_else(|_| EnvFilter::new("graphql_api_gateway=info,tower_http=info,info"));
    tracing_subscriber::registry()
        .with(filter)
        .with(fmt::layer())
        .init();

    let host = env::var("HOST").unwrap_or_else(|_| "localhost".to_string());
    let port = env::var("PORT")
        .ok()
        .and_then(|value| value.parse::<u16>().ok())
        .unwrap_or(4000);

    let grpc_endpoint =
        env::var("GRPC_ENDPOINT").unwrap_or_else(|_| "http://localhost:5203".to_string());

    tracing::info!(endpoint = %grpc_endpoint, "connecting to DataProcessor gRPC");

    let grpc_client = GrpcClient::connect(grpc_endpoint)
        .await
        .expect("Failed to connect to gRPC service");
    tracing::info!("gRPC channel ready (all four service clients share this connection)");

    let schema = Schema::build(QueryRoot, EmptyMutation, EmptySubscription)
        .data(grpc_client)
        .finish();

    let cors = CorsLayer::new()
        .allow_origin(Any)
        .allow_methods(Any)
        .allow_headers(Any);

    let app = Router::new()
        .route("/graphql", get(graphiql_handler).post(graphql_handler))
        .route("/graphiql", get(graphiql_handler))
        .with_state(schema)
        .layer(TraceLayer::new_for_http())
        .layer(cors);

    let bind_address = format!("{}:{}", host, port);
    let listener = tokio::net::TcpListener::bind(&bind_address)
        .await
        .unwrap_or_else(|_| panic!("Failed to bind to {}", bind_address));

    tracing::info!(%bind_address, "GraphQL API Gateway listening");
    tracing::info!("POST /graphql — GraphQL endpoint; GET /graphiql — playground");

    axum::serve(listener, app).await.expect("Server error");
}
