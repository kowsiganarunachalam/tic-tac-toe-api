-- Schema for tic-tac-toe-api on PostgreSQL.
-- Inferred from the queries in Persistence/Implementation/GameDao.cs
-- (no DDL existed in the repo; tables were created ad hoc against CockroachDB).

CREATE TABLE IF NOT EXISTS players (
    id          BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name        TEXT NOT NULL,
    playername  TEXT NOT NULL,
    created_on  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS rooms (
    id          BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    room_code   TEXT NOT NULL,
    created_by  BIGINT NOT NULL REFERENCES players(id)
);

CREATE TABLE IF NOT EXISTS matches (
    id          BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    room_id     BIGINT NOT NULL REFERENCES rooms(id),
    winner      BIGINT NOT NULL REFERENCES players(id),
    created_on  TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by  BIGINT NOT NULL REFERENCES players(id)
);

CREATE TABLE IF NOT EXISTS room_players (
    room_id     BIGINT NOT NULL REFERENCES rooms(id),
    player_id   BIGINT NOT NULL REFERENCES players(id),
    UNIQUE (room_id, player_id)
);
