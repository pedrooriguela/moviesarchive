## Pré-requisitos

Antes de clonar o repositório, instale:

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (recomendado via [nvm-windows](https://github.com/coreybutler/nvm-windows) ou [nvm](https://github.com/nvm-sh/nvm))
- Acesso de rede ao servidor PostgreSQL do projeto

## 1. Clonar o repositório

```bash
git clone <URL_DO_REPOSITORIO>
cd MoviesArchive
```

## 2. Configurar o back-end

### 2.1. Configurar os segredos locais

Cada desenvolvedor configura estes valores na própria máquina. User Secrets não são compartilhados pelo Git. A API já tem um `UserSecretsId`, então não é necessário executar `dotnet user-secrets init`.

```bash
cd MoviesArchive.Api
dotnet user-secrets set "PostgresSettings:ConnectionString" "Host=143.107.102.29;Port=65432;Database=t3_grupo_6;Username=admin_t3_g6;Password=3PhT9zQvER"
```

Gere uma chave JWT forte com OpenSSL:

```bash
openssl rand -base64 48
```

Copie o valor gerado e configure os parâmetros JWT exigidos pela API:

```bash
dotnet user-secrets set "JwtSettings:Key" "COLE_A_CHAVE_GERADA_AQUI"
dotnet user-secrets set "JwtSettings:Issuer" "MoviesArchive"
dotnet user-secrets set "JwtSettings:Audience" "MoviesArchive"
```

`Issuer` e `Audience` devem corresponder aos valores usados na emissão dos tokens. A chave pode ser diferente para cada desenvolvedor; nesse caso, tokens criados por uma instância local não serão válidos em outra.

### 2.2. Iniciar a API

Ainda na pasta `MoviesArchive.Api`, inicie a aplicação:

```bash
dotnet run
```

No perfil de desenvolvimento, a API fica disponível em `http://localhost:5197`, com a documentação em `http://localhost:5197/swagger`.

O banco `t3_grupo_6` e suas tabelas já existem. **Não execute `dotnet ef database update` nem rode `schema.sql` nesse banco**: as migrations atuais não representam todo o schema e podem tentar criar tabelas existentes.
