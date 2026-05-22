# Dependencies
FROM node:22-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci

# Build
FROM node:22-alpine AS build
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
ARG VITE_GRAPHQL_URL=/graphql
ARG VITE_NOTIFICATIONS_URL=/notifications/motionHub
ENV VITE_GRAPHQL_URL=$VITE_GRAPHQL_URL
ENV VITE_NOTIFICATIONS_URL=$VITE_NOTIFICATIONS_URL
RUN npm run build

# Serve
FROM nginx:1.27-alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
