function requireEnv(value: string | undefined, name: string): string {
    if (!value?.trim()) {
        throw new Error(`Missing required environment variable: ${name}`);
    }
    return value;
}

function resolveServiceUrl(value: string): string {
    if (/^https?:\/\//i.test(value)) {
        return value;
    }

    const origin = globalThis.location?.origin ?? 'http://localhost';

    return new URL(value, origin).href;
}

const config = {
    graphqlUrl: resolveServiceUrl(requireEnv(import.meta.env.VITE_GRAPHQL_URL, 'VITE_GRAPHQL_URL')),
    notificationsUrl: resolveServiceUrl(
        requireEnv(import.meta.env.VITE_NOTIFICATIONS_URL, 'VITE_NOTIFICATIONS_URL'),
    ),
};

export default config;
