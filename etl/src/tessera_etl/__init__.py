"""Tessera's ETL tool in Python: a ***pipeline file*** in, one SQL ***seed script*** out.

The package has three layers. `kernel` holds the generic chain of ***steps*** and a result
type; `core` holds the pure work, from reading the pipeline file to writing SQL; `shell` reads
files, runs pyogrio and GDAL, and writes the script and its log.
"""
