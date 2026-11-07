-- Planos, assinaturas, convites do plano familiar e classificação indicativa dos filmes.
-- Rodar uma única vez no banco t3_grupo_6 (psql ou DBeaver), antes de subir a API nova.
-- É idempotente: rodar de novo não quebra nada.
--
-- Só acrescenta colunas e tabelas, então o código atual da main continua funcionando.
-- A coluna antiga Users.PlanId fica por enquanto, porque a main ainda a lê:
-- ver a ETAPA 2 no fim do arquivo.

BEGIN;

-- ------------------------------------------------------------
-- 1. Plans: tipo, limite de integrantes e se está à venda
-- ------------------------------------------------------------
ALTER TABLE "Plans" ADD COLUMN IF NOT EXISTS "Type" text;
ALTER TABLE "Plans" ADD COLUMN IF NOT EXISTS "MaxMembers" integer;
ALTER TABLE "Plans" ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT true;
ALTER TABLE "Plans" ALTER COLUMN "Price" TYPE numeric(10,2);

-- Planos que já existissem sem tipo (o código nunca usou esta tabela) saem de venda
UPDATE "Plans" SET "Type" = 'Individual', "MaxMembers" = 1, "IsActive" = false
WHERE "Type" IS NULL;

ALTER TABLE "Plans" ALTER COLUMN "Type" SET NOT NULL;
ALTER TABLE "Plans" ALTER COLUMN "MaxMembers" SET NOT NULL;

ALTER TABLE "Plans" DROP CONSTRAINT IF EXISTS "CK_Plans_Type";
ALTER TABLE "Plans" ADD CONSTRAINT "CK_Plans_Type"
    CHECK ("Type" IN ('Individual', 'Family'));

-- Individual: 1 pessoa. Familiar: até 4, titular incluído.
ALTER TABLE "Plans" DROP CONSTRAINT IF EXISTS "CK_Plans_MaxMembers";
ALTER TABLE "Plans" ADD CONSTRAINT "CK_Plans_MaxMembers" CHECK (
    ("Type" = 'Individual' AND "MaxMembers" = 1) OR
    ("Type" = 'Family' AND "MaxMembers" BETWEEN 2 AND 4));

-- Os dois planos à venda (preços de exemplo; o admin poderá mudar)
INSERT INTO "Plans" ("Name", "Description", "Type", "Price", "MaxMembers", "IsActive")
SELECT v.name, v.description, v.type, v.price, v.max_members, true
FROM (VALUES
    ('Individual', 'Assista e baixe os filmes do acervo. Para uma pessoa.', 'Individual', 19.90, 1),
    ('Familiar', 'Tudo do Individual para até 4 pessoas, cada uma com a própria conta.', 'Family', 34.90, 4)
) AS v(name, description, type, price, max_members)
WHERE NOT EXISTS (SELECT 1 FROM "Plans" p WHERE p."Name" = v.name);

-- ------------------------------------------------------------
-- 2. Subscriptions: a assinatura de um titular.
--    EndedAt nulo = ativa. Trocar de plano encerra a linha (PlanChanged) e abre outra.
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "Subscriptions" (
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
    -- Encerrada sempre tem motivo, e ativa nunca tem
    CONSTRAINT "CK_Subscriptions_Ended"
        CHECK (("EndedAt" IS NULL) = ("EndReason" IS NULL)),
    CONSTRAINT "CK_Subscriptions_Dates"
        CHECK ("EndedAt" IS NULL OR "EndedAt" >= "StartedAt")
);

-- Um titular tem no máximo uma assinatura ativa
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Subscriptions_OwnerId_Active"
    ON "Subscriptions" ("OwnerId") WHERE "EndedAt" IS NULL;
CREATE INDEX IF NOT EXISTS "IX_Subscriptions_PlanId" ON "Subscriptions" ("PlanId");

-- ------------------------------------------------------------
-- 3. Users: data de nascimento e assinatura de que a pessoa faz parte
-- ------------------------------------------------------------
-- Opcional no banco porque as contas atuais não têm; a API exige no cadastro novo
-- e antes de contratar ou aceitar convite
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "BirthDate" date;
ALTER TABLE "Users" DROP CONSTRAINT IF EXISTS "CK_Users_BirthDate";
ALTER TABLE "Users" ADD CONSTRAINT "CK_Users_BirthDate"
    CHECK ("BirthDate" IS NULL OR "BirthDate" >= DATE '1900-01-01');

-- Aponta só para a assinatura ATIVA (titular ou convidado). Nula = conta gratuita.
-- Ao encerrar uma assinatura, a API zera esta coluna de todos os integrantes.
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "SubscriptionId" uuid;
ALTER TABLE "Users" DROP CONSTRAINT IF EXISTS "FK_Users_Subscriptions_SubscriptionId";
ALTER TABLE "Users" ADD CONSTRAINT "FK_Users_Subscriptions_SubscriptionId"
    FOREIGN KEY ("SubscriptionId") REFERENCES "Subscriptions" ("Id") ON DELETE SET NULL;
-- Buscar os integrantes de uma assinatura vai direto às linhas certas
CREATE INDEX IF NOT EXISTS "IX_Users_SubscriptionId" ON "Users" ("SubscriptionId");

