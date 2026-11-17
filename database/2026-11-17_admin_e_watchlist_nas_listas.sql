-- Perfil de administrador, remoção de filmes que mantém o histórico e watchlist dentro de "Minhas listas".
-- Rodar uma única vez no banco t3_grupo_6 (psql ou DBeaver), antes de subir a API nova.
-- É idempotente: rodar de novo não quebra nada nem duplica a watchlist de ninguém.

BEGIN;

-- 1. ADMINISTRADORES: pode haver mais de um. O primeiro é marcado no fim deste arquivo;
--    os próximos, pelo painel do administrador (aba "Administradores").
ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "IsAdmin" boolean NOT NULL DEFAULT false;

-- 2. REMOÇÃO DE FILMES: o administrador remove um filme e ele some do catálogo, da busca, das
--    listas e da ficha, mas a linha continua no banco para o histórico de quem já assistiu.
--    Nulo = filme no catálogo.
ALTER TABLE "Movies" ADD COLUMN IF NOT EXISTS "DeletedAt" timestamp with time zone;

-- 3. WATCHLIST COMO LISTA: cada usuário tem no máximo uma lista com IsWatchlist = true.
--    Ela se comporta como as outras listas, só não pode ser renomeada nem excluída.
ALTER TABLE "Lists" ADD COLUMN IF NOT EXISTS "IsWatchlist" boolean NOT NULL DEFAULT false;

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Lists_UserId_Watchlist" ON "Lists" ("UserId") WHERE "IsWatchlist";

-- O nome "Watchlist" passa a ser reservado. Quem já tinha uma lista comum com esse nome
-- fica com ela, renomeada para "Watchlist (lista)".
UPDATE "Lists"
SET "Title" = "Title" || ' (lista)'
WHERE "Title" = 'Watchlist' AND NOT "IsWatchlist";

-- Copia a watchlist antiga (tabela "Watchlist") para a lista nova, na ordem em que os filmes
-- foram adicionados. Só copia para quem ainda não tem a lista nova, então rodar de novo não
-- traz de volta filmes que a pessoa já tirou da watchlist. Se a tabela antiga já foi apagada
-- (ou o banco foi criado pelo schema.sql novo), não há nada para copiar.
DO $$
BEGIN
    IF to_regclass('"Watchlist"') IS NULL THEN
        RETURN;
    END IF;

    WITH created AS (
        INSERT INTO "Lists" ("UserId", "Title", "Description", "IsPublic", "IsWatchlist", "CreatedAt")
        SELECT w."UserId", 'Watchlist', NULL, false, true, min(w."AddedAt")
        FROM "Watchlist" w
        WHERE NOT EXISTS (
            SELECT 1 FROM "Lists" l WHERE l."UserId" = w."UserId" AND l."IsWatchlist")
        GROUP BY w."UserId"
        RETURNING "Id", "UserId"
    )
    INSERT INTO "ListMovies" ("ListId", "MovieId", "Position", "AddedAt")
    SELECT c."Id",
           w."MovieId",
           row_number() OVER (PARTITION BY w."UserId" ORDER BY w."AddedAt", w."MovieId") - 1,
           w."AddedAt"
    FROM created c
    JOIN "Watchlist" w ON w."UserId" = c."UserId";
END
$$;

-- A tabela "Watchlist" antiga não é mais usada pela API, mas fica no banco enquanto alguém do
-- grupo ainda estiver com a versão anterior. Quando todos atualizarem, ela pode ser apagada com
-- database/2026-11-17_remover_tabela_watchlist.sql.

COMMIT;

-- 4. PRIMEIRO ADMINISTRADOR: troque o nome de usuário e rode este comando (uma vez só).
-- UPDATE "Users" SET "IsAdmin" = true WHERE "Name" = 'seu_nome_de_usuario';
