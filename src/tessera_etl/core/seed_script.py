"""GDAL's arguments for the copy and the extract, and the head of the ***seed script***.

DbUp, a .NET library, runs each seed script once; `table_sql` writes each table's part. Every
function here is pure, so the tests check each choice without running GDAL.
"""

from collections.abc import Iterable

from .plan import TablePlan


def copy_first(source: str, tables: int) -> bool:
    """True when one copy of the source into a GeoPackage, a file with an index, saves time.

    That is when the source is GeoJSON, which has no index, so GDAL reads the whole file again
    for each table it extracts, and the pipeline file names more than one table. A geodatabase
    has indexes of its own, so it gains nothing from a copy.
    """
    dot = source.rfind(".")
    slash = max(source.rfind("/"), source.rfind("\\"))
    extension = source[dot:].lower() if dot > slash else ""
    return tables > 1 and extension in (".geojson", ".json")


def copy_arguments(source: str, geopackage: str) -> tuple[str, ...]:
    """ogr2ogr's arguments to copy the source, as it is, into a GeoPackage."""
    return ("-f", "GPKG", geopackage, source)


def extract_arguments(source: str, geojson: str, table: TablePlan) -> tuple[str, ...]:
    """ogr2ogr's arguments to extract one table to GeoJSON, ready for the filters.

    - EPSG:4326 puts every shape in longitude and latitude, whatever system the source uses.
    - -dim XY drops heights and measures, which a flat vector tile cannot carry.
    - CONVERT_TO_LINEAR turns true curves, such as a geodatabase's arcs, into short straight
      lines, which PostGIS's tile functions read.
    - -resolveDomains writes a coded value's description, such as 'Museum', in place of its code.
    - The layer, when given, picks one feature class of a geodatabase; -where picks rows.

    The Python build reads with pyogrio, and runs ogr2ogr with these arguments only for a table
    that holds curves (see shell/extract.py).
    """
    return (
        "-f",
        "GeoJSON",
        geojson,
        source,
        *((table.layer,) if table.layer is not None else ()),
        "-t_srs",
        "EPSG:4326",
        "-dim",
        "XY",
        "-nlt",
        "CONVERT_TO_LINEAR",
        "-resolveDomains",
        *(("-where", table.where) if table.where is not None else ()),
    )


def schema_and_name(table: TablePlan) -> tuple[str, str]:
    """The table's ***schema*** and name, which GDAL and the SQL take apart.

    The split is safe: the pipeline file's check has already refused any name not written
    schema.table.
    """
    schema, name = table.table.split(".")
    return schema, name


def head(pipeline_file: str, tables: Iterable[TablePlan]) -> tuple[str, ...]:
    """The script's first lines: who made it, a warning never to change it, and each schema."""
    schemas = dict.fromkeys(schema_and_name(table)[0] for table in tables)
    return (
        f"-- Made by 'npm run etl' from {pipeline_file}. DbUp runs this script once (ADR 0009).",
        "-- Never change its SQL after it has run: DbUp would not run it again. Write a new script instead.",
        *(f'CREATE SCHEMA IF NOT EXISTS "{schema}";' for schema in schemas),
    )
