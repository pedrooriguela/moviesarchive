-- Aplicar antes de executar esta versão da API. Não é executado automaticamente.
-- Compatível com vínculos existentes: todos permanecem aprovados.
BEGIN;
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "IsPrivate" boolean NOT NULL DEFAULT false;
ALTER TABLE "UserFollowers" ADD COLUMN IF NOT EXISTS "IsAccepted" boolean NOT NULL DEFAULT true;
CREATE INDEX IF NOT EXISTS "IX_UserFollowers_Pending" ON "UserFollowers" ("FollowingId", "CreatedAt") WHERE NOT "IsAccepted";
COMMIT;
