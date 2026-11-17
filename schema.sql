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

-- Sessões: refresh tokens (só o hash SHA-256; o valor fica no cookie httpOnly do navegador)
CREATE TABLE "RefreshTokens" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "UserId" uuid NOT NULL,
    "TokenHash" text NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "RevokedAt" timestamp with time zone,
    "ReplacedByTokenHash" text,
    CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RefreshTokens_Users_UserId"
        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_RefreshTokens_TokenHash" ON "RefreshTokens" ("TokenHash");
CREATE INDEX "IX_RefreshTokens_UserId" ON "RefreshTokens" ("UserId");

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
    -- Endereço do vídeo (MP4/WebM) tocado pelo player; sem ele, o filme não tem play
    "VideoUrl" text,
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
    -- Resposta a outra resposta da mesma avaliação (nulo = resposta direta à avaliação)
    "ParentResponseId" uuid,
    -- Excluída, mas mantida (sem texto) porque outras respostas dependem dela
    "DeletedAt" timestamp with time zone,
    CONSTRAINT "PK_Responses" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Responses_Reviews_ReviewId"
        FOREIGN KEY ("ReviewId") REFERENCES "Reviews" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Responses_Users_UserId"
        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Responses_Responses_ParentResponseId"
        FOREIGN KEY ("ParentResponseId") REFERENCES "Responses" ("Id") ON DELETE SET NULL
);

CREATE INDEX "IX_Responses_ReviewId" ON "Responses" ("ReviewId");
CREATE INDEX "IX_Responses_UserId" ON "Responses" ("UserId");
CREATE INDEX "IX_Responses_ParentResponseId" ON "Responses" ("ParentResponseId");

-- Curtidas em avaliações e em respostas (uma por usuário)
CREATE TABLE "ReviewLikes" (
    "UserId" uuid NOT NULL,
    "ReviewId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_ReviewLikes" PRIMARY KEY ("UserId", "ReviewId"),
    CONSTRAINT "FK_ReviewLikes_Users_UserId"
        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ReviewLikes_Reviews_ReviewId"
        FOREIGN KEY ("ReviewId") REFERENCES "Reviews" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_ReviewLikes_ReviewId" ON "ReviewLikes" ("ReviewId");

CREATE TABLE "ResponseLikes" (
    "UserId" uuid NOT NULL,
    "ResponseId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_ResponseLikes" PRIMARY KEY ("UserId", "ResponseId"),
    CONSTRAINT "FK_ResponseLikes_Users_UserId"
        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ResponseLikes_Responses_ResponseId"
        FOREIGN KEY ("ResponseId") REFERENCES "Responses" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_ResponseLikes_ResponseId" ON "ResponseLikes" ("ResponseId");

-- Notificações: quando alguém curte ou responde uma avaliação ou resposta do usuário
CREATE TABLE "Notifications" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    -- Quem recebe o aviso
    "UserId" uuid NOT NULL,
    -- Quem curtiu ou respondeu
    "ActorId" uuid NOT NULL,
    "Type" text NOT NULL,
    -- Filme e avaliação: só nos avisos de avaliação
    "MovieId" uuid,
    "ReviewId" uuid,
    -- Resposta curtida, ou a resposta nova (avisos de resposta); nula em curtida de avaliação
    "ResponseId" uuid,
    -- Convite do plano familiar: só nos avisos de plano (chave estrangeira criada depois de "FamilyInvites")
    "FamilyInviteId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ReadAt" timestamp with time zone,
    CONSTRAINT "PK_Notifications" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Notifications_Users_UserId"
        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Notifications_Users_ActorId"
        FOREIGN KEY ("ActorId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Notifications_Movies_MovieId"
        FOREIGN KEY ("MovieId") REFERENCES "Movies" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Notifications_Reviews_ReviewId"
        FOREIGN KEY ("ReviewId") REFERENCES "Reviews" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Notifications_Responses_ResponseId"
        FOREIGN KEY ("ResponseId") REFERENCES "Responses" ("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_Notifications_Type"
        CHECK ("Type" IN ('ReviewLiked', 'ReplyLiked', 'ReviewReplied', 'ReplyReplied',
                          'PlanInviteReceived', 'PlanInviteAccepted', 'PlanInviteDeclined')),
    -- Avisos de avaliação têm filme e avaliação; avisos de plano têm só o convite
    CONSTRAINT "CK_Notifications_Target"
        CHECK (
            ("Type" IN ('ReviewLiked', 'ReplyLiked', 'ReviewReplied', 'ReplyReplied')
                AND "MovieId" IS NOT NULL AND "ReviewId" IS NOT NULL AND "FamilyInviteId" IS NULL)
            OR
            ("Type" IN ('PlanInviteReceived', 'PlanInviteAccepted', 'PlanInviteDeclined')
                AND "FamilyInviteId" IS NOT NULL
                AND "MovieId" IS NULL AND "ReviewId" IS NULL AND "ResponseId" IS NULL)
        )
);

CREATE INDEX "IX_Notifications_UserId_CreatedAt" ON "Notifications" ("UserId", "CreatedAt");
-- Contador do sino: só as não lidas de cada usuário
CREATE INDEX "IX_Notifications_UserId_Unread" ON "Notifications" ("UserId") WHERE "ReadAt" IS NULL;
CREATE INDEX "IX_Notifications_ReviewId" ON "Notifications" ("ReviewId");
CREATE INDEX "IX_Notifications_ResponseId" ON "Notifications" ("ResponseId");
CREATE INDEX "IX_Notifications_FamilyInviteId" ON "Notifications" ("FamilyInviteId");

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

-- Avisos de plano apontam para o convite (a tabela "Notifications" vem antes desta)
ALTER TABLE "Notifications"
    ADD CONSTRAINT "FK_Notifications_FamilyInvites_FamilyInviteId"
    FOREIGN KEY ("FamilyInviteId") REFERENCES "FamilyInvites" ("Id") ON DELETE CASCADE;

COMMIT;