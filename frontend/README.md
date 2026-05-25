# Frontend

A React single-page application that visualizes building metrics from the GraphQL API Gateway and displays live motion alerts via SignalR.

## Overview

The frontend provides three main views:

| Route | Page | Description |
|-------|------|-------------|
| `/` | Dashboard | Room-level charts for air quality and energy, with auto-refresh |
| `/parameters` | Parameters | Detailed metric tables and filters |
| `/motion` | Motion | Live motion status per room and event history |

Real-time motion notifications appear as toasts across all pages when the SignalR connection is active.

## Architecture

```
Browser
   │
   ├── GraphQL (graphql-request) ──▶ /graphql ──▶ GraphQL API Gateway
   │
   └── SignalR (@microsoft/signalr) ──▶ /notifications/motionHub ──▶ NotificationsService
```

In Docker Compose, nginx in the frontend container proxies both backends so the app can use relative URLs (see `nginx.conf`).

## Tech Stack

- **React 19** with React Router
- **Vite 6** + TypeScript
- **Tailwind CSS 4** for styling
- **Recharts** for metric trend charts
- **graphql-request** for GraphQL queries
- **@microsoft/signalr** for real-time motion events
- **Vitest** + Testing Library for unit tests

## Configuration

Copy the example environment file:

```bash
cp .env.example .env
```

| Variable | Description | Docker Compose default |
|----------|-------------|------------------------|
| `VITE_GRAPHQL_URL` | GraphQL endpoint | `/graphql` |
| `VITE_NOTIFICATIONS_URL` | SignalR hub URL | `/notifications/motionHub` |

For local development without Docker, point these to absolute URLs (e.g. `http://localhost:4000/graphql` and `http://localhost:8092/notifications/motionHub`).

## Running

### With Docker Compose

From the repository root:

```bash
docker compose up frontend
```

Open [http://localhost:3000](http://localhost:3000).

### Local development

```bash
npm install
npm run dev
```

Vite dev server starts on [http://localhost:5173](http://localhost:5173) by default. Set `VITE_*` variables in `.env` to reach the backend services.

### Build and preview

```bash
npm run build
npm run preview
```

### Tests and lint

```bash
npm test
npm run lint
```

## Project Structure

```
src/
├── pages/           # Dashboard, Parameters, Motion
├── components/      # Layout, charts, skeletons
├── context/         # SignalR notifications provider
├── hooks/           # Data-fetching hooks (e.g. useRooms)
├── lib/             # GraphQL client, SignalR client, formatting
└── types/           # GraphQL types, constants, config
```
