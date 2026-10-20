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

A partir da raiz do repositório, entre na pasta da API:

```bash
cd MoviesArchive.Api
```

**Banco de dados (PostgreSQL):**

```bash
dotnet user-secrets set "PostgresSettings:ConnectionString" "Host=143.107.102.29;Port=65432;Database=t3_grupo_6;Username=admin_t3_g6;Password=3PhT9zQvER"
```

**JWT:**

```bash
dotnet user-secrets set "JwtSettings:Key" "K7x9mP2vL8qR4wT6yU1zA3bC5dE8fG0hJ2kM4nO6pQ8sV1xW3zY5cB7dF9gH1jL="
dotnet user-secrets set "JwtSettings:Issuer" "MoviesArchive"
dotnet user-secrets set "JwtSettings:Audience" "MoviesArchive"
```

`Issuer` e `Audience` devem corresponder aos valores usados na emissão dos tokens. Com a mesma `Key` em todas as máquinas, um token gerado por uma instância local também é válido nas outras.

**E-mail (SMTP do Gmail, usado no envio do link de redefinição de senha):**

```bash
dotnet user-secrets set "SmtpSettings:Host" "smtp.gmail.com"
dotnet user-secrets set "SmtpSettings:Port" "587"
dotnet user-secrets set "SmtpSettings:Username" "noreply.booksarchive@gmail.com"
dotnet user-secrets set "SmtpSettings:Password" "kznw glgl uyjc ifnu"
dotnet user-secrets set "SmtpSettings:FromName" "MoviesArchive"
dotnet user-secrets set "SmtpSettings:FromEmail" "noreply.booksarchive@gmail.com"
```

> A senha SMTP é uma **senha de app** do Gmail (16 caracteres) e precisa ficar entre aspas por causa dos espaços.

Confira se todos os valores foram gravados:

```bash
dotnet user-secrets list
```

A saída deve listar as 10 chaves acima (`PostgresSettings`, `JwtSettings` e `SmtpSettings`).

### 2.2. Iniciar a API

Ainda na pasta `MoviesArchive.Api`, inicie a aplicação:

```bash
dotnet run
```

No perfil de desenvolvimento, a API fica disponível em `http://localhost:5197`, com a documentação em `http://localhost:5197/swagger`.

O banco `t3_grupo_6` e suas tabelas já existem. **Não execute `dotnet ef database update` nem rode `schema.sql` nesse banco**: as migrations atuais não representam todo o schema e podem tentar criar tabelas existentes.

## 3. Configurar o front-end

### 3.1. Instalar o Node.js

O front-end usa Vite 8, que exige **Node.js 20.19+ ou 22.12+**. Recomendamos instalar a versão LTS pelo nvm.

**Linux / macOS (nvm):**

```bash
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.40.3/install.sh | bash
```

Feche e abra o terminal (ou rode `source ~/.bashrc` / `source ~/.zshrc`) e instale o Node:

```bash
nvm install --lts
nvm use --lts
```

**Windows (nvm-windows):**

Baixe e execute o instalador `nvm-setup.exe` da página de [releases do nvm-windows](https://github.com/coreybutler/nvm-windows/releases). Depois, em um terminal aberto como administrador:

```bash
nvm install lts
nvm use lts
```

Confira se a instalação funcionou:

```bash
node -v
npm -v
```

> Evite instalar o Node pelo `apt` (Debian/Ubuntu): a versão dos repositórios costuma ser antiga demais para o Vite 8.

### 3.2. Instalar as dependências

A partir da raiz do repositório:

```bash
cd MoviesArchive.Client
npm install
```

Isso cria a pasta `node_modules` com todas as dependências (React, Vite etc.). Só é necessário repetir quando o `package.json` mudar.

### 3.3. Configurar a URL da API (opcional)

Por padrão o front-end acessa a API em `http://localhost:5197`. Se a sua API rodar em outro endereço, crie um `.env` a partir do exemplo e ajuste o valor:

```bash
cp .env.example .env
```

```env
VITE_API_URL=http://localhost:5197
```

Reinicie o `npm run dev` sempre que alterar o `.env`.

### 3.4. Iniciar o front-end

Com a API já rodando (passo 2.2), em **outro terminal**:

```bash
cd MoviesArchive.Client
npm run dev
```

Acesse `http://localhost:5173` no navegador.

> Use a porta `5173`: a política de CORS da API só aceita requisições dessa origem. Se o Vite subir em outra porta (por exemplo `5174`, porque a 5173 já estava ocupada), encerre o outro processo e rode novamente.

### Problemas comuns

| Erro | Causa | Solução |
|---|---|---|
| `npm: command not found` | Node.js não instalado ou nvm não carregado no terminal | Refaça o passo 3.1 e abra um novo terminal |
| `vite: not found` | Dependências não instaladas | Rode `npm install` dentro de `MoviesArchive.Client` |
| Página padrão do Vite em vez da tela de login | Navegador com cache ou outro servidor na porta 5173 | Recarregue com `Ctrl + Shift + R` e confira se só há um `npm run dev` rodando |
| "Não foi possível conectar à API" | API parada ou em outra porta | Inicie a API (passo 2.2) ou ajuste `VITE_API_URL` |
| Erro de CORS no console | Front-end rodando fora da porta 5173 | Rode o front-end em `http://localhost:5173` |
