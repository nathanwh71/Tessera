-- The two roles Tessera signs in as (US-01 c1, ADR 0001). The tests run this same script, so a test
-- signs in as the shipped role, never a hand-made copy.
--
-- A role is a database user. tessera_reader is the service's role: it may read, and it sees only the
-- tables granted to it, so a grant publishes a table and a revoke withdraws it (US-03).
-- tessera_publisher is the data publisher's role: it owns the places' schemas and tables, so it
-- alone loads them (the seed, ADR 0009) and grants them (US-24).
--
-- Each password comes from the environment, never from this file. psql's \getenv reads it; the
-- image runs this file with ON_ERROR_STOP, so a missing password stops the first start.

\getenv reader_password TESSERA_READER_PASSWORD
\getenv publisher_password TESSERA_PUBLISHER_PASSWORD

\if :{?reader_password}
\else
  DO $$ BEGIN RAISE EXCEPTION 'Set TESSERA_READER_PASSWORD before the database first starts.'; END $$;
\endif
\if :{?publisher_password}
\else
  DO $$ BEGIN RAISE EXCEPTION 'Set TESSERA_PUBLISHER_PASSWORD before the database first starts.'; END $$;
\endif

CREATE ROLE tessera_reader LOGIN PASSWORD :'reader_password';
-- A second lock beside the grants: even a table granted for writing by mistake stays read-only.
ALTER ROLE tessera_reader SET default_transaction_read_only = on;

CREATE ROLE tessera_publisher LOGIN PASSWORD :'publisher_password';

-- The publisher makes a schema for each group of layers, so it needs CREATE on the database.
SELECT format('GRANT CONNECT ON DATABASE %I TO tessera_reader, tessera_publisher', current_database()) \gexec
SELECT format('GRANT CREATE ON DATABASE %I TO tessera_publisher', current_database()) \gexec
