# 01 — User Registration

Ver decisões gerais em [00-architecture-overview.md](00-architecture-overview.md).

## Objetivo

Permitir que um novo usuário se registre na plataforma com nome, e-mail e senha,
persistindo os dados com a senha protegida por hash. Nesta etapa **sem mensageria**
— o evento `UserRegistered` entra em [03-messaging.md](03-messaging.md).

## Entidade `User` (Domain)

| Campo | Tipo | Regra |
|---|---|---|
| `Id` | `Guid` | Gerado no construtor |
| `Name` | `string` | Obrigatório, 2–100 caracteres |
| `Email` | `string` | Obrigatório, formato de e-mail válido, único no sistema |
| `PasswordHash` | `string` | Nunca armazenar senha em texto puro |
| `CreatedAt` | `DateTime` (UTC) | Gerado no construtor |

A entidade não expõe setters públicos para os campos acima (imutável após criação
nesta v1 — não há endpoint de edição de perfil).

## Regras de negócio

1. E-mail deve ser único — tentativa de registro com e-mail já existente é rejeitada.
2. Senha mínima de 8 caracteres (validação simples, sem regras de complexidade
   adicionais nesta v1).
3. Senha é transformada em hash via BCrypt antes de persistir; o hash (nem a senha
   original) nunca é retornado nas respostas da API.
4. Nome e e-mail têm espaços em branco removidos (trim) antes de validar/persistir.

## Endpoint

### `POST /api/auth/register`

**Request body:**

```json
{
  "name": "Ana Souza",
  "email": "ana@example.com",
  "password": "senha1234"
}
```

**Response — 201 Created:**

```json
{
  "id": "e3f8c1a0-...-...",
  "name": "Ana Souza",
  "email": "ana@example.com",
  "createdAt": "2026-07-15T12:00:00Z"
}
```

**Response — 400 Bad Request** (validação falhou, ex.: senha curta, e-mail
inválido, campos vazios):

```json
{
  "errors": [
    "A senha deve ter no mínimo 8 caracteres."
  ]
}
```

**Response — 409 Conflict** (e-mail já cadastrado):

```json
{
  "error": "Já existe um usuário cadastrado com este e-mail."
}
```

## Persistência (EF Core + PostgreSQL)

- `AppDbContext` com `DbSet<User> Users`.
- Migration inicial criando a tabela `Users` com índice único em `Email`.
- `UserRepository` implementando `IUserRepository` (definida em `Application/Abstractions`)
  com, no mínimo: `AddAsync(User)`, `GetByEmailAsync(string)`.

## Critérios de aceite

- [ ] `POST /api/auth/register` com dados válidos persiste o usuário no PostgreSQL e
      retorna 201 com os dados do usuário (sem senha/hash).
- [ ] Registrar com e-mail já existente retorna 409 e não duplica o registro.
- [ ] Registrar com senha menor que 8 caracteres retorna 400.
- [ ] Registrar com e-mail em formato inválido retorna 400.
- [ ] A senha nunca aparece em texto puro na resposta, no banco ou em logs.
- [ ] Teste automatizado cobrindo: registro com sucesso cria o usuário no banco.
