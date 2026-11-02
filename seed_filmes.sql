-- Povoamento inicial do catálogo (20 filmes).
-- Gerado a partir de src/mocks/movies.js. Pode ser rodado de novo: IDs fixos + ON CONFLICT.
-- Observação: ReleaseDate usa 1º de janeiro do ano, porque o mock só tem o ano.
-- Rodar com: psql ... -v ON_ERROR_STOP=1 -1 -f seed_filmes.sql

INSERT INTO "Movies" ("Id", "Title", "Description", "ReleaseDate", "Rating", "Language", "Director", "PosterUrl", "Genres")
VALUES
  ('11111111-1111-4111-8111-000000000001', 'Metropolis', 'Em uma cidade dividida entre elite e operários, um jovem descobre o preço do luxo de seus governantes.', '1927-01-01', 4.6, 'Alemão', 'Fritz Lang', NULL, ARRAY['Drama', 'FiccaoCientifica']::text[]),
  ('11111111-1111-4111-8111-000000000002', 'Nosferatu', 'Um corretor de imóveis viaja aos Cárpatos e encontra um conde interessado em se mudar para a cidade dele.', '1922-01-01', 4.2, 'Alemão', 'F. W. Murnau', NULL, ARRAY['Terror']::text[]),
  ('11111111-1111-4111-8111-000000000003', 'O Gabinete do Dr. Caligari', 'Em uma feira alemã, um hipnotizador usa um sonâmbulo para cometer crimes.', '1920-01-01', 4.1, 'Alemão', 'Robert Wiene', NULL, ARRAY['Suspense', 'Terror']::text[]),
  ('11111111-1111-4111-8111-000000000004', 'Viagem à Lua', 'Um grupo de astrônomos parte num projétil em direção à Lua.', '1902-01-01', 3.8, 'Francês', 'Georges Méliès', NULL, ARRAY['Fantasia', 'FiccaoCientifica']::text[]),
  ('11111111-1111-4111-8111-000000000005', 'Casablanca', 'Em Marrocos, durante a guerra, um dono de bar precisa escolher entre o amor e o dever quando um antigo caso reaparece.', '1942-01-01', 4.5, 'Inglês', 'Michael Curtiz', NULL, ARRAY['Drama', 'Romance']::text[]),
  ('11111111-1111-4111-8111-000000000006', 'Cidadão Kane', 'A vida de um magnata da imprensa é reconstruída por meio das lembranças de quem o conheceu.', '1941-01-01', 4.4, 'Inglês', 'Orson Welles', NULL, ARRAY['Drama']::text[]),
  ('11111111-1111-4111-8111-000000000007', 'Os Sete Samurais', 'Um vilarejo contrata samurais sem mestre para proteger a colheita de bandidos.', '1954-01-01', 4.6, 'Japonês', 'Akira Kurosawa', NULL, ARRAY['Acao', 'Drama']::text[]),
  ('11111111-1111-4111-8111-000000000008', 'Viagem a Tóquio', 'Um casal idoso visita os filhos em Tóquio e se sente deslocado na vida agitada da cidade.', '1953-01-01', 4.3, 'Japonês', 'Yasujirō Ozu', NULL, ARRAY['Drama']::text[]),
  ('11111111-1111-4111-8111-000000000009', 'Psicose', 'Uma secretária foge com dinheiro roubado e para num motel isolado administrado por um rapaz tímido.', '1960-01-01', 4.4, 'Inglês', 'Alfred Hitchcock', NULL, ARRAY['Suspense', 'Terror']::text[]),
  ('11111111-1111-4111-8111-000000000010', 'A Noite dos Mortos-Vivos', 'Um grupo se refugia numa casa isolada enquanto mortos-vivos cercam o lugar.', '1968-01-01', 4, 'Inglês', 'George A. Romero', NULL, ARRAY['Terror']::text[]),
  ('11111111-1111-4111-8111-000000000011', 'Cinema Paradiso', 'Um cineasta relembra a infância numa cidade pequena da Sicília e o projecionista que foi seu mentor.', '1988-01-01', 4.5, 'Italiano', 'Giuseppe Tornatore', NULL, ARRAY['Drama', 'Romance']::text[]),
  ('11111111-1111-4111-8111-000000000012', 'Central do Brasil', 'Uma ex-professora que escreve cartas para analfabetos se envolve com um menino órfão na estação Central do Rio.', '1998-01-01', 4.4, 'Português', 'Walter Salles', NULL, ARRAY['Drama']::text[]),
  ('11111111-1111-4111-8111-000000000013', 'Pixote: A Lei do Mais Fraco', 'Um menino abandonado num reformatório sobrevive nas ruas e se envolve com o crime.', '1980-01-01', 4.1, 'Português', 'Hector Babenco', NULL, ARRAY['Crime', 'Drama']::text[]),
  ('11111111-1111-4111-8111-000000000014', 'Oldboy', 'Um homem é preso sem explicação por quinze anos e, ao ser libertado, parte em busca de vingança.', '2003-01-01', 4.3, 'Coreano', 'Park Chan-wook', NULL, ARRAY['Acao', 'Suspense']::text[]),
  ('11111111-1111-4111-8111-000000000015', 'Parasita', 'Uma família pobre se infiltra, um a um, na casa de uma família rica.', '2019-01-01', 4.5, 'Coreano', 'Bong Joon-ho', NULL, ARRAY['Drama', 'Suspense']::text[]),
  ('11111111-1111-4111-8111-000000000016', 'Amélie', 'Uma jovem de Montmartre decide mudar a vida das pessoas ao seu redor, em segredo.', '2001-01-01', 4.2, 'Francês', 'Jean-Pierre Jeunet', NULL, ARRAY['Comedia', 'Romance']::text[]),
  ('11111111-1111-4111-8111-000000000017', 'Pulp Fiction', 'Histórias de criminosos de Los Angeles se entrelaçam ao longo de um fim de semana violento.', '1994-01-01', 4.5, 'Inglês', 'Quentin Tarantino', NULL, ARRAY['Crime', 'Drama']::text[]),
  ('11111111-1111-4111-8111-000000000018', 'O Iluminado', 'Um escritor aceita cuidar de um hotel isolado no inverno, e a solidão começa a afetá-lo.', '1980-01-01', 4.2, 'Inglês', 'Stanley Kubrick', NULL, ARRAY['Suspense', 'Terror']::text[]),
  ('11111111-1111-4111-8111-000000000019', 'Um Corpo que Cai', 'Um ex-detetive obcecado por uma mulher passa a investigar o passado dela.', '1958-01-01', 4.4, 'Inglês', 'Alfred Hitchcock', NULL, ARRAY['Romance', 'Suspense']::text[]),
  ('11111111-1111-4111-8111-000000000020', 'Quero Ser John Malkovich', 'Um escriturário descobre uma passagem que leva à mente do ator John Malkovich.', '1999-01-01', 3.9, 'Inglês', 'Spike Jonze', NULL, ARRAY['Comedia', 'Fantasia']::text[])
ON CONFLICT ("Id") DO UPDATE SET
  "Title" = EXCLUDED."Title",
  "Description" = EXCLUDED."Description",
  "ReleaseDate" = EXCLUDED."ReleaseDate",
  "Rating" = EXCLUDED."Rating",
  "Language" = EXCLUDED."Language",
  "Director" = EXCLUDED."Director",
  "PosterUrl" = EXCLUDED."PosterUrl",
  "Genres" = EXCLUDED."Genres";
