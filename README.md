# Employee Directory

Employee Directory is a .NET 10 teaching solution containing:

- `EmployeeDirectory.Api`: an ASP.NET Core Web API backed by SQL Server;
- `EmployeeDirectory.Web`: an ASP.NET Core MVC client;
- `EmployeeDirectory.Api.Tests`: controller and authorization tests.

The application demonstrates cookie authentication and role-based authorization with `Viewer` and `Admin` roles.

> The demonstration usernames and passwords are hard-coded for learning purposes. Never use this approach for production accounts.

## Prerequisites

Install the following before opening the solution:

1. [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
2. Visual Studio 2026 with the **ASP.NET and web development** workload, or Visual Studio Code with C# Dev Kit
3. SQL Server Express, SQL Server Developer, or LocalDB
4. Git

Confirm the SDK is available:

```powershell
dotnet --version
```

The output must begin with `10.`.

## 1. Clone and open the solution

```powershell
git clone <repository-url>
cd EmployeeDirectory.Api
dotnet restore EmployeeDirectory.Api.slnx
```

Open `EmployeeDirectory.Api.slnx` in Visual Studio, or open the repository folder in Visual Studio Code.

## 2. Configure SQL Server

Update `EmployeeDirectory.Api/appsettings.json` so `DefaultConnection` points to your SQL Server instance.

SQL Server Express example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=EmployeeDirectory;Integrated Security=True;TrustServerCertificate=True;"
  }
}
```

LocalDB example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=EmployeeDirectory;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

The API applies its Entity Framework Core migrations when it starts. To apply them manually instead, install the EF tool and run:

```powershell
dotnet tool install --global dotnet-ef
dotnet ef database update --project EmployeeDirectory.Api\EmployeeDirectory.Api.csproj
```

If `dotnet-ef` is already installed, use `dotnet tool update --global dotnet-ef`.

## 3. Trust the development HTTPS certificate

```powershell
dotnet dev-certs https --trust
```

The web project is configured to call the API at `https://localhost:7227/`. If that address changes, update `EmployeeDirectory.Web/appsettings.json`.

## 4. Run the application

Open two PowerShell terminals in the repository root.

Terminal 1 - start the API:

```powershell
dotnet run --project EmployeeDirectory.Api\EmployeeDirectory.Api.csproj --launch-profile https
```

Terminal 2 - start the MVC web application:

```powershell
dotnet run --project EmployeeDirectory.Web\EmployeeDirectory.Web.csproj --launch-profile https
```

Open `https://localhost:7205`. The application redirects unauthenticated users to the sign-in page.

In Visual Studio, configure multiple startup projects and set both `EmployeeDirectory.Api` and `EmployeeDirectory.Web` to **Start**.

## Demonstration users

| Username | Password | Role | Permissions |
| --- | --- | --- | --- |
| `viewer` | `viewer123` | Viewer | List, search, and view employees |
| `admin` | `admin123` | Admin | List, search, view, add, update, and delete employees |

An unauthenticated API request receives `401 Unauthorized`. A signed-in Viewer who calls an Admin-only endpoint receives `403 Forbidden`.

## How cookie authentication works

1. The user submits credentials to `POST /api/auth/login`.
2. The API validates the demonstration account and creates name and role claims.
3. ASP.NET Core returns an encrypted, HTTP-only `EmployeeDirectory.Auth` cookie.
4. The cookie is sent with later API requests.
5. The employee controller allows both roles to read data and requires `Admin` for changes.
6. `POST /api/auth/logout` invalidates the authentication session.

API authentication endpoints:

| Method | Route | Purpose |
| --- | --- | --- |
| `POST` | `/api/auth/login` | Validate credentials and issue a cookie |
| `GET` | `/api/auth/me` | Return the current username and role |
| `POST` | `/api/auth/logout` | remove the authentication cookie |

The MVC project has its own encrypted sign-in cookie. It stores the current user's API cookie in that user's authentication ticket and forwards it explicitly on API requests. Automatic cookies are disabled on the named `HttpClient` because pooled handlers must not share one user's cookie with another user.

The web UI hides add, edit, and delete controls from Viewers, and its controller restricts write actions to Admins. The API independently applies the same role checks and remains the security boundary.

## Test the API manually

Open `EmployeeDirectory.Api/EmployeeDirectory.Api.http` in Visual Studio or Visual Studio Code with the REST Client extension. Run requests in this order:

1. Viewer login
2. Employee list - expect `200 OK`
3. Delete as Viewer - expect `403 Forbidden`
4. Admin login
5. Add or delete as Admin - expect success
6. Current user
7. Logout

The HTTP client retains cookies between requests. Log out before switching users if your editor does not replace the earlier cookie automatically.

Swagger is available at `https://localhost:7227/swagger` while the API is running. Swagger UI does not automatically provide a cookie login workflow, so the `.http` file is the easiest manual test.

## Run the automated tests

Run all tests:

```powershell
dotnet test EmployeeDirectory.Api.Tests\EmployeeDirectory.Api.Tests.csproj
```

Run the complete release build and tests, matching the pipeline:

```powershell
dotnet restore EmployeeDirectory.Api.slnx
dotnet build EmployeeDirectory.Api.slnx --configuration Release --no-restore
dotnet test EmployeeDirectory.Api.Tests\EmployeeDirectory.Api.Tests.csproj --configuration Release --no-build
```

`AuthorizationTests` hosts the real API middleware pipeline with `WebApplicationFactory<Program>`. The test environment skips the SQL Server migration and replaces `IEmployeeService` with a mock, so authorization tests do not require a database. They verify:

- anonymous requests receive `401`;
- Viewers can read employees;
- Viewers receive `403` for Admin-only changes;
- Admins can perform changes;
- invalid credentials receive `401`.

## GitHub Actions unit-test pipeline

`.github/workflows/unit-tests.yml` runs for pull requests and pushes to `main` or `master`. The Windows runner:

1. checks out the repository;
2. installs the .NET 10 SDK;
3. restores the solution;
4. builds the solution in Release mode;
5. runs the test project.

To see a pipeline result on GitHub, push the branch and open the repository's **Actions** tab. A pull request cannot be merged safely until the workflow is green.

## Add or change a role

1. Add the role constant to `EmployeeDirectory.Api/Authorization/AppRoles.cs`.
2. Update the relevant `[Authorize(Roles = "...")]` attributes.
3. Ensure login adds a matching `ClaimTypes.Role` claim.
4. Mirror the role in the MVC project when the web UI needs it.
5. Add tests for anonymous, allowed, and forbidden requests.
6. Run the complete test suite.

Comma-separated role names mean either role is accepted. Separate `[Authorize]` attributes are cumulative, so do not stack role attributes when the intention is "Admin or Viewer."

## Production checklist

Before adapting this example for a real system:

- replace hard-coded users with ASP.NET Core Identity or an external identity provider;
- store salted password hashes, never plain-text passwords;
- use HTTPS in every environment;
- persist and protect data-protection keys across deployments;
- configure cookie lifetime, `Secure`, `SameSite`, and consent requirements;
- retain anti-forgery protection on cookie-authenticated write and logout forms;
- add login throttling, account lockout, password reset, and audit logging;
- never log passwords or cookie values;
- move connection strings and other secrets to user secrets, environment variables, or a secret manager.
