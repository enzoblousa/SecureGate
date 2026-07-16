# 05 — Envio real de email (smtp4dev)

Ver decisões gerais em [00-architecture-overview.md](00-architecture-overview.md) e o
fluxo de mensageria em [03-messaging.md](03-messaging.md).

## Objetivo

`03-messaging.md` deixou o `UserRegisteredConsumer` apenas logando "e-mail de
boas-vindas enviado para {email}" — decisão consciente de v1 (foco era mensageria).
Esta etapa substitui o log por um envio SMTP real, capturado por um servidor SMTP
local de desenvolvimento (`smtp4dev`), sem depender de credenciais reais nem de um
provedor externo.

## Regras de negócio

1. O email de boas-vindas é enviado **depois** que o `UserRegisteredEvent` é
   consumido — mesmo ponto onde hoje só existe o log (nenhuma mudança no fluxo de
   registro/publicação já documentado em `03-messaging.md`).
2. **Falha ao enviar o email não derruba o processamento da mensagem RabbitMQ.**
   Mesma lógica da regra 2 de `03-messaging.md` para a publicação do evento: o erro é
   logado e engolido dentro do consumer — sem retry de aplicação, sem DLQ dedicada
   (fora de escopo, ver abaixo).
3. O corpo do email é HTML simples, embutido como string no `SmtpEmailSender` — sem
   engine de template.
4. Endereço remetente fixo por configuração (`Smtp:From`), sem lógica de negócio
   adicional sobre remetente/assunto.

## Componentes

- **`SecureGate.Application/Abstractions/IEmailSender.cs`** (novo):
  ```csharp
  public interface IEmailSender
  {
      Task SendWelcomeEmailAsync(string toEmail, string name, CancellationToken cancellationToken = default);
  }
  ```
  Mesmo padrão de abstração já usado para `IUserRepository`/`IEventPublisher`/`IPasswordHasher`.

- **`SecureGate.Api/Infrastructure/Email/SmtpEmailSender.cs`** (novo): implementa
  `IEmailSender` usando `MailKit.Net.Smtp.SmtpClient`. Monta o corpo HTML simples de
  boas-vindas e conecta usando `Smtp:Host`/`Smtp:Port`/`Smtp:From` (configuração,
  mesmo padrão de `RabbitMq:*`/`ConnectionStrings:Default`).

- **`UserRegisteredConsumer`** (existente, `Api/Infrastructure/Messaging`): passa a
  injetar `IEmailSender` e, dentro de `Consume`, chama `SendWelcomeEmailAsync`
  envolvido em `try/catch` (regra de negócio 2). Mantém o log de
  "e-mail de boas-vindas enviado para {email}" em caso de sucesso; loga erro em nível
  `Warning` em caso de falha.

- **`Program.cs`**: registra `IEmailSender` → `SmtpEmailSender` no DI.

## Fluxo de dados

Estende o diagrama de `03-messaging.md`: após `Bus-->>Consumer: consome
UserRegisteredEvent`, o consumer chama `IEmailSender.SendWelcomeEmailAsync`, que abre
uma conexão SMTP com o `smtp4dev` e envia a mensagem. O `smtp4dev` captura o email e
disponibiliza na UI web e por API REST — o request HTTP de registro não espera nenhuma
parte disso; o client já recebeu `201` assim que o usuário foi salvo.

## Infraestrutura (`docker-compose.yml`)

Novo serviço:

```yaml
smtp4dev:
  image: rnwood/smtp4dev:latest
  ports:
    - "5000:80"    # UI web
    - "2525:25"    # porta SMTP
```

`api` ganha `depends_on: smtp4dev` e as variáveis `Smtp__Host: "smtp4dev"`,
`Smtp__Port: "25"`, `Smtp__From: "noreply@securegate.dev"`.

Para a Opção B do README (API local + infraestrutura via Docker), a porta externa
`2525` entra em `appsettings.Development.json` (`Smtp:Host=localhost`,
`Smtp:Port=2525`), mesmo padrão já usado hoje para Postgres/RabbitMQ nessa opção.

README ganha uma entrada na tabela de referência rápida e uma menção à UI do
smtp4dev na seção "Opção A".

## Testes

- `UserRegisteredConsumer`: teste com `AddMassTransitTestHarness` (já usado em
  `03-messaging.md`) + um `FakeEmailSender`, verificando que
  `SendWelcomeEmailAsync` foi chamado com o email e nome corretos após o evento ser
  consumido.
- Teste cobrindo: uma falha simulada no `FakeEmailSender` (lança exceção) não impede
  o consumer de processar a mensagem nem propaga a exceção (regra de negócio 2).
- `SmtpEmailSender` não ganha teste unitário — é uma casca fina sobre o MailKit.
  Validação real fica para o smoke test manual: `docker compose up`, registrar um
  usuário via `POST /api/auth/register`, e conferir o email na UI do smtp4dev
  (`http://localhost:5000`).

## Fora de escopo desta etapa

- **Templates de email reutilizáveis / engine de template** (Razor, Scriban, etc.).
- **Outros tipos de email** (recuperação de senha, confirmação de conta) — só o
  welcome email desta etapa.
- **Envio para provedor externo real em produção** (SendGrid, SES, etc.) — candidato a
  v2; a configuração `Smtp:*` já deixa a porta aberta para trocar de provedor sem
  mudar código.
- **Confirmação de leitura / verificação de email (double opt-in)**.
- **Retry/dead-letter queue customizados** para falha de envio — mesmo escopo já
  excluído em `03-messaging.md`.

## Critérios de aceite

- [ ] Registrar um usuário via `POST /api/auth/register` resulta num email de
      boas-vindas capturado pelo smtp4dev, visível na UI web.
- [ ] Falha simulada no envio (`FakeEmailSender` lança exceção) não impede o consumer
      de processar a mensagem nem propaga exceção.
- [ ] Teste automatizado cobrindo: consumer chama `IEmailSender.SendWelcomeEmailAsync`
      com os dados corretos após consumir o evento.
- [ ] Teste automatizado cobrindo: falha no envio de email é engolida (não derruba o
      processamento da mensagem).
- [ ] `docker compose up -d --build` sobe o `smtp4dev` junto com os demais serviços,
      saudável.
- [ ] Smoke test manual: registrar um usuário e ver o email correspondente na UI do
      smtp4dev (`http://localhost:5000`).
