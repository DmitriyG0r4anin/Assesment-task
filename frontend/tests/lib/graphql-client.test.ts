vi.mock("@/types/config", () => ({
  default: {
    graphqlUrl: "http://localhost:4000/graphql",
  },
}));

const graphqlClientMock = vi.fn();

vi.mock("graphql-request", () => ({
  GraphQLClient: class {
    constructor(endpoint: string, options: unknown) {
      graphqlClientMock(endpoint, options);
    }
  },
}));

describe("graphqlClient", () => {
  beforeEach(() => {
    vi.resetModules();
    graphqlClientMock.mockClear();
  });

  it("uses the configured GraphQL endpoint", async () => {
    await import("@/lib/graphql-client");

    expect(graphqlClientMock).toHaveBeenCalledWith(
      "http://localhost:4000/graphql",
      {
        headers: {
          "Content-Type": "application/json",
        },
      },
    );
  });
});
