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

## Role-based access requirements

Role-based access works only when authentication and authorization are configured together. Authentication identifies the user, while authorization checks whether that user has the required role.

The following pieces are required in this solution.

### 1. Register cookie authentication and authorization

The API must register cookie authentication as its default authentication scheme and add authorization services:

```csharp
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();
```

Cookie authentication normally redirects users to an HTML login page. Because this project exposes an API, the redirect events return `401 Unauthorized` and `403 Forbidden` instead.

### 2. Add middleware in the correct order

`EmployeeDirectory.Api/Program.cs` must call authentication before authorization and before mapping controllers:

```csharp
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
```

`UseAuthentication` reads the encrypted cookie and creates `HttpContext.User`. `UseAuthorization` then compares that user's claims with the endpoint requirements. Reversing or omitting these calls prevents role checks from working correctly.

The MVC project requires the same middleware order because it maintains a separate web authentication cookie.

### 3. Create a role claim during login

A successful login must create both a name claim and a role claim:

```csharp
var claims = new[]
{
    new Claim(ClaimTypes.Name, username),
    new Claim(ClaimTypes.Role, role)
};
```

The role value must exactly match one of the constants in `EmployeeDirectory.Api/Authorization/AppRoles.cs`. Role names are case-sensitive in normal role checks. A cookie without a `ClaimTypes.Role` claim can authenticate the user but cannot authorize role-protected endpoints.

### 4. Issue and return the authentication cookie

After creating the claims, the login endpoint must call `HttpContext.SignInAsync`. ASP.NET Core encrypts the identity and sends it in the `EmployeeDirectory.Auth` cookie.

Clients must retain that cookie and send it with later API requests:

```http
Cookie: EmployeeDirectory.Auth=<encrypted-value>
```

Browsers handle this automatically when they call the API directly under a compatible cookie policy. The MVC application calls the API on the server, so it stores each user's API cookie inside that user's encrypted web authentication ticket and forwards it explicitly.

The named API `HttpClient` must keep `UseCookies = false`. `HttpClientFactory` pools message handlers, and a shared automatic cookie container could accidentally send one user's authentication cookie on another user's request.

### 5. Protect controllers and actions

The employee controller allows both roles to read:

```csharp
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Viewer}")]
public class EmployeeController : ControllerBase
```

Write actions add a stricter Admin requirement:

```csharp
[Authorize(Roles = AppRoles.Admin)]
```

The resulting access rules are:

| Employee operation | Anonymous | Viewer | Admin |
| --- | --- | --- | --- |
| List, search, or view employees | `401` | Allowed | Allowed |
| Add, update, or delete employees | `401` | `403` | Allowed |

UI checks are only for usability. Hiding Admin buttons from a Viewer does not secure the API; the API controller must always enforce the role requirement.

### 6. Keep the API and web application running

The MVC application depends on the API for login and employee operations. For role-based access to work through the website:

- both projects must be running;
- `EmployeeDirectory.Web/appsettings.json` must contain the correct API base URL;
- the API and web ports must match their launch profiles;
- the development HTTPS certificate must be trusted when HTTPS profiles are used;
- the browser must accept the web authentication cookie;
- the API process must keep consistent data-protection keys while a cookie is in use.

Restarting the API during development can invalidate an existing API cookie. If a signed-in user unexpectedly receives `401`, sign out and sign in again.

### 7. Verify every access path

Role-based access should be tested through the complete HTTP middleware pipeline, not only by calling controller methods directly. At minimum, verify:

1. an anonymous read returns `401`;
2. a Viewer read succeeds;
3. a Viewer write returns `403` without calling the employee service;
4. an Admin write reaches the employee service;
5. invalid credentials return `401`;
6. logout removes the authenticated session.

