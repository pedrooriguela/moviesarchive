BEGIN;

-- ============================================================
-- 1. PLANOS
-- ============================================================
-- A conta gratuita não é um plano: é a conta sem assinatura
CREATE TABLE "Plans" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "Name" text NOT NULL,
    "Price" numeric(10,2) NOT NULL DEFAULT 0,
    "Description" text,
    "Type" text NOT NULL,
    "MaxMembers" integer NOT NULL,
    "IsActive" boolean NOT NULL DEFAULT true,
    CONSTRAINT "PK_Plans" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_Plans_Price" CHECK ("Price" >= 0),
    CONSTRAINT "CK_Plans_Type" CHECK ("Type" IN ('Individual', 'Family')),
    -- Individual: 1 pessoa. Familiar: até 4, titular incluído.
    CONSTRAINT "CK_Plans_MaxMembers" CHECK (
        ("Type" = 'Individual' AND "MaxMembers" = 1) OR
        ("Type" = 'Family' AND "MaxMembers" BETWEEN 2 AND 4))
);

CREATE UNIQUE INDEX "IX_Plans_Name" ON "Plans" ("Name");

-- Planos à venda (preços de exemplo)
INSERT INTO "Plans" ("Name", "Description", "Type", "Price", "MaxMembers") VALUES
    ('Individual', 'Assista e baixe os filmes do acervo. Para uma pessoa.', 'Individual', 19.90, 1),
    ('Familiar', 'Tudo do Individual para até 4 pessoas, cada uma com a própria conta.', 'Family', 34.90, 4);

-- ============================================================
-- 2. USUÁRIOS
-- ============================================================
CREATE TABLE "Users" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "Name" text NOT NULL,
    "Email" text NOT NULL,
    "Password" text NOT NULL,
    "PhotoData" bytea,
    "PhotoContentType" text,
    "PasswordResetToken" text,
    "PasswordResetExpiresAt" timestamp with time zone,
    -- Opcional porque contas antigas não têm; a API exige no cadastro novo
    "BirthDate" date,
    -- Assinatura ATIVA de que a pessoa faz parte (titular ou convidada); nula = gratuita.
    -- A chave estrangeira é criada depois de "Subscriptions" (seção 12).
    "SubscriptionId" uuid,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_Users_BirthDate" CHECK ("BirthDate" IS NULL OR "BirthDate" >= DATE '1900-01-01')
);

-- Unicidade de email sem diferenciar maiúsculas/minúsculas
CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" (lower("Email"));
CREATE INDEX "IX_Users_SubscriptionId" ON "Users" ("SubscriptionId");

