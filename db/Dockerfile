# The packaged database (US-01). The service, its seed and the tests all start this one image, so
# a test meets the shipped roles, never a hand-made copy.
# PostgreSQL 18 with PostGIS 3.6 (US-01). A fixed tag means every machine, and every test, meets
# the same database until someone changes this line on purpose.
FROM postgis/postgis:18-3.6

# The image runs these scripts once, on a new, empty data folder, after its own 10_postgis.sh.
COPY initdb/ /docker-entrypoint-initdb.d/

# jit=off: with just-in-time compilation switched on, a tile query took 7.1 seconds at first and 0.016 at best;
# without it, 0.001 (phase 0's settings.md).
# max_connections=20: two copies of the service hold at most 4 connections each (Database.PoolSize,
# N-29), so 8; the rest leave room for the seed, npm run publish and psql.
# shared_buffers=256MB and effective_cache_size=2GB: the office machine shares its memory with the
# service, Nginx and Windows. PostgreSQL keeps 256 MB of tables for itself, and plans as if the
# operating system holds about 2 GB more of them in its own cache.
# random_page_cost=1.1: on an SSD, a read from anywhere costs about as much as the next read in
# order (the default, 4, suits a spinning disk), so the planner prefers the spatial index to
# reading whole tables.
CMD ["postgres", \
     "-c", "jit=off", \
     "-c", "max_connections=20", \
     "-c", "shared_buffers=256MB", \
     "-c", "effective_cache_size=2GB", \
     "-c", "random_page_cost=1.1"]
