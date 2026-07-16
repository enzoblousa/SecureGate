# Email Sending (smtp4dev) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the log-only `UserRegisteredConsumer` behavior with a real SMTP email send, captured locally by `smtp4dev`, per `specs/05-email-sending.md`.

**Architecture:** New `IEmailSender` abstraction in `SecureGate.Application/Abstractions`, implemented by `SmtpEmailSender` (MailKit) in `SecureGate.Api/Infrastructure/Email`. `UserRegisteredConsumer` calls it inside a `try/catch` that swallows failures (same pattern as `RegisterUserService.PublishUserRegisteredEventAsync`). `smtp4dev` runs as a new `docker-compose.yml` service.

**Tech Stack:** .NET 8, MailKit, MassTransit (existing), docker-compose, xUnit + MassTransit test harness (existing patterns).

---

### Task 1: `IEmailSender` abstraction + `FakeEmailSender` test double

**Files:**
- Create: `src/SecureGate.Application/Abstractions/IEmailSender.cs`
- Create: `tests/SecureGate.Tests/Messaging/FakeEmailSender.cs`

- [ ] **Step 1: Create the `IEmailSender` interface**

```csharp
// src/SecureGate.Application/Abstractions/IEmailSender.cs
namespace SecureGate.Application.Abstractions;

public interface IEmailSender
{
    Task SendWelcomeEmailAsync(string toEmail, string name, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Create the `FakeEmailSender` test double**

```csharp
// tests/SecureGate.Tests/Messaging/FakeEmailSender.cs
using SecureGate.Application.Abstractions;

namespace SecureGate.Tests.Messaging;

public sealed class FakeEmailSender : IEmailSender
{
    private readonly List<(string ToEmail, string Name)> _sentEmails = new();

    public IReadOnlyList<(string ToEmail, string Name)> SentEmails => _sentEmails;

    public Exception? ExceptionToThrow { get; set; }

