# 03 — Messaging (RabbitMQ + MassTransit)

Ver decisões gerais em [00-architecture-overview.md](00-architecture-overview.md).

## Objetivo

Ao registrar um usuário com sucesso (ver [01-user-registration.md](01-user-registration.md)),
publicar um evento `UserRegisteredEvent` numa fila RabbitMQ via MassTransit. Um
consumer, rodando no mesmo processo da API, escuta esse evento e apenas registra em
log "e-mail de boas-vindas enviado para {email}" — não envia e-mail de verdade. Esta é
a etapa central do projeto: o objetivo declarado do portfólio é praticar mensageria.

## Regras de negócio

1. O evento é publicado **depois** que o usuário já foi persistido no PostgreSQL —
   nunca antes (consistente com o fluxo em
   [00-architecture-overview.md](00-architecture-overview.md)).
2. **Falha ao publicar o evento não derruba o registro.** Sem Outbox Pattern nesta v1
   (ver "Fora de escopo"), não há garantia transacional entre salvar o usuário e
   publicar o evento — é uma decisão consciente. Se o broker estiver fora do ar ou a
   publicação falhar por qualquer motivo, o erro é logado e a API ainda retorna 201
   normalmente: o evento é tratado como um efeito colateral não crítico (notificação),
   não como parte da regra de negócio "registrar usuário".
3. O consumer processa o evento de forma assíncrona, fora do ciclo da requisição HTTP
   que fez o registro — o cliente nunca espera o consumer para receber o 201.
4. Nenhuma lógica de negócio nova é acionada pelo consumer nesta v1: ele só loga. Não
   há envio real de e-mail, nem outro sistema reagindo ao evento.

## Contrato do evento

`SecureGate.Application/Events/UserRegisteredEvent.cs` — compartilhado entre publisher
(`RegisterUserService`) e consumer (`UserRegisteredConsumer`), já que ambos vivem no
mesmo processo/solution nesta v1:

```csharp
public sealed record UserRegisteredEvent(Guid UserId, string Name, string Email, DateTime RegisteredAt);
```

## Publicação (Application)

- `IEventPublisher` (`Application/Abstractions`): `Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class`.
  Abstrai o MassTransit para a Application não depender diretamente da lib de
  mensageria — mesmo padrão já usado para `IUserRepository`/`IPasswordHasher`/`ITokenGenerator`.
- `RegisterUserService.RegisterAsync` publica o `UserRegisteredEvent` logo após
  `IUserRepository.AddAsync` ter sucesso. A chamada é envolvida em `try/catch`: exceção
  na publicação é logada (via `ILogger`) e engolida — não propaga para o controller
  (regra de negócio 2).

## Consumo (Infrastructure)

- `MassTransitEventPublisher` (`Api/Infrastructure/Messaging`) implementa
  `IEventPublisher` usando `IPublishEndpoint` do MassTransit.
- `UserRegisteredConsumer` (`Api/Infrastructure/Messaging`) implementa
  `IConsumer<UserRegisteredEvent>`; no `Consume`, loga
  `"E-mail de boas-vindas enviado para {Email}"` em nível `Information`.
- `Program.cs` configura `AddMassTransit(x => { x.AddConsumer<UserRegisteredConsumer>(); x.UsingRabbitMq(...); })`,
  registrando o consumer e apontando para o broker via configuração (`RabbitMq:Host`,
  `RabbitMq:Username`, `RabbitMq:Password`), no mesmo padrão de
  `ConnectionStrings:Default` e `Jwt:Secret` já usado (valor de dev direto no
  `appsettings.Development.json`/`docker-compose.yml`, nunca segredo real comitado).

## Infraestrutura (`docker-compose.yml`)

- Novo serviço `rabbitmq` (imagem `rabbitmq:3-management-alpine`), expondo `5672`
  (AMQP, usado pela API) e `15672` (painel de management, só para inspeção manual
  durante o desenvolvimento).
- `api` ganha `depends_on: rabbitmq` (com healthcheck) e as variáveis
  `RabbitMq__Host`, `RabbitMq__Username`, `RabbitMq__Password`.

## Testes

- Testes de `RegisterUserService` (já existentes) ganham um `FakeEventPublisher` para
  verificar que `PublishAsync` é chamado com o `UserRegisteredEvent` correto após um
  registro bem-sucedido, e que uma falha simulada em `PublishAsync` não impede o
  retorno do `RegisterUserResult` (regra de negócio 2).
- `UserRegisteredConsumer` é testado com o **test harness em memória do próprio
  MassTransit** (`AddMassTransitTestHarness`), publicando o evento e verificando que o
  consumer foi chamado e logou a mensagem esperada — não precisa de um RabbitMQ real
  para esse teste (mais rápido, sem depender do Docker, é o padrão recomendado pela
  própria lib).
- Validação end-to-end (RabbitMQ real) fica para o smoke test manual via
  `docker compose up` + `curl` no `POST /api/auth/register`, conferindo nos logs do
  container da API que a mensagem "e-mail de boas-vindas enviado para {email}"
  apareceu — mesmo processo que já usamos nas etapas 1 e 2.

## Fora de escopo desta etapa

- **Outbox Pattern**: sem garantia transacional entre salvar o usuário e publicar o
  evento (ver regra de negócio 2). Documentado como trade-off consciente, candidato a
  v2 caso o projeto evolua para múltiplos serviços de verdade.
- **Saga / orquestração** entre múltiplos serviços — não existe outro serviço
  consumindo o evento além do consumer local.
- **Retry/dead-letter queue customizados** — usa o comportamento padrão do MassTransit
  (retry básico de transporte); sem política de retry de aplicação nem DLQ dedicada.
- **Consumer em processo/worker separado** — roda no mesmo processo da API, consistente
  com a decisão em [00-architecture-overview.md](00-architecture-overview.md).

## Critérios de aceite

- [x] `POST /api/auth/register` com dados válidos, além de persistir o usuário e
      retornar 201, publica um `UserRegisteredEvent` no RabbitMQ.
- [x] O consumer processa o evento publicado e loga
      "e-mail de boas-vindas enviado para {email}" com o e-mail correto.
- [x] Uma falha simulada na publicação do evento não impede o `RegisterUserService` de
      retornar o resultado do registro (o erro é logado, não propagado).
- [x] Teste automatizado (test harness do MassTransit) cobrindo: evento publicado é
      consumido e o consumer loga a mensagem esperada.
- [x] Teste automatizado cobrindo: `RegisterUserService` publica o evento correto após
      um registro bem-sucedido.
- [x] Smoke test manual via `docker compose up`: registrar um usuário e confirmar nos
      logs do container da API a mensagem de log do consumer.
