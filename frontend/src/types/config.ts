function requireEnv(value: string | undefined, name: string): string {
  if (!value?.trim()) {
    throw new Error(`Missing required environment variable: ${name}`);
  }
  return value;
}

const config = {
  graphqlUrl: requireEnv(import.meta.env.VITE_GRAPHQL_URL, "VITE_GRAPHQL_URL"),
  notificationsUrl: requireEnv(
    import.meta.env.VITE_NOTIFICATIONS_URL,
    "VITE_NOTIFICATIONS_URL",
  ),
};

export default config;
