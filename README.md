# SecureGate

API de autenticação em .NET 8 com registro, login (JWT) e publicação de evento
`UserRegistered` via RabbitMQ/MassTransit, mais um frontend Angular básico.
Contexto completo do projeto em [specs/00-architecture-overview.md](specs/00-architecture-overview.md).

## Pré-requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Postgres, RabbitMQ e, opcionalmente, a API)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) — só necessário se for rodar a API fora do Docker (`global.json` fixa a versão `8.0.416`)
- [Node.js](https://nodejs.org/) + npm — para o frontend Angular

## 1. Backend

Existem duas formas de subir o backend. Use a **Opção A** se só quer o sistema
rodando (ex.: para testar o frontend). Use a **Opção B** se estiver
desenvolvendo a API e quiser hot reload + Swagger.

### Opção A — tudo via Docker Compose (mais simples)

Sobe Postgres, RabbitMQ e a API, todos containerizados:

```bash
docker compose up -d --build
```

- API: `http://localhost:8080`
- RabbitMQ management UI: `http://localhost:15672` (usuário/senha: `guest`/`guest`)
- Postgres: `localhost:5432` (usuário/senha: `postgres`/`postgres`)

> A API sobe em modo `Production` nesse caminho, então o Swagger **não** fica
> disponível — use a Opção B para isso.

Ver logs / parar:

```bash
docker compose logs -f api      # acompanhar logs da API
docker compose down             # parar tudo (mantém os dados dos volumes)
docker compose down -v          # parar e apagar os dados do Postgres também
```

### Opção B — API local + infraestrutura via Docker (dev loop)

Sobe só Postgres e RabbitMQ em containers, e roda a API direto na máquina com
`dotnet run` (hot reload, Swagger habilitado):

```bash
docker compose up -d postgres rabbitmq
dotnet run --project src/SecureGate.Api
```

- API: `http://localhost:5012` (perfil `http` do `launchSettings.json`)
- Swagger: `http://localhost:5012/swagger` (abre automaticamente)

> Repare que essa porta (`5012`) é diferente da porta usada pelo proxy do
> frontend (`8080`, ver seção 2). Se quiser usar o frontend junto com essa
> opção, rode a API com `--urls http://localhost:8080` ou ajuste
> `web/proxy.conf.json`.

### Testes do backend

```bash
dotnet test
```

## 2. Frontend

O frontend espera a API respondendo em `http://localhost:8080` (configurado em
`web/proxy.conf.json`), então suba o backend pela **Opção A** antes deste
passo — ou aponte a API local para a porta `8080` (ver aviso acima).

```bash
cd web
npm install       # só na primeira vez, ou quando as dependências mudarem
npm start
```

- App: `http://localhost:4200`
- Chamadas para `/api/**` são redirecionadas para `http://localhost:8080` automaticamente (proxy do Angular CLI)

### Testes do frontend

```bash
cd web
npm test
```

## Referência rápida

| Comando | O que faz |
|---|---|
| `docker compose up -d --build` | Sobe Postgres + RabbitMQ + API (tudo em Docker) |
| `docker compose up -d postgres rabbitmq` | Sobe só a infraestrutura, pra rodar a API local |
| `dotnet run --project src/SecureGate.Api` | Roda a API local com Swagger |
| `dotnet test` | Roda os testes do backend |
| `docker compose down` | Para os containers |
| `cd web && npm start` | Roda o frontend Angular em `localhost:4200` |
| `cd web && npm test` | Roda os testes do frontend |