These cases are covered by `EmployeeDirectory.Api.Tests/AuthorizationTests.cs`.

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
dotnet test EmployeeDirectory.Api.slnx
```

To run only the existing API test project:

```powershell
dotnet test EmployeeDirectory.Api.Tests\EmployeeDirectory.Api.Tests.csproj
```

Run the complete Release build and all solution tests, matching the pipeline:

```powershell
dotnet restore EmployeeDirectory.Api.slnx
dotnet build EmployeeDirectory.Api.slnx --configuration Release --no-restore
dotnet test EmployeeDirectory.Api.slnx --configuration Release --no-build
```

`AuthorizationTests` hosts the real API middleware pipeline with `WebApplicationFactory<Program>`. The test environment skips the SQL Server migration and replaces `IEmployeeService` with a mock, so authorization tests do not require a database. They verify:

- anonymous requests receive `401`;
- Viewers can read employees;
- Viewers receive `403` for Admin-only changes;
- Admins can perform changes;
- invalid credentials receive `401`.

## GitHub Actions unit-test pipeline

The workflow is defined in `.github/workflows/unit-tests.yml`. GitHub automatically detects YAML files in the `.github/workflows` directory; no separate pipeline server is required.

The pipeline is generalized to run **all unit-test projects registered in `EmployeeDirectory.Api.slnx`**. It is not limited to `EmployeeDirectory.Api.Tests`, does not depend on a project name ending in `.Tests`, and does not need a separate workflow step for each test project. It can run xUnit, MSTest, or NUnit projects together as long as each project:

1. is added to `EmployeeDirectory.Api.slnx`;
2. references `Microsoft.NET.Test.Sdk`;
3. references its chosen test framework;
4. includes the matching test adapter when that framework requires one.

The solution-level command performs the generalized discovery:

```powershell
dotnet test EmployeeDirectory.Api.slnx --configuration Release --no-build --verbosity normal
```

When another correctly configured unit-test project is added to the solution, this command and the GitHub Actions workflow detect and run it automatically.

### Workflow configuration

```yaml
name: Build and unit test

on:
  push:
    branches: ["main", "master"]
  pull_request:

jobs:
  test:
    runs-on: windows-latest

    steps:
      - name: Check out repository
        uses: actions/checkout@v4

      - name: Set up .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"

      - name: Restore
        run: dotnet restore EmployeeDirectory.Api.slnx

      - name: Build
        run: dotnet build EmployeeDirectory.Api.slnx --configuration Release --no-restore

      - name: Run all solution tests
        run: dotnet test EmployeeDirectory.Api.slnx --configuration Release --no-build --verbosity normal
```

The YAML indentation is significant. Use spaces rather than tabs when editing the workflow.

### When the workflow runs

The `on` section configures two triggers:

- `push` runs the workflow after a commit is pushed to `main` or `master`;
- `pull_request` runs it when a pull request is opened, updated, or reopened.

Commits pushed only to another branch do not trigger the `push` rule, but they trigger the workflow when that branch has an open pull request.

### How each pipeline step works

The `test` job runs on a new `windows-latest` GitHub-hosted runner. Each run starts with a clean machine, so the workflow cannot rely on files from a student's computer or an earlier pipeline run.

1. **Check out repository**

   `actions/checkout@v4` downloads the commit being tested onto the runner. Later commands therefore use the exact source code from that push or pull request.

2. **Set up .NET**

   `actions/setup-dotnet@v4` installs a compatible .NET 10 SDK. The `10.0.x` value accepts the latest available .NET 10 patch release while keeping the major and minor version fixed.

3. **Restore**

   ```powershell
   dotnet restore EmployeeDirectory.Api.slnx
   ```

   Restore reads every project in the solution and downloads its NuGet dependencies, including xUnit, Moq, `Microsoft.NET.Test.Sdk`, and `Microsoft.AspNetCore.Mvc.Testing`. If a package cannot be restored, the job stops and is marked as failed.

4. **Build**

   ```powershell
   dotnet build EmployeeDirectory.Api.slnx --configuration Release --no-restore
   ```

   This compiles the API, MVC project, and test project in Release mode. `--no-restore` prevents an unnecessary second restore because the previous step already completed it. A compiler error fails the job and prevents the test step from running.

5. **Run tests**

   ```powershell
   dotnet test EmployeeDirectory.Api.slnx --configuration Release --no-build --verbosity normal
   ```

   Passing the solution instead of one `.csproj` makes the workflow independent of a particular test-project name. `dotnet test` checks every project registered in the solution. Projects that reference `Microsoft.NET.Test.Sdk` are recognized as test projects, and their test adapters discover tests such as xUnit `[Fact]` methods. `--no-build` tests the binaries created by the build step, and `--verbosity normal` writes test and failure details to the Actions log. Any failed test returns a non-zero exit code, which marks the workflow as failed.

### Why the pipeline does not need SQL Server

The authorization tests set the API environment to `Testing`. In this environment:

- startup skips the Entity Framework Core SQL Server migration;
- the tests replace `IEmployeeService` with a Moq test double;
- `WebApplicationFactory<Program>` runs the routing, cookie authentication, authorization, and controller pipeline in memory.

This keeps the pipeline repeatable and prevents tests from depending on a developer's SQL Server instance or connection string.

### Enable and verify the workflow on GitHub

1. Commit `.github/workflows/unit-tests.yml` to the repository.
2. Push the commit to GitHub.
3. Open the repository on GitHub.
4. Select the **Actions** tab.
5. Select **Build and unit test**.
6. Open the latest run and inspect the `test` job.
7. Expand a step to view its command output.

A green check means every workflow step succeeded. A red cross means at least one step failed. Open the failed step and read the first relevant error rather than only the final `Process completed with exit code 1` message.

GitHub Actions is normally enabled by default. If no run appears, a repository administrator should check **Settings > Actions > General** and confirm that actions are allowed.

### Require tests before merging

Repository administrators can prevent untested pull requests from being merged:

1. Open **Settings > Branches** or **Settings > Rules > Rulesets**.
2. Create a rule for `main`.
3. Enable **Require status checks to pass before merging**.
4. Select the status check produced by **Build and unit test**.
5. Save and enable the rule.

The status check appears only after the workflow has run at least once.

### Reproduce a failure locally

Run the same commands from the repository root:

```powershell
dotnet restore EmployeeDirectory.Api.slnx
dotnet build EmployeeDirectory.Api.slnx --configuration Release --no-restore
dotnet test EmployeeDirectory.Api.slnx --configuration Release --no-build --verbosity normal
```

Fix the first restore, compiler, or test error, run the commands again, then commit and push the fix. Do not solve pipeline failures by removing tests or ignoring failed commands.

### How test-project detection is generalized

The workflow does not search for a folder called `Tests` and does not hard-code `EmployeeDirectory.Api.Tests.csproj`. Project and folder names are conventions for people; they do not make a project executable by the test runner.

Test detection has two levels:

1. `dotnet test EmployeeDirectory.Api.slnx` loads every project registered in the solution.
2. `Microsoft.NET.Test.Sdk` identifies a project as a test project and starts its configured framework adapter.

The existing project contains these required packages:

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
<PackageReference Include="xunit" Version="2.9.3" />
<PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
```

