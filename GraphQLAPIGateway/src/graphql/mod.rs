// graphql/mod.rs — Module declaration for the GraphQL layer.
//
// Rust uses explicit module declarations in a `mod.rs` file to expose
// sub-modules to the rest of the crate.  This single line makes the
// `schema` module (defined in `graphql/schema.rs`) accessible as
// `graphql::schema` from the crate root — similar to having a
// namespace file or a `using` re-export in C#.
pub mod schema;
