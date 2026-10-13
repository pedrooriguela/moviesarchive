BEGIN;

-- ============================================================
-- 1. PLANOS
-- ============================================================
CREATE TABLE "Plans" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "Name" text NOT NULL,
    "Price" numeric NOT NULL DEFAULT 0,
    "Description" text,
    CONSTRAINT "PK_Plans" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_Plans_Price" CHECK ("Price" >= 0)
);

CREATE UNIQUE INDEX "IX_Plans_Name" ON "Plans" ("Name");

-- ============================================================
-- 2. USUÁRIOS
-- ============================================================
CREATE TABLE "Users" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "Name" text NOT NULL,
    "Email" text NOT NULL,
    "Password" text NOT NULL,
    "PhotoUrl" text,
    "PlanId" uuid,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Users_Plans_PlanId"
        FOREIGN KEY ("PlanId") REFERENCES "Plans" ("Id") ON DELETE SET NULL
);

-- Unicidade de email sem diferenciar maiúsculas/minúsculas
CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" (lower("Email"));
CREATE INDEX "IX_Users_PlanId" ON "Users" ("PlanId");

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
    "Rating" numeric,
    "Language" text,
    "PosterUrl" text,
    CONSTRAINT "PK_Movies" PRIMARY KEY ("Id")
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
    "AddedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_ListMovies" PRIMARY KEY ("ListId", "MovieId"),
    CONSTRAINT "FK_ListMovies_Lists_ListId"
        FOREIGN KEY ("ListId") REFERENCES "Lists" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ListMovies_Movies_MovieId"
        FOREIGN KEY ("MovieId") REFERENCES "Movies" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_ListMovies_MovieId" ON "ListMovies" ("MovieId");

-- ============================================================
-- 8. AVALIAÇÕES (REVIEWS)
-- ============================================================
CREATE TABLE "Reviews" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "UserId" uuid NOT NULL,
    "MovieId" uuid NOT NULL,
    "Rating" integer NOT NULL,
    "Comment" text,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_Reviews" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Reviews_Movies_MovieId"
        FOREIGN KEY ("MovieId") REFERENCES "Movies" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Reviews_Users_UserId"
        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "CK_Reviews_Rating" CHECK ("Rating" BETWEEN 1 AND 5)
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

COMMIT;