- `Microsoft.NET.Test.Sdk` connects the project to `dotnet test`;
- `xunit` provides attributes and assertions such as `[Fact]`;
- `xunit.runner.visualstudio` supplies the adapter that discovers and runs xUnit tests.

MSTest or NUnit projects require their equivalent framework and adapter packages, but they can still be tested by the same solution-level command.

### Add another test project

Create the project and add it to the solution:

```powershell
dotnet new xunit -n EmployeeDirectory.Web.Tests
dotnet sln EmployeeDirectory.Api.slnx add EmployeeDirectory.Web.Tests\EmployeeDirectory.Web.Tests.csproj
dotnet add EmployeeDirectory.Web.Tests\EmployeeDirectory.Web.Tests.csproj reference EmployeeDirectory.Web\EmployeeDirectory.Web.csproj
```

Confirm that the generated test project references `Microsoft.NET.Test.Sdk`, xUnit, and the xUnit runner. Then run:

```powershell
dotnet test EmployeeDirectory.Api.slnx
```

The GitHub Actions workflow requires no change because the new project is part of the solution.

If a test project exists on disk but is not in `EmployeeDirectory.Api.slnx`, the solution-level command cannot detect it. Add it with `dotnet sln ... add` and commit both the test project and the updated solution file.

A pull request should not be merged until the workflow displays a green successful result.

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

## Announcement: Cookie Authentication Learning Resource

A complete example of cookie-based authentication and role-based authorization is available in the Employee Directory repository:

🔗 Repository: https://github.com/EMGPRS/empl0yee-directory-grp3

📘 Step-by-step guide: https://github.com/EMGPRS/empl0yee-directory-grp3/blob/main/README.md

The example demonstrates how to:

- configure cookie authentication in ASP.NET Core;
- implement login, logout, and current-user endpoints;
- create `Admin` and `Viewer` roles;
- protect API and MVC controller actions with `[Authorize]`;
- return the correct `401 Unauthorized` and `403 Forbidden` API responses;
- forward the authenticated API cookie from the MVC application;
- test authentication with `WebApplicationFactory`;
- run automated tests with GitHub Actions.

Students can clone the repository, follow the guide, and adapt the implementation for their own ASP.NET Core API and MVC projects.
