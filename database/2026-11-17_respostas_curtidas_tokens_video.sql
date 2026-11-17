-- Respostas às avaliações (com respostas a respostas), curtidas, refresh tokens e vídeo dos filmes.
-- Rodar uma única vez no banco t3_grupo_6 (psql ou DBeaver), antes de subir a API nova.
-- É idempotente: rodar de novo não quebra nada.

BEGIN;

-- 1. RESPOSTAS: a tabela "Responses" já existe no schema da disciplina; aqui ela ganha
--    resposta a resposta (ParentResponseId) e exclusão que mantém a conversa (DeletedAt).
CREATE TABLE IF NOT EXISTS "Responses" (
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
CREATE INDEX IF NOT EXISTS "IX_Responses_ReviewId" ON "Responses" ("ReviewId");
CREATE INDEX IF NOT EXISTS "IX_Responses_UserId" ON "Responses" ("UserId");

ALTER TABLE "Responses" ADD COLUMN IF NOT EXISTS "ParentResponseId" uuid;
ALTER TABLE "Responses" ADD COLUMN IF NOT EXISTS "DeletedAt" timestamp with time zone;

-- Se a resposta de cima for apagada junto com a conta do autor, as de baixo continuam
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Responses_Responses_ParentResponseId') THEN
        ALTER TABLE "Responses"
            ADD CONSTRAINT "FK_Responses_Responses_ParentResponseId"
            FOREIGN KEY ("ParentResponseId") REFERENCES "Responses" ("Id") ON DELETE SET NULL;
    END IF;
END $$;
CREATE INDEX IF NOT EXISTS "IX_Responses_ParentResponseId" ON "Responses" ("ParentResponseId");

-- 2. CURTIDAS em avaliações e em respostas (uma por usuário)
CREATE TABLE IF NOT EXISTS "ReviewLikes" (
    "UserId" uuid NOT NULL,
    "ReviewId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_ReviewLikes" PRIMARY KEY ("UserId", "ReviewId"),
    CONSTRAINT "FK_ReviewLikes_Users_UserId"
        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ReviewLikes_Reviews_ReviewId"
        FOREIGN KEY ("ReviewId") REFERENCES "Reviews" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_ReviewLikes_ReviewId" ON "ReviewLikes" ("ReviewId");

CREATE TABLE IF NOT EXISTS "ResponseLikes" (
    "UserId" uuid NOT NULL,
    "ResponseId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_ResponseLikes" PRIMARY KEY ("UserId", "ResponseId"),
    CONSTRAINT "FK_ResponseLikes_Users_UserId"
        FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ResponseLikes_Responses_ResponseId"
        FOREIGN KEY ("ResponseId") REFERENCES "Responses" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_ResponseLikes_ResponseId" ON "ResponseLikes" ("ResponseId");

-- 3. REFRESH TOKENS: só o hash SHA-256 fica no banco; o valor vive no cookie httpOnly
CREATE TABLE IF NOT EXISTS "RefreshTokens" (
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
CREATE UNIQUE INDEX IF NOT EXISTS "IX_RefreshTokens_TokenHash" ON "RefreshTokens" ("TokenHash");
CREATE INDEX IF NOT EXISTS "IX_RefreshTokens_UserId" ON "RefreshTokens" ("UserId");

-- 4. VÍDEO: endereço do arquivo (MP4 ou WebM) que o player toca. Sem ele, o filme não tem play.
ALTER TABLE "Movies" ADD COLUMN IF NOT EXISTS "VideoUrl" text;

-- Exemplo, para testar o player: "A Noite dos Mortos-Vivos" (1968) está em domínio público
-- e tem uma cópia em MP4 no Internet Archive. Só preenche se o filme existir e ainda não tiver vídeo.
UPDATE "Movies"
SET "VideoUrl" = 'https://archive.org/download/Night.Of.The.Living.Dead_1080p/NightOfTheLivingDead_720p_512kb.mp4'
WHERE "VideoUrl" IS NULL
  AND ("Title" ILIKE 'Night of the Living Dead' OR "Title" ILIKE 'A Noite dos Mortos-Vivos')
  AND ("ReleaseDate" IS NULL OR extract(year FROM "ReleaseDate") = 1968);

COMMIT;