    public Task SendWelcomeEmailAsync(string toEmail, string name, CancellationToken cancellationToken = default)
    {
        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        _sentEmails.Add((toEmail, name));
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 3: Build to confirm everything compiles**

Run: `dotnet build`
Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add src/SecureGate.Application/Abstractions/IEmailSender.cs tests/SecureGate.Tests/Messaging/FakeEmailSender.cs
git commit -m "feat: add IEmailSender abstraction and fake test double"
```

---

### Task 2: `UserRegisteredConsumer` sends the welcome email (and swallows send failures)

**Files:**
- Modify: `tests/SecureGate.Tests/Messaging/UserRegisteredConsumerTests.cs`
- Modify: `src/SecureGate.Api/Infrastructure/Messaging/UserRegisteredConsumer.cs`

- [ ] **Step 1: Replace the test file with failing tests**

```csharp
// tests/SecureGate.Tests/Messaging/UserRegisteredConsumerTests.cs
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SecureGate.Api.Infrastructure.Messaging;
using SecureGate.Application.Abstractions;
using SecureGate.Application.Events;
using Xunit;

namespace SecureGate.Tests.Messaging;

public class UserRegisteredConsumerTests
{
    [Fact]
    public async Task Consume_SendsWelcomeEmailWithTheEventEmailAndName()
    {
        var fakeLogger = new FakeLogger<UserRegisteredConsumer>();
        var fakeEmailSender = new FakeEmailSender();

        var services = new ServiceCollection();
        services.AddSingleton<ILogger<UserRegisteredConsumer>>(fakeLogger);
        services.AddSingleton<IEmailSender>(fakeEmailSender);
        services.AddMassTransitTestHarness(x => x.AddConsumer<UserRegisteredConsumer>());

        await using var provider = services.BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var @event = new UserRegisteredEvent(Guid.NewGuid(), "Ana Souza", "ana@example.com", DateTime.UtcNow);
        await harness.Bus.Publish(@event);

        Assert.True(await harness.Consumed.Any<UserRegisteredEvent>());
        var sentEmail = Assert.Single(fakeEmailSender.SentEmails);
        Assert.Equal("ana@example.com", sentEmail.ToEmail);
        Assert.Equal("Ana Souza", sentEmail.Name);
    }

    [Fact]
    public async Task Consume_LogsSuccessMessageWithTheEventEmail()
    {
        var fakeLogger = new FakeLogger<UserRegisteredConsumer>();
        var fakeEmailSender = new FakeEmailSender();

        var services = new ServiceCollection();
        services.AddSingleton<ILogger<UserRegisteredConsumer>>(fakeLogger);
        services.AddSingleton<IEmailSender>(fakeEmailSender);
        services.AddMassTransitTestHarness(x => x.AddConsumer<UserRegisteredConsumer>());

        await using var provider = services.BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var @event = new UserRegisteredEvent(Guid.NewGuid(), "Ana Souza", "ana@example.com", DateTime.UtcNow);
        await harness.Bus.Publish(@event);

        Assert.True(await harness.Consumed.Any<UserRegisteredEvent>());
        Assert.Contains(fakeLogger.Messages, message => message.Contains("ana@example.com"));
    }

    [Fact]
    public async Task Consume_WhenEmailSendingFails_StillProcessesTheMessageWithoutThrowing()
    {
        var fakeLogger = new FakeLogger<UserRegisteredConsumer>();
        var fakeEmailSender = new FakeEmailSender { ExceptionToThrow = new InvalidOperationException("smtp4dev indisponível") };

        var services = new ServiceCollection();
        services.AddSingleton<ILogger<UserRegisteredConsumer>>(fakeLogger);
        services.AddSingleton<IEmailSender>(fakeEmailSender);
        services.AddMassTransitTestHarness(x => x.AddConsumer<UserRegisteredConsumer>());

        await using var provider = services.BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var @event = new UserRegisteredEvent(Guid.NewGuid(), "Ana Souza", "ana@example.com", DateTime.UtcNow);
        await harness.Bus.Publish(@event);

        Assert.True(await harness.Consumed.Any<UserRegisteredEvent>());
        Assert.Empty(fakeEmailSender.SentEmails);
    }
}
```

Note: `FakeLogger<T>` already exists at `tests/SecureGate.Tests/Messaging/FakeLogger.cs` — no change needed there.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter FullyQualifiedName~UserRegisteredConsumerTests`
Expected: build error or FAIL — `UserRegisteredConsumer` constructor doesn't accept `IEmailSender` yet, and it never calls `SendWelcomeEmailAsync`.

- [ ] **Step 3: Implement the consumer change**

```csharp
// src/SecureGate.Api/Infrastructure/Messaging/UserRegisteredConsumer.cs
using MassTransit;
using SecureGate.Application.Abstractions;
using SecureGate.Application.Events;

namespace SecureGate.Api.Infrastructure.Messaging;

public sealed class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<UserRegisteredConsumer> _logger;

    public UserRegisteredConsumer(IEmailSender emailSender, ILogger<UserRegisteredConsumer> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        try
        {
            await _emailSender.SendWelcomeEmailAsync(
                context.Message.Email,
                context.Message.Name,
                context.CancellationToken);

            _logger.LogInformation("E-mail de boas-vindas enviado para {Email}", context.Message.Email);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao enviar e-mail de boas-vindas para {Email}", context.Message.Email);
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter FullyQualifiedName~UserRegisteredConsumerTests`
Expected: `Passed! - Failed: 0, Passed: 3`

- [ ] **Step 5: Commit**

```bash
git add tests/SecureGate.Tests/Messaging/UserRegisteredConsumerTests.cs src/SecureGate.Api/Infrastructure/Messaging/UserRegisteredConsumer.cs
git commit -m "feat: send welcome email from UserRegisteredConsumer, swallow send failures"
```

---

### Task 3: `SmtpEmailSender` (MailKit) + configuration wiring

**Files:**
- Modify: `src/SecureGate.Api/SecureGate.Api.csproj` (add MailKit package)
- Create: `src/SecureGate.Api/Infrastructure/Email/SmtpSettings.cs`
- Create: `src/SecureGate.Api/Infrastructure/Email/SmtpEmailSender.cs`
- Modify: `src/SecureGate.Api/Program.cs`
- Modify: `src/SecureGate.Api/appsettings.json`

No new automated test in this task — `SmtpEmailSender` is a thin wrapper over MailKit, validated by the manual smoke test in Task 6 (per `specs/05-email-sending.md`, "Testes" section).

- [ ] **Step 1: Add the MailKit package**

Run: `dotnet add src/SecureGate.Api/SecureGate.Api.csproj package MailKit`
Expected: `info : PackageReference for package 'MailKit' ... added to file '...SecureGate.Api.csproj'.`

- [ ] **Step 2: Create `SmtpSettings`**

```csharp
// src/SecureGate.Api/Infrastructure/Email/SmtpSettings.cs
namespace SecureGate.Api.Infrastructure.Email;

public sealed class SmtpSettings
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string From { get; set; } = string.Empty;
}
```

- [ ] **Step 3: Create `SmtpEmailSender`**

```csharp
// src/SecureGate.Api/Infrastructure/Email/SmtpEmailSender.cs
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using SecureGate.Application.Abstractions;

namespace SecureGate.Api.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;

    public SmtpEmailSender(IOptions<SmtpSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string name, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_settings.From));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Bem-vindo(a) ao SecureGate";
        message.Body = new TextPart("html")
        {
            Text = $"<h1>Bem-vindo(a), {name}!</h1><p>Seu registro no SecureGate foi concluído com sucesso.</p>"
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.None, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
```

- [ ] **Step 4: Wire DI and options in `Program.cs`**

Add the using near the other `SecureGate.Api.Infrastructure.*` usings:

```csharp
using SecureGate.Api.Infrastructure.Email;
```

Add the registration next to the other `Configure<...Settings>` call:

```csharp
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection(SmtpSettings.SectionName));
```

Add the DI registration next to the other `AddScoped<I..., ...>` calls:

```csharp
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
```

- [ ] **Step 5: Add the `Smtp` section to `appsettings.json`**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=securegate;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Issuer": "SecureGate",
    "Audience": "SecureGate",
    "ExpirationMinutes": 60
  },
  "RabbitMq": {
    "Host": "localhost",
    "Username": "guest",
    "Password": "guest"
  },
  "Smtp": {
    "Host": "localhost",
    "Port": 2525,
    "From": "noreply@securegate.dev"
  }
}
```

(Same pattern as `RabbitMq`: this is the default for Opção B/local dev, pointing at the port exposed on the host; `docker-compose.yml` overrides it for the containerized API in Task 4.)

- [ ] **Step 6: Build and run the full test suite**

Run: `dotnet build`
Expected: `Build succeeded.`

Run: `dotnet test`
Expected: all tests pass, none broken by the new dependency/wiring.

- [ ] **Step 7: Commit**

```bash
git add src/SecureGate.Api/SecureGate.Api.csproj src/SecureGate.Api/Infrastructure/Email/SmtpSettings.cs src/SecureGate.Api/Infrastructure/Email/SmtpEmailSender.cs src/SecureGate.Api/Program.cs src/SecureGate.Api/appsettings.json
git commit -m "feat: implement SmtpEmailSender using MailKit"
```

---

### Task 4: `docker-compose.yml` — add the `smtp4dev` service

**Files:**
- Modify: `docker-compose.yml`

- [ ] **Step 1: Add the `smtp4dev` service, `api` env vars, and `depends_on` entry**

Add this service alongside `postgres` and `rabbitmq`:

```yaml
  smtp4dev:
    image: rnwood/smtp4dev:latest
    ports:
      - "5000:80"
      - "2525:25"
