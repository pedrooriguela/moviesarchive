-- Alterações para o catálogo (busca, ficha e página inicial).
-- NÃO executar sem combinar com o grupo: o banco é compartilhado.
-- Revise antes de aplicar. Os valores de Genres devem bater com o enum Genre do back-end.

ALTER TABLE "Movies" ADD COLUMN IF NOT EXISTS "Director" text;

ALTER TABLE "Movies" ADD COLUMN IF NOT EXISTS "Genres" text[] NOT NULL DEFAULT '{}';

ALTER TABLE "Movies" DROP CONSTRAINT IF EXISTS "CK_Movies_Genres";

ALTER TABLE "Movies" ADD CONSTRAINT "CK_Movies_Genres"
    CHECK ("Genres" <@ ARRAY['Acao','Comedia','Crime','Drama','Fantasia','FiccaoCientifica','Romance','Suspense','Terror']::text[]);
