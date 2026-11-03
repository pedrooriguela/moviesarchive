-- Remove a coluna Movies.Rating (nota fixa do filme).
-- A nota agora é a média das avaliações (tabela Reviews), calculada pela API.
--
-- ATENÇÃO: apaga as notas fixas gravadas nessa coluna, sem volta.
-- Rode só depois de a API nova (sem Movie.Rating) estar em uso por todo o grupo:
-- uma API antiga que ainda leia essa coluna passa a dar erro.
-- Faça um backup antes, se quiser guardar os valores:
--   CREATE TABLE "Movies_Rating_Backup" AS SELECT "Id", "Rating" FROM "Movies";

ALTER TABLE "Movies" DROP COLUMN IF EXISTS "Rating";
