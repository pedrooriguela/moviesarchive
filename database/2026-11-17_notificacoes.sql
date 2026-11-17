-- Notificações: avisam quando alguém curte ou responde uma avaliação ou resposta sua.
-- Rodar uma única vez no banco t3_grupo_6, DEPOIS de 2026-11-17_respostas_curtidas_tokens_video.sql
-- (usa a tabela "Responses" com as colunas novas). É idempotente.

BEGIN;

CREATE TABLE IF NOT EXISTS "Notifications" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    -- Quem recebe o aviso
    "UserId" uuid NOT NULL,
    -- Quem curtiu ou respondeu
    "ActorId" uuid NOT NULL,
    "Type" text NOT NULL,
    "MovieId" uuid NOT NULL,
    "ReviewId" uuid NOT NULL,
    -- Resposta curtida, ou a resposta nova (avisos de resposta); nula em curtida de avaliação
    "ResponseId" uuid,
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
        CHECK ("Type" IN ('ReviewLiked', 'ReplyLiked', 'ReviewReplied', 'ReplyReplied'))
);

CREATE INDEX IF NOT EXISTS "IX_Notifications_UserId_CreatedAt" ON "Notifications" ("UserId", "CreatedAt");
-- Contador do sino: só as não lidas de cada usuário
CREATE INDEX IF NOT EXISTS "IX_Notifications_UserId_Unread" ON "Notifications" ("UserId") WHERE "ReadAt" IS NULL;
CREATE INDEX IF NOT EXISTS "IX_Notifications_ReviewId" ON "Notifications" ("ReviewId");
CREATE INDEX IF NOT EXISTS "IX_Notifications_ResponseId" ON "Notifications" ("ResponseId");

COMMIT;
