-- Apaga a tabela "Watchlist" antiga, que a API deixou de usar: a watchlist agora é uma lista
-- (Lists.IsWatchlist), criada por database/2026-11-17_admin_e_watchlist_nas_listas.sql.
-- Rode só quando TODO o grupo já estiver com a API nova. Não tem volta.

DROP TABLE IF EXISTS "Watchlist";
