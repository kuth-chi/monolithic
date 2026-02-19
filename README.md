# monolithic

Backend source code for the monolithic business management platform, built with **ASP.NET Core Web API (Controllers)** on **.NET 10**.

## Current architecture

- **Style:** Modular Monolith (clean module boundaries)
- **Framework:** ASP.NET Core Web API (controller-based)
- **Runtime:** .NET 10
- **Infra (Docker):** PostgreSQL, Redis, RabbitMQ
- **Caching strategy:** L1 `IMemoryCache` + L2 Redis distributed cache
- **Identity:** ASP.NET Core Identity with EF Core (PostgreSQL) + Role-based + Permission-based authorization
- **Authorization:** Fine-grained permission system with multi-layer access control

### Initial modules scaffolded

- `Users` (role/permission-ready user endpoints)
- `Analytics` (real-time dashboard endpoint scaffold)

These modules are intentionally designed for high reusability (DRY), and can be extended into:

- Employee Management
- Business
- Finance (Accounting)
- Customer Management
- Vendor Management
- Inventory Management
- Sales Management
- Purchase Management
- Business Analytics (Dashboard + Report)

## Run with Docker

1. Copy `.env.example` to `.env` (already created locally) and set secure values.
2. Start services with Docker Compose from this folder.
3. API is exposed on `http://localhost:8080`.

### Included services

- `api` (ASP.NET Core Web API)
- `postgres` (`5432`)
- `redis` (`6379`)
- `rabbitmq` (`5672`, management UI `15672`)

## API endpoints (starter)

- `GET /healthz`
- `POST /api/v1/auth/login` (placeholder)
- `GET /api/v1/auth/me` (authorized)
- `POST /api/v1/auth/logout` (authorized)
- `GET /api/v1/users`
- `GET /api/v1/users/{id}`
- `POST /api/v1/users`
- `GET /api/v1/dashboard/realtime`

## Seeded Users (on first startup)

| Email | Password | Role | Notes |
| ------- | ---------- | ------ | ------- |
| `admin@example.com` | `AdminPassword123!` | Owner | Full system access (`*:full`) |
| `accountant@example.com` | `AccountantPassword123!` | Staff | Accounting, inventory, reporting |
| `sales@example.com` | `SalesPassword123!` | Staff | Sales, customers, reporting |
| `user1@example.com` | `UserPassword123!` | User | Read-only access |

See the [Identity & Authorization guide](../monolithic.wiki/Identity.md) in the wiki for complete details on roles, permissions, and authorization usage.

## Next recommended step

Implement JWT token generation in AuthController.Login() and add real authentication middleware for production readiness.
