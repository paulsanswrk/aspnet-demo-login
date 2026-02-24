# Identity Portal

A professional ASP.NET Core 10 login portal demo featuring TOTP-based Two-Factor Authentication, MSSQL Server, and ASP.NET Core Identity with best-practice security patterns.

## Quick Start

```bash
# Prerequisites: .NET 10 SDK, MSSQL Server running locally

# Restore & run
dotnet run --urls "http://localhost:5100"
```

The database is created and migrated automatically on first startup.

## Documentation

| Document | Description |
|----------|-------------|
| [Getting Started](docs/getting-started.md) | Prerequisites, setup, and running the project |
| [Architecture](docs/architecture.md) | Project structure, design decisions, and data flow |
| [Authentication Flow](docs/authentication-flow.md) | Registration, login, 2FA setup, and enforcement |
| [Security](docs/security.md) | Identity config, lockout, security stamps, and policies |

## Tech Stack

- **Framework**: ASP.NET Core 10 (LTS) with Razor Pages
- **Database**: MSSQL Server + Entity Framework Core 10
- **Auth**: ASP.NET Core Identity + TOTP 2FA
- **QR Code**: QRCoder library
- **UI**: Custom dark theme (no CSS frameworks)

## License

MIT
