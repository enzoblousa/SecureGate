# 04 — Frontend (Angular)

Ver decisões gerais em [00-architecture-overview.md](00-architecture-overview.md).

## Objetivo

Construir uma interface visual simples em Angular para o SecureGate, cobrindo
exatamente a superfície hoje exposta pela API: registro de usuário, login, e uma tela
pós-login mostrando os dados do usuário autenticado. Sem CRUD de perfil, RBAC ou
qualquer tela além dessas — a superfície do frontend espelha a superfície do backend
([01-user-registration.md](01-user-registration.md), [02-login.md](02-login.md)).

## Escopo (telas)

1. `/register` — formulário de registro (nome, e-mail, senha).
2. `/login` — formulário de login (e-mail, senha).
3. `/home` — protegida por auth guard; mostra nome e e-mail decodificados do JWT
   armazenado; botão "Sair".
4. `/` redireciona para `/login`.

## Decisões técnicas

| Decisão | Escolha | Justificativa |
|---|---|---|
| Estrutura de componentes | Angular standalone (sem NgModules) | Padrão atual da Angular CLI; menos boilerplate. |
| UI | Angular Material | Componentes prontos (inputs, botões, snackbar) sem escrever CSS do zero. |
| Formulários | Reactive Forms | Validação declarativa espelhando as regras do backend (nome 2–100, e-mail válido, senha ≥ 8) — só feedback de UX; o backend continua sendo a fonte de verdade. |
| Integração com a API | Proxy reverso, caminhos relativos (`/api/...`) | Evita CORS — zero mudança no backend .NET. Mesmo comportamento em dev (`ng serve` + `proxy.conf.json`) e produção (nginx `proxy_pass` no container). |
| Autenticação | `AuthService` guarda `{ token, expiresAt }` no `localStorage`; decodifica claims do JWT manualmente (`atob` + `JSON.parse` no payload) | Simplicidade — não precisa de uma lib externa (`jwt-decode`) só pra ler 3 campos. |
| Guarda de rota | `authGuard` funcional em `/home` | Redireciona pra `/login` se não autenticado ou com token expirado. |
| Interceptor HTTP (Bearer token) | Não incluído nesta etapa | Não existe nenhum endpoint protegido além de registro/login pra consumir o token — YAGNI. |
| Testes automatizados | Não incluídos nesta etapa | Validação manual no navegador; pode entrar como evolução futura. |

## Estrutura de pastas

```
web/
├── src/
│   ├── app/
│   │   ├── app.config.ts
│   │   ├── app.routes.ts
│   │   ├── app.component.ts
│   │   ├── auth/
│   │   │   ├── auth.service.ts
│   │   │   ├── auth.guard.ts
│   │   │   ├── models/
│   │   │   │   └── auth.models.ts        # RegisterRequest, LoginRequest, TokenResult, DecodedUser
│   │   │   ├── register/
│   │   │   │   └── register.component.ts
│   │   │   └── login/
│   │   │       └── login.component.ts
│   │   └── home/
│   │       └── home.component.ts
│   ├── index.html
│   ├── main.ts
│   └── styles.scss
├── proxy.conf.json
├── nginx.conf
├── Dockerfile
├── angular.json
├── package.json
└── tsconfig*.json
```

## Fluxos

### Registro

1. Usuário preenche nome/e-mail/senha em `/register`.
2. Validação client-side (Reactive Forms) bloqueia o submit se inválido, mostrando
   erros inline.
3. `AuthService.register()` faz `POST /api/auth/register`.
4. Sucesso (201): navega para `/login` com uma confirmação visual (snackbar).
5. Erro (400/409): mostra a mensagem retornada pela API (snackbar, ou inline no campo
   relevante — ex.: 409 aponta pro campo e-mail).

### Login

1. Usuário preenche e-mail/senha em `/login`.
2. `AuthService.login()` faz `POST /api/auth/login`.
3. Sucesso (200): salva `{ token, expiresAt }` no `localStorage`, navega para `/home`.
4. Erro (400/401): mostra a mensagem retornada pela API (snackbar).

### Home

1. `authGuard` verifica `AuthService.isAuthenticated()` (token presente e `expiresAt`
   no futuro) antes de ativar a rota; senão redireciona pra `/login`.
2. Componente decodifica o JWT armazenado e exibe `name`/`email`.
3. Botão "Sair" chama `AuthService.logout()` (limpa o `localStorage`) e navega pra
   `/login`.

## Infraestrutura

- `web/Dockerfile`: multi-stage — `node:20-alpine` (`npm ci && ng build`) →
  `nginx:alpine` servindo `dist/` com um `nginx.conf` próprio fazendo `proxy_pass` de
  `/api/` para `http://api:8080/api/`.
- `docker-compose.yml` ganha o serviço `web` (porta ex. `4200:80`, `depends_on: api`),
  no mesmo padrão dos serviços já existentes (`postgres`, `rabbitmq`, `api`).
- Dev local sem Docker: `web/proxy.conf.json` + `ng serve --proxy-config
  proxy.conf.json`, apontando `/api` para `http://localhost:8080` (a API já expõe essa
  porta mesmo sem subir o container do `web`).

## Fora de escopo desta etapa

- Testes automatizados (Jasmine/Karma) do frontend.
- Interceptor HTTP para anexar o Bearer token — sem endpoint protegido pra consumir.
- Qualquer tela de perfil/CRUD/RBAC — não existe no backend.
- Refresh token / renovação automática de sessão — consistente com a v1 do backend
  (sem refresh token, ver [02-login.md](02-login.md)).

## Critérios de aceite

- [ ] `/register` envia os dados pro backend; sucesso navega pra `/login` com
      confirmação visual; erros (400/409) aparecem na tela com a mensagem da API.
- [ ] `/login` envia os dados pro backend; sucesso salva o token e navega pra `/home`;
      erros (400/401) aparecem na tela com a mensagem da API.
- [ ] `/home` só é acessível autenticado (token válido e não expirado); mostra nome e
      e-mail do usuário logado.
- [ ] Acessar `/home` sem token (ou com token expirado) redireciona pra `/login`.
- [ ] Botão "Sair" limpa a sessão e volta pra `/login`.
- [ ] `docker compose up` sobe o frontend junto com API/Postgres/RabbitMQ, acessível
      numa porta fixa, funcionando ponta a ponta (registro → login → home) sem erro de
      CORS.
- [ ] `ng serve` local (fora do Docker) também funciona ponta a ponta contra a API
      rodando via `docker compose up` (usando o proxy de dev).