-- ============================================================
-- 3. SEGUIDORES (Relacionamento N:N Usuário -> Usuário)
-- ============================================================
CREATE TABLE "UserFollowers" (
    "FollowerId" uuid NOT NULL,
    "FollowingId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_UserFollowers" PRIMARY KEY ("FollowerId", "FollowingId"),
    CONSTRAINT "FK_UserFollowers_Users_FollowerId"
        FOREIGN KEY ("FollowerId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserFollowers_Users_FollowingId"
        FOREIGN KEY ("FollowingId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_UserFollowers_NoSelfFollow"
        CHECK ("FollowerId" <> "FollowingId")
);

CREATE INDEX "IX_UserFollowers_FollowingId" ON "UserFollowers" ("FollowingId");

-- ============================================================
-- 4. FILMES
-- ============================================================
CREATE TABLE "Movies" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "Title" text NOT NULL,
    "Description" text,
    "ReleaseDate" date,
    "Language" text,
    "Director" text,
    "PosterUrl" text,
    "Genres" text[] NOT NULL DEFAULT '{}',
    -- Classificação indicativa: idade mínima (0 = livre). Sem classificação conhecida, 18.
    "AgeRating" integer NOT NULL DEFAULT 18,
    "RuntimeMinutes" integer,
    CONSTRAINT "PK_Movies" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_Movies_AgeRating" CHECK ("AgeRating" IN (0, 10, 12, 14, 16, 18)),
    CONSTRAINT "CK_Movies_RuntimeMinutes" CHECK ("RuntimeMinutes" IS NULL OR "RuntimeMinutes" > 0),
    CONSTRAINT "CK_Movies_Genres" CHECK ("Genres" <@ ARRAY['Acao','Comedia','Crime','Drama','Fantasia','FiccaoCientifica','Romance','Suspense','Terror']::text[])
);

-- ============================================================
-- 5. FILMES FAVORITOS (Relacionamento N:N Usuário -> Filme)
-- ============================================================
CREATE TABLE "UserFavorites" (
    "UserId" uuid NOT NULL,
    "MovieId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_UserFavorites" PRIMARY KEY ("UserId", "MovieId"),
    CONSTRAINT "FK_UserFavorites_Users_UserId"
        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserFavorites_Movies_MovieId"
        FOREIGN KEY ("MovieId") REFERENCES "Movies" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_UserFavorites_MovieId" ON "UserFavorites" ("MovieId");

-- ============================================================
-- 6. LISTAS DE FILMES
-- ============================================================
CREATE TABLE "Lists" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "UserId" uuid NOT NULL,
    "Title" text NOT NULL,
    "Description" text,
    "IsPublic" boolean NOT NULL DEFAULT false,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_Lists" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Lists_Users_UserId"
        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

-- Também cobre consultas por UserId (prefixo do índice)
CREATE UNIQUE INDEX "IX_Lists_UserId_Title" ON "Lists" ("UserId", "Title");

-- ============================================================
-- 7. ITENS DA LISTA (Relacionamento N:N Lista -> Filme)
-- ============================================================
CREATE TABLE "ListMovies" (
    "ListId" uuid NOT NULL,
    "MovieId" uuid NOT NULL,
    "Position" integer NOT NULL DEFAULT 0,
    "AddedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_ListMovies" PRIMARY KEY ("ListId", "MovieId"),
    CONSTRAINT "FK_ListMovies_Lists_ListId"
        FOREIGN KEY ("ListId") REFERENCES "Lists" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ListMovies_Movies_MovieId"
        FOREIGN KEY ("MovieId") REFERENCES "Movies" ("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_ListMovies_Position" CHECK ("Position" >= 0)
);

CREATE INDEX "IX_ListMovies_MovieId" ON "ListMovies" ("MovieId");

-- ============================================================
-- 8. AVALIAÇÕES (REVIEWS)
-- ============================================================
CREATE TABLE "Reviews" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "UserId" uuid NOT NULL,
    "MovieId" uuid NOT NULL,
    "Rating" numeric(2,1) NOT NULL,
    "Comment" text,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_Reviews" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Reviews_Movies_MovieId"
        FOREIGN KEY ("MovieId") REFERENCES "Movies" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Reviews_Users_UserId"
        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    -- De 0,5 a 5 estrelas, em passos de meia estrela
    CONSTRAINT "CK_Reviews_Rating" CHECK ("Rating" BETWEEN 0.5 AND 5 AND mod("Rating" * 2, 1) = 0)
);

CREATE INDEX "IX_Reviews_MovieId" ON "Reviews" ("MovieId");
CREATE UNIQUE INDEX "IX_Reviews_UserId_MovieId" ON "Reviews" ("UserId", "MovieId");

-- ============================================================
-- 9. RESPOSTAS ÀS AVALIAÇÕES
-- ============================================================
CREATE TABLE "Responses" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "ReviewId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Comment" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_Responses" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Responses_Reviews_ReviewId"
        FOREIGN KEY ("ReviewId") REFERENCES "Reviews" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Responses_Users_UserId"
        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Responses_ReviewId" ON "Responses" ("ReviewId");
CREATE INDEX "IX_Responses_UserId" ON "Responses" ("UserId");

-- ============================================================
-- 10. WATCHLIST (Relacionamento N:N Usuário -> Filme)
-- ============================================================
CREATE TABLE "Watchlist" (
    "UserId" uuid NOT NULL,
    "MovieId" uuid NOT NULL,
    "AddedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_Watchlist" PRIMARY KEY ("UserId", "MovieId"),
    CONSTRAINT "FK_Watchlist_Users_UserId"
        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Watchlist_Movies_MovieId"
        FOREIGN KEY ("MovieId") REFERENCES "Movies" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_Watchlist_MovieId" ON "Watchlist" ("MovieId");

-- ============================================================
-- 11. HISTÓRICO DE VISUALIZAÇÕES
-- ============================================================
-- Uma linha por clique no play: o mesmo filme pode aparecer várias vezes
CREATE TABLE "WatchHistory" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "UserId" uuid NOT NULL,
    "MovieId" uuid NOT NULL,
    "WatchedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_WatchHistory" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WatchHistory_Users_UserId"
        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_WatchHistory_Movies_MovieId"
        FOREIGN KEY ("MovieId") REFERENCES "Movies" ("Id") ON DELETE CASCADE
);

-- A linha do tempo busca por usuário, do mais recente para o mais antigo
CREATE INDEX "IX_WatchHistory_UserId_WatchedAt" ON "WatchHistory" ("UserId", "WatchedAt" DESC);
CREATE INDEX "IX_WatchHistory_MovieId" ON "WatchHistory" ("MovieId");

-- ============================================================
-- 12. ASSINATURAS
-- ============================================================
-- EndedAt nulo = ativa. Trocar de plano encerra a linha (PlanChanged) e abre outra.
CREATE TABLE "Subscriptions" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "OwnerId" uuid NOT NULL,
    "PlanId" uuid NOT NULL,
    "StartedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "EndedAt" timestamp with time zone,
    "EndReason" text,
    CONSTRAINT "PK_Subscriptions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Subscriptions_Users_OwnerId"
        FOREIGN KEY ("OwnerId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    -- Plano com assinaturas não pode ser apagado: o admin o tira de venda (IsActive)
    CONSTRAINT "FK_Subscriptions_Plans_PlanId"
        FOREIGN KEY ("PlanId") REFERENCES "Plans" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "CK_Subscriptions_EndReason"
        CHECK ("EndReason" IS NULL OR "EndReason" IN ('Canceled', 'PlanChanged', 'AccountDeleted')),
    CONSTRAINT "CK_Subscriptions_Ended"
        CHECK (("EndedAt" IS NULL) = ("EndReason" IS NULL)),
    CONSTRAINT "CK_Subscriptions_Dates"
        CHECK ("EndedAt" IS NULL OR "EndedAt" >= "StartedAt")
);

-- Um titular tem no máximo uma assinatura ativa
CREATE UNIQUE INDEX "IX_Subscriptions_OwnerId_Active"
    ON "Subscriptions" ("OwnerId") WHERE "EndedAt" IS NULL;
CREATE INDEX "IX_Subscriptions_PlanId" ON "Subscriptions" ("PlanId");

-- Integrantes: Users.SubscriptionId aponta para a assinatura ativa (criada aqui por causa do ciclo)
ALTER TABLE "Users" ADD CONSTRAINT "FK_Users_Subscriptions_SubscriptionId"
    FOREIGN KEY ("SubscriptionId") REFERENCES "Subscriptions" ("Id") ON DELETE SET NULL;

-- ============================================================
-- 13. CONVITES DO PLANO FAMILIAR
-- ============================================================
CREATE TABLE "FamilyInvites" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "SubscriptionId" uuid NOT NULL,
    "InvitedUserId" uuid NOT NULL,
    "Status" text NOT NULL DEFAULT 'Pending',
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "RespondedAt" timestamp with time zone,
    CONSTRAINT "PK_FamilyInvites" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_FamilyInvites_Subscriptions_SubscriptionId"
        FOREIGN KEY ("SubscriptionId") REFERENCES "Subscriptions" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_FamilyInvites_Users_InvitedUserId"
        FOREIGN KEY ("InvitedUserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_FamilyInvites_Status"
        CHECK ("Status" IN ('Pending', 'Accepted', 'Declined', 'Canceled')),
    CONSTRAINT "CK_FamilyInvites_Responded"
        CHECK (("Status" = 'Pending') = ("RespondedAt" IS NULL)),
    CONSTRAINT "CK_FamilyInvites_Expires"
        CHECK ("ExpiresAt" > "CreatedAt")
);

-- No máximo um convite pendente por pessoa na mesma assinatura
CREATE UNIQUE INDEX "IX_FamilyInvites_Pending"
    ON "FamilyInvites" ("SubscriptionId", "InvitedUserId") WHERE "Status" = 'Pending';
CREATE INDEX "IX_FamilyInvites_InvitedUserId" ON "FamilyInvites" ("InvitedUserId");

COMMIT;