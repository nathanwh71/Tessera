# The packaged database (US-01). The service, the import and the tests all start this one image, so
# a test meets the shipped roles, never a hand-made copy.
FROM postgis/postgis:18-3.6

# The image runs these scripts once, on a new, empty data folder, after its own 10_postgis.sh.
COPY initdb/ /docker-entrypoint-initdb.d/

# jit=off: with just-in-time compilation switched on, a tile query took 7.1 seconds at first and 0.016 at best;
# without it, 0.001 (phase 0's settings.md). The other values suit one office machine.
CMD ["postgres", \
     "-c", "jit=off", \
     "-c", "max_connections=20", \
     "-c", "shared_buffers=256MB", \
     "-c", "effective_cache_size=2GB", \
     "-c", "random_page_cost=1.1"]