-- ------------------------------------------------------------
-- 4. FamilyInvites: convites do titular do plano familiar
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS "FamilyInvites" (
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
    -- Pendente nunca tem data de resposta; os demais sempre têm
    CONSTRAINT "CK_FamilyInvites_Responded"
        CHECK (("Status" = 'Pending') = ("RespondedAt" IS NULL)),
    CONSTRAINT "CK_FamilyInvites_Expires"
        CHECK ("ExpiresAt" > "CreatedAt")
);

-- No máximo um convite pendente por pessoa na mesma assinatura.
-- Um pendente vencido continua 'Pending' aqui: a API o cancela antes de convidar de novo.
CREATE UNIQUE INDEX IF NOT EXISTS "IX_FamilyInvites_Pending"
    ON "FamilyInvites" ("SubscriptionId", "InvitedUserId") WHERE "Status" = 'Pending';
CREATE INDEX IF NOT EXISTS "IX_FamilyInvites_InvitedUserId" ON "FamilyInvites" ("InvitedUserId");

-- ------------------------------------------------------------
-- 5. Movies: classificação indicativa (idade mínima, 0 = livre) e duração
-- ------------------------------------------------------------
-- Sem classificação conhecida, 18: na dúvida, esconder de menores
ALTER TABLE "Movies" ADD COLUMN IF NOT EXISTS "AgeRating" integer NOT NULL DEFAULT 18;
ALTER TABLE "Movies" ADD COLUMN IF NOT EXISTS "RuntimeMinutes" integer;

ALTER TABLE "Movies" DROP CONSTRAINT IF EXISTS "CK_Movies_AgeRating";
ALTER TABLE "Movies" ADD CONSTRAINT "CK_Movies_AgeRating"
    CHECK ("AgeRating" IN (0, 10, 12, 14, 16, 18));
ALTER TABLE "Movies" DROP CONSTRAINT IF EXISTS "CK_Movies_RuntimeMinutes";
ALTER TABLE "Movies" ADD CONSTRAINT "CK_Movies_RuntimeMinutes"
    CHECK ("RuntimeMinutes" IS NULL OR "RuntimeMinutes" > 0);

-- Os 20 filmes da carga de teste (seed_filmes.sql). Classificações e durações APROXIMADAS,
-- só para testar; não são dados oficiais. Só preenche quem ainda não tem duração,
-- para não sobrescrever o que for ajustado depois.
UPDATE "Movies" AS m
SET "AgeRating" = v.age_rating, "RuntimeMinutes" = v.runtime
FROM (VALUES
    ('11111111-1111-4111-8111-000000000001'::uuid, 10, 153),  -- Metropolis
    ('11111111-1111-4111-8111-000000000002'::uuid, 12, 94),   -- Nosferatu
    ('11111111-1111-4111-8111-000000000003'::uuid, 12, 76),   -- O Gabinete do Dr. Caligari
    ('11111111-1111-4111-8111-000000000004'::uuid, 0, 13),    -- Viagem à Lua
    ('11111111-1111-4111-8111-000000000005'::uuid, 10, 102),  -- Casablanca
    ('11111111-1111-4111-8111-000000000006'::uuid, 10, 119),  -- Cidadão Kane
    ('11111111-1111-4111-8111-000000000007'::uuid, 14, 207),  -- Os Sete Samurais
    ('11111111-1111-4111-8111-000000000008'::uuid, 0, 136),   -- Viagem a Tóquio
    ('11111111-1111-4111-8111-000000000009'::uuid, 14, 109),  -- Psicose
    ('11111111-1111-4111-8111-000000000010'::uuid, 16, 96),   -- A Noite dos Mortos-Vivos
    ('11111111-1111-4111-8111-000000000011'::uuid, 12, 124),  -- Cinema Paradiso
    ('11111111-1111-4111-8111-000000000012'::uuid, 12, 113),  -- Central do Brasil
    ('11111111-1111-4111-8111-000000000013'::uuid, 18, 128),  -- Pixote
    ('11111111-1111-4111-8111-000000000014'::uuid, 18, 120),  -- Oldboy
    ('11111111-1111-4111-8111-000000000015'::uuid, 16, 132),  -- Parasita
    ('11111111-1111-4111-8111-000000000016'::uuid, 14, 122),  -- Amélie
    ('11111111-1111-4111-8111-000000000017'::uuid, 18, 154),  -- Pulp Fiction
    ('11111111-1111-4111-8111-000000000018'::uuid, 16, 146),  -- O Iluminado
    ('11111111-1111-4111-8111-000000000019'::uuid, 14, 128),  -- Um Corpo que Cai
    ('11111111-1111-4111-8111-000000000020'::uuid, 14, 112)   -- Quero Ser John Malkovich
) AS v(id, age_rating, runtime)
WHERE m."Id" = v.id AND m."RuntimeMinutes" IS NULL;

COMMIT;

-- ============================================================
-- ETAPA 2: rodar só depois que todos estiverem com o código novo
-- (sem Users.PlanId), ou seja, depois do merge desta branch na main.
-- ============================================================
-- BEGIN;
-- ALTER TABLE "Users" DROP CONSTRAINT IF EXISTS "FK_Users_Plans_PlanId";
-- DROP INDEX IF EXISTS "IX_Users_PlanId";
-- ALTER TABLE "Users" DROP COLUMN IF EXISTS "PlanId";
-- COMMIT;
