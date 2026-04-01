# Termínář Frontend

React + Mantine v9 web application for the Termínář course reservation system.

## Tech Stack

- React 19 + TypeScript
- Mantine v9 (UI components)
- React Router v7
- TanStack Query v5
- i18next (EN/CS)
- Vite

## Setup

### Prerequisites

- Node.js 18+
- pnpm

### Install

```bash
pnpm install
```

### Configure environment

Copy `.env.example` to `.env.local` and set the API URL:

```bash
cp .env.example .env.local
```

Edit `.env.local`:

```
VITE_API_BASE_URL=http://localhost:5000
```

### Development

```bash
pnpm dev
```

### Build

```bash
pnpm build
```

### Preview production build

```bash
pnpm preview
```

## Project Structure

```
src/
  app/                  # App-level setup (router, providers, theme)
  features/
    auth/               # Login, AuthContext, authApi
    landing/            # Landing page
    tenants/            # Tenant registration
    courses/            # Course list, detail, create, edit
    registrations/      # Course roster, registration management
    staff/              # Staff management
  shared/
    api/                # apiFetch client
    components/         # Shared UI (AppShell, StatusBadge, ConfirmModal, etc.)
    hooks/              # usePagination
    i18n/               # Translations (en/cs)
```

## Authentication

JWT Bearer token auth. The app stores `refreshToken`, `userId`, and `tenantSlug` in `localStorage`. On startup, it attempts to refresh the session automatically.

## Multi-tenancy

Each request includes an `X-Tenant-Slug` header identifying the tenant workspace.
