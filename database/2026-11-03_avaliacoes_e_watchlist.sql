-- Avaliações com meia estrela e watchlist.
-- Rodar uma única vez no banco t3_grupo_6 (psql ou DBeaver), antes de subir a API nova.
-- É idempotente: rodar de novo não quebra nada.

BEGIN;

-- 1. Reviews.Rating: inteiro 1–5  ->  numeric(2,1) de 0,5 a 5, em passos de 0,5.
--    As notas já gravadas (inteiros) continuam válidas.
ALTER TABLE "Reviews" DROP CONSTRAINT IF EXISTS "CK_Reviews_Rating";

ALTER TABLE "Reviews"
    ALTER COLUMN "Rating" TYPE numeric(2,1) USING "Rating"::numeric(2,1);

ALTER TABLE "Reviews"
    ADD CONSTRAINT "CK_Reviews_Rating"
    CHECK ("Rating" BETWEEN 0.5 AND 5 AND mod("Rating" * 2, 1) = 0);

-- 2. Watchlist: filmes que o usuário quer assistir (N:N Usuário -> Filme)
CREATE TABLE IF NOT EXISTS "Watchlist" (
    "UserId" uuid NOT NULL,
    "MovieId" uuid NOT NULL,
    "AddedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_Watchlist" PRIMARY KEY ("UserId", "MovieId"),
    CONSTRAINT "FK_Watchlist_Users_UserId"
        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_Watchlist_Movies_MovieId"
        FOREIGN KEY ("MovieId") REFERENCES "Movies" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_Watchlist_MovieId" ON "Watchlist" ("MovieId");

COMMIT;
