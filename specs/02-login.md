# 02 — Login

Ver decisões gerais em [00-architecture-overview.md](00-architecture-overview.md).

## Objetivo

Permitir que um usuário já registrado (ver [01-user-registration.md](01-user-registration.md))
se autentique com e-mail e senha, recebendo um JWT para uso em requisições futuras.
Nesta etapa não existe nenhum endpoint protegido por `[Authorize]` — o objetivo é
exclusivamente emitir o token.

## Regras de negócio

1. Login requer e-mail e senha corretos. E-mail inexistente **ou** senha incorreta
   retornam exatamente a mesma resposta de erro (401 genérico), para não revelar se o
   e-mail cadastrado existe (evita enumeração de usuários).
2. A senha informada é comparada com o hash armazenado via BCrypt. `IPasswordHasher`
   (já existente) ganha um método `Verify(string password, string passwordHash)`.
3. O JWT emitido carrega as claims: `sub` (Id do usuário), `email`, `name`. Sem
   `role`/`scope` — não há RBAC nesta v1, todo usuário autenticado tem o mesmo nível
   de acesso (consistente com [00-architecture-overview.md](00-architecture-overview.md)).
4. Expiração do token: configurável (`Jwt:ExpirationMinutes` em `appsettings.json`),
   padrão 60 minutos. Sem refresh token nesta versão — expirado, o usuário faz login
   novamente.
5. E-mail é trimmed antes de comparar, consistente com o registro.

## Endpoint

### `POST /api/auth/login`

**Request body:**

```json
{
  "email": "ana@example.com",
  "password": "senha1234"
}
```

**Response — 200 OK:**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-07-15T13:00:00Z"
}
```

**Response — 400 Bad Request** (e-mail em formato inválido ou campos vazios):

```json
{
  "errors": [
    "O e-mail é obrigatório."
  ]
}
```

**Response — 401 Unauthorized** (e-mail não encontrado OU senha incorreta —
mensagem idêntica nos dois casos):

```json
{
  "error": "E-mail ou senha inválidos."
}
```

## Geração do JWT (Infrastructure)

- `ITokenGenerator` (`Application/Abstractions`): `TokenResult Generate(User user)`,
  onde `TokenResult` é um DTO com `Token` (string) e `ExpiresAt` (`DateTime` UTC).
- `JwtTokenGenerator` (`Api/Infrastructure/Security`) implementando `ITokenGenerator`
  via `System.IdentityModel.Tokens.Jwt`.
- Configuração em `appsettings.json`: `Jwt:Issuer`, `Jwt:Audience`,
  `Jwt:ExpirationMinutes`. `Jwt:Secret` **não** entra no `appsettings.json` comitado —
  fica só em `appsettings.Development.json` (valor de desenvolvimento, documentado
  como tal) e é passado via variável de ambiente `Jwt__Secret` no `docker-compose.yml`,
  no mesmo padrão já usado para `ConnectionStrings__Default`.

## Aplicação (`LoginService`)

- `Application/Auth/LoginService.cs`: recebe `LoginRequest` (email, password), busca o
  usuário via `IUserRepository.GetByEmailAsync` (já existente), verifica a senha via
  `IPasswordHasher.Verify`, e retorna `LoginResult` (token, expiresAt) via
  `ITokenGenerator.Generate`.
- Se o usuário não existir OU a senha não bater, lança `InvalidCredentialsException`
  (nova, mapeada para 401 no controller) — mesma exceção nos dois casos, para garantir
  a resposta idêntica exigida na regra de negócio 1.

## Fora de escopo desta etapa

- Middleware de autenticação (`AddAuthentication`/`[Authorize]`) para proteger outros
  endpoints — não há endpoint protegido nesta v1; entra quando o primeiro endpoint
  autenticado for necessário.
- Refresh token, rotação de token, logout/revogação de token.
- Rate limiting ou bloqueio de conta após tentativas de login falhas.

## Critérios de aceite

- [ ] `POST /api/auth/login` com e-mail e senha corretos retorna 200 com um JWT válido
      (assinatura, issuer, audience e `exp` corretos) e o campo `expiresAt`.
- [ ] Login com e-mail inexistente retorna 401 com a mensagem genérica.
- [ ] Login com senha incorreta retorna 401 com a mesma mensagem genérica do e-mail
      inexistente (resposta idêntica nos dois casos).
- [ ] Login com e-mail em formato inválido ou campos vazios retorna 400.
- [ ] O token gerado contém as claims `sub` (Id do usuário), `email` e `name`.
- [ ] Teste automatizado cobrindo: login com sucesso gera um token decodificável com
      os claims esperados.
- [ ] Teste automatizado cobrindo: login com senha incorreta e login com e-mail
      inexistente retornam a mesma resposta (401 genérico).
