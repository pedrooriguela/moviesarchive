-- Notificações do plano familiar: convite recebido, convite aceito e convite recusado.
-- Rodar uma única vez no banco t3_grupo_6, DEPOIS de 2026-11-17_notificacoes.sql
-- (e de 2026-11-07_assinaturas.sql, que cria "FamilyInvites"). É idempotente.
--
-- O que muda em "Notifications":
--   - "MovieId" e "ReviewId" passam a aceitar nulo (avisos de plano não têm filme nem avaliação)
--   - coluna nova "FamilyInviteId": o convite do aviso (apagar o convite apaga o aviso)
--   - "Type" aceita os três tipos novos
--   - cada tipo exige as colunas certas: avisos de avaliação têm filme e avaliação;
--     avisos de plano têm só o convite

BEGIN;

ALTER TABLE "Notifications" ALTER COLUMN "MovieId" DROP NOT NULL;
ALTER TABLE "Notifications" ALTER COLUMN "ReviewId" DROP NOT NULL;

ALTER TABLE "Notifications" ADD COLUMN IF NOT EXISTS "FamilyInviteId" uuid;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Notifications_FamilyInvites_FamilyInviteId') THEN
        ALTER TABLE "Notifications"
            ADD CONSTRAINT "FK_Notifications_FamilyInvites_FamilyInviteId"
            FOREIGN KEY ("FamilyInviteId") REFERENCES "FamilyInvites" ("Id") ON DELETE CASCADE;
    END IF;
END $$;

ALTER TABLE "Notifications" DROP CONSTRAINT IF EXISTS "CK_Notifications_Type";
ALTER TABLE "Notifications" ADD CONSTRAINT "CK_Notifications_Type"
    CHECK ("Type" IN ('ReviewLiked', 'ReplyLiked', 'ReviewReplied', 'ReplyReplied',
                      'PlanInviteReceived', 'PlanInviteAccepted', 'PlanInviteDeclined'));

ALTER TABLE "Notifications" DROP CONSTRAINT IF EXISTS "CK_Notifications_Target";
ALTER TABLE "Notifications" ADD CONSTRAINT "CK_Notifications_Target"
    CHECK (
        ("Type" IN ('ReviewLiked', 'ReplyLiked', 'ReviewReplied', 'ReplyReplied')
            AND "MovieId" IS NOT NULL AND "ReviewId" IS NOT NULL AND "FamilyInviteId" IS NULL)
        OR
        ("Type" IN ('PlanInviteReceived', 'PlanInviteAccepted', 'PlanInviteDeclined')
            AND "FamilyInviteId" IS NOT NULL
            AND "MovieId" IS NULL AND "ReviewId" IS NULL AND "ResponseId" IS NULL)
    );

CREATE INDEX IF NOT EXISTS "IX_Notifications_FamilyInviteId" ON "Notifications" ("FamilyInviteId");

COMMIT;
