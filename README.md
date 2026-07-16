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
- smtp4dev (emails capturados): `http://localhost:5000`

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

## 3. Testando e verificando que está tudo funcionando

Com backend (Opção A) e frontend rodando, nessa ordem:

### 3.1 Health check rápido dos containers

```bash
docker compose ps
```

Espera-se `postgres`, `rabbitmq`, `smtp4dev` e `api` todos com status `Up` (os dois
primeiros também `healthy`).

### 3.2 Registro pela UI

1. Abra `http://localhost:4200/register`
2. Preencha nome, e-mail e senha (mínimo 8 caracteres) e envie
3. Deve redirecionar para `/login` com a mensagem "Cadastro realizado!"
4. Faça login com as mesmas credenciais — deve cair em `/home`

### 3.3 Registro via linha de comando (sem UI)

```bash
curl -X POST http://localhost:8080/api/auth/register \
  -H "Content-Type: application/json" \
  -d "{\"name\":\"Teste\",\"email\":\"teste@example.com\",\"password\":\"senha1234\"}"
```

Espera-se `201 Created` com os dados do usuário (sem senha/hash).

### 3.4 Conferir o e-mail de boas-vindas (smtp4dev)

Abra `http://localhost:5000` — o e-mail "Bem-vindo(a) ao SecureGate" enviado para o
endereço registrado deve aparecer na lista, capturado via SMTP real (nenhum e-mail sai
para a internet, tudo fica local).

**Quer receber de verdade no seu e-mail (ex.: Gmail) em vez do smtp4dev?** Copie
`docker-compose.override.yml.example` para `docker-compose.override.yml` (já está no
`.gitignore` — nunca comite esse arquivo) e preencha com uma
[senha de app do Gmail](https://myaccount.google.com/apppasswords) (exige verificação
em duas etapas ativada). Depois rode `docker compose up -d --build` normalmente; o
Compose mescla o override automaticamente.

### 3.5 Conferir o evento no RabbitMQ

Abra `http://localhost:15672` (usuário/senha `guest`/`guest`) → aba **Queues** → fila
`UserRegistered` — o gráfico de mensagens deve mostrar a publicação/consumo do evento
a cada registro. RabbitMQ é só o broker: não tem UI de cadastro, isso fica no frontend
Angular (`localhost:4200`).

### 3.6 Ver os dados no banco (Postgres)

```bash
docker exec -it securegate-postgres-1 psql -U postgres -d securegate -c "SELECT \"Id\", \"Email\", \"Name\", \"CreatedAt\" FROM \"Users\" ORDER BY \"CreatedAt\" DESC LIMIT 10;"
```

Ou conecte com um cliente GUI (DBeaver, TablePlus, pgAdmin) em `localhost:5432`,
banco `securegate`, usuário/senha `postgres`/`postgres`.

### 3.7 Logs da API em tempo real

```bash
docker compose logs -f api
```

Ao registrar um usuário, deve aparecer a linha
`E-mail de boas-vindas enviado para {email}` (sem `Falha ao enviar`).

### 3.8 Suíte de testes automatizados

```bash
dotnet test        # backend — 58 testes
cd web && npm test # frontend
```

## 4. Rodando em produção

**Status atual: não existe.** Tudo neste repositório (`docker-compose.yml`, secrets,
build do Angular) é para desenvolvimento local. Não há pipeline de CI/CD, servidor de
produção do frontend, nem gerenciamento de segredos — não é "rodar um comando e está
no ar". Esta seção documenta o que precisaria mudar antes de um deploy real, não um
passo a passo pronto.

### 4.1 Segredos

`docker-compose.yml` hoje tem `Jwt__Secret`, senha do Postgres (`postgres`/`postgres`)
e credenciais do RabbitMQ (`guest`/`guest`) direto no arquivo — aceitável só porque são
valores de dev, nunca usados fora da máquina local. Em produção nenhum desses pode
ficar hardcoded nem versionado: usar variáveis de ambiente injetadas por um gerenciador
de segredos (Azure Key Vault, AWS Secrets Manager, Docker/Kubernetes secrets), com um
`Jwt__Secret` novo e forte (o atual está literalmente nomeado "não use em produção").

### 4.2 HTTPS

`Program.cs` chama `app.UseHttpsRedirection()`, mas nada termina TLS — o
`Dockerfile` só expõe `http://+:8080` (dá pra confirmar pelo aviso nos logs: "Failed to
determine the https port for redirect"). Precisa de um reverse proxy (nginx, Caddy,
Traefik) ou um load balancer de nuvem fazendo terminação TLS na frente da API.

### 4.3 Banco de dados e RabbitMQ

Os containers `postgres` e `rabbitmq` do `docker-compose.yml` são para dev — sem
backup, sem réplica, credenciais fracas. Em produção, usar um banco gerenciado (Azure
Database for PostgreSQL, Amazon RDS) e um broker gerenciado (CloudAMQP, Amazon MQ) ou
um cluster RabbitMQ operado com backup e monitoramento de verdade.

### 4.4 Migrations

`Program.cs` roda `dbContext.Database.Migrate()` automaticamente toda vez que a API
sobe. Funciona para este projeto, mas com múltiplas réplicas rodando ao mesmo tempo
isso pode gerar corrida entre instâncias tentando migrar simultaneamente. Em produção,
o comum é rodar a migration como um passo de deploy separado (antes de subir as
réplicas da API), não dentro do próprio processo da aplicação.

### 4.5 Frontend

Não existe hoje um jeito de servir o Angular em produção — só `ng serve` (dev). Seria
necessário: `ng build` (gera `web/dist/`), e um servidor estático (nginx, Caddy, ou um
host como Netlify/Vercel/S3+CloudFront) configurado para servir esses arquivos e fazer
proxy de `/api/**` para a API — o mesmo papel que `proxy.conf.json` cumpre em dev.

### 4.6 CORS / Hosts permitidos

`"AllowedHosts": "*"` em `appsettings.json` só é seguro hoje porque frontend e API
sempre conversam via proxy same-origin (dev) ou mesmo domínio. Se em produção o
frontend for servido de um domínio diferente da API, é preciso restringir
`AllowedHosts` e configurar uma política de CORS explícita.

### 4.7 E-mail

`Smtp:*` aponta pro `smtp4dev` local ([seção 3.4](#34-conferir-o-e-mail-de-boas-vindas-smtp4dev))
— captura tudo localmente, não envia nada de verdade. Em produção, apontar para um
provedor transacional de verdade (SendGrid, Amazon SES, Resend), com credenciais via
gerenciador de segredos (mesmo ponto da seção 4.1) — os campos `Smtp__Username`,
`Smtp__Password` e `Smtp__EnableStartTls` já existem pra isso, ver
`docker-compose.override.yml.example`.

## Referência rápida

| Comando | O que faz |
|---|---|
| `docker compose up -d --build` | Sobe Postgres + RabbitMQ + API (tudo em Docker) |
| `docker compose up -d postgres rabbitmq` | Sobe só a infraestrutura, pra rodar a API local |
| `dotnet run --project src/SecureGate.Api` | Roda a API local com Swagger |
| `dotnet test` | Roda os testes do backend |
| `docker compose down` | Para os containers |
| `docker compose logs -f smtp4dev` | Ver logs do smtp4dev (captura de e-mail) |
| `cd web && npm start` | Roda o frontend Angular em `localhost:4200` |
| `cd web && npm test` | Roda os testes do frontend |