```

Add these three lines to the existing `api.environment` block (after `RabbitMq__Password`):

```yaml
      Smtp__Host: "smtp4dev"
      Smtp__Port: "25"
      Smtp__From: "noreply@securegate.dev"
```

Add this entry to the existing `api.depends_on` block (after `rabbitmq`):

```yaml
      smtp4dev:
        condition: service_started
```

The full `docker-compose.yml` after this change:

```yaml
services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: securegate
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    volumes:
      - postgres-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 5s
      timeout: 5s
      retries: 5

  rabbitmq:
    image: rabbitmq:3-management-alpine
    ports:
      - "5672:5672"
      - "15672:15672"
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "-q", "ping"]
      interval: 5s
      timeout: 5s
      retries: 10

  smtp4dev:
    image: rnwood/smtp4dev:latest
    ports:
      - "5000:80"
      - "2525:25"

  api:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    environment:
      ConnectionStrings__Default: "Host=postgres;Port=5432;Database=securegate;Username=postgres;Password=postgres"
      Jwt__Secret: "dev-only-secret-do-not-use-in-production-change-me-1234567890"
      RabbitMq__Host: "rabbitmq"
      RabbitMq__Username: "guest"
      RabbitMq__Password: "guest"
      Smtp__Host: "smtp4dev"
      Smtp__Port: "25"
      Smtp__From: "noreply@securegate.dev"
    depends_on:
      postgres:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy
      smtp4dev:
        condition: service_started

