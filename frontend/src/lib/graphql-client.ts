import { GraphQLClient } from "graphql-request";
import config from "../types/config";

const endpoint = config.graphqlUrl;

export const graphqlClient = new GraphQLClient(endpoint, {
  headers: {
    "Content-Type": "application/json",
  },
});
