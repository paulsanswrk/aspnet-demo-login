# Getting Started

## Prerequisites

| Requirement | Version | Notes |
|-------------|---------|-------|
| .NET SDK | 10.0+ | `dotnet --version` to verify |
| MSSQL Server | 2019+ | Developer Edition is fine |
| Authenticator App | — | Google Authenticator, Microsoft Authenticator, etc. |

### Installing .NET 10 SDK

```bash
# Using the official install script
wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --version 10.0.103

# Add to PATH
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"

# Verify
dotnet --version  # → 10.0.103
```

### Installing EF Core Tools

```bash
dotnet tool install --global dotnet-ef
```

---

## Configuration

### Connection String

Edit `appsettings.json` to match your MSSQL Server credentials:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=IdentityPortalDb;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;Encrypt=False;"
  }
}
```

> **Note**: For development, `TrustServerCertificate=True` and `Encrypt=False` bypass SSL certificate validation.

---

## Running the Application

```bash
# Navigate to project directory
cd /path/to/IdentityPortal

# Run on a specific port
dotnet run --urls "http://localhost:5100"
```

The application will:
1. Restore NuGet packages (first run)
2. Connect to MSSQL Server
3. Apply EF Core migrations automatically (`db.Database.Migrate()`)
4. Start listening on `http://localhost:5100`

---

## Database Management

### Viewing Migrations

```bash
dotnet ef migrations list
```

### Applying Migrations Manually

```bash
dotnet ef database update
```

### Adding New Migrations

```bash
dotnet ef migrations add MigrationName
```

### Resetting the Database

```bash
dotnet ef database drop --force
dotnet ef database update
```

---

## NuGet Packages

| Package | Purpose |
|---------|---------|
| `Microsoft.EntityFrameworkCore.SqlServer` | EF Core SQL Server provider |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Identity + EF Core integration |
| `Microsoft.EntityFrameworkCore.Design` | EF Core tooling (migrations) |
| `QRCoder` | QR code generation for authenticator setup |