volumes:
  postgres-data:
```

- [ ] **Step 2: Validate the compose file**

Run: `docker compose config --quiet`
Expected: no output, exit code 0 (means the YAML is valid).

- [ ] **Step 3: Commit**

```bash
git add docker-compose.yml
git commit -m "feat: add smtp4dev service to docker-compose"
```

---

### Task 5: Document `smtp4dev` in the README

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Add the smtp4dev URL to the "Opção A" bullet list**

In the `### Opção A — tudo via Docker Compose (mais simples)` section, after the line `- RabbitMQ management UI: ...`, add:

```markdown
- smtp4dev (emails capturados): `http://localhost:5000`
```

- [ ] **Step 2: Add a row to the "Referência rápida" table**

After the `| docker compose down |` row, add:

```markdown
| `docker compose logs -f smtp4dev` | Ver logs do smtp4dev (captura de e-mail) |
```

- [ ] **Step 3: Commit**

```bash
git add README.md
git commit -m "docs: document smtp4dev in README"
```

---

### Task 6: Manual smoke test & close out acceptance criteria

**Files:**
- Modify: `specs/05-email-sending.md` (check off acceptance criteria)

- [ ] **Step 1: Rebuild and start everything**

Run: `docker compose up -d --build`
Expected: `postgres`, `rabbitmq`, `smtp4dev`, and `api` all report `Running`/`Started`.

- [ ] **Step 2: Confirm container health**

Run: `docker compose ps`
Expected: `api` container status `Up`, no restart loop.

- [ ] **Step 3: Register a new user**

Run:
```bash
curl -X POST http://localhost:8080/api/auth/register -H "Content-Type: application/json" -d "{\"name\":\"Smoke Test\",\"email\":\"smoke-test@example.com\",\"password\":\"senha1234\"}"
```
Expected: HTTP 201 with the created user's JSON body.

- [ ] **Step 4: Confirm the email arrived in smtp4dev**

Open `http://localhost:5000` in a browser and confirm a message to `smoke-test@example.com` with subject "Bem-vindo(a) ao SecureGate" is listed. Alternatively:

Run: `docker compose logs api --tail=20`
Expected: log line `E-mail de boas-vindas enviado para smoke-test@example.com` (no `Falha ao enviar` warning).

- [ ] **Step 5: Check off acceptance criteria in the spec**

Edit `specs/05-email-sending.md`, changing all six `- [ ]` under "Critérios de aceite" to `- [x]`.

- [ ] **Step 6: Commit**

```bash
git add specs/05-email-sending.md
git commit -m "docs: mark email-sending acceptance criteria complete"
```
