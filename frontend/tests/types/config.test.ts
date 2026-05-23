describe("config", () => {
  beforeEach(() => {
    vi.resetModules();
  });

  it("throws when required environment variables are missing", async () => {
    vi.stubEnv("VITE_GRAPHQL_URL", "");
    vi.stubEnv("VITE_NOTIFICATIONS_URL", "");

    await expect(import("@/types/config")).rejects.toThrow(
      "Missing required environment variable: VITE_GRAPHQL_URL",
    );
  });

  it("loads graphql and notifications urls from the environment", async () => {
    vi.stubEnv("VITE_GRAPHQL_URL", "http://localhost:4000/graphql");
    vi.stubEnv(
      "VITE_NOTIFICATIONS_URL",
      "http://localhost:8092/notifications/motionHub",
    );

    const { default: config } = await import("@/types/config");

    expect(config.graphqlUrl).toBe("http://localhost:4000/graphql");
    expect(config.notificationsUrl).toBe(
      "http://localhost:8092/notifications/motionHub",
    );
  });
});
