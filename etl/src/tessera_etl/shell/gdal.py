"""Runs GDAL's ogr2ogr: the machine's own when it has one, and GDAL's Docker image when not.

***GDAL*** is the open library most map software uses to read and write map files; ogr2ogr is
its program that copies places from one file format to another. ***Docker*** runs a program
inside a sealed, ready-made copy of a system, called an image, so a machine without GDAL can
still run GDAL's own build.

The pipeline reads layers with pyogrio, so it runs ogr2ogr only for the few cases pyogrio cannot
read as needed (shell/extract.py lists them): curves, other coordinate systems and domains coded
by text. Every path passed in is absolute, and Docker sees each folder at the same path, so the
arguments need no change between the two.
"""

import functools
import subprocess
from collections.abc import Sequence

from ..kernel.result import Err, Ok, Result

IMAGE = "ghcr.io/osgeo/gdal:alpine-small-3.11.4"
"""GDAL's own image, for a machine with no ogr2ogr of its own; the tag fixes the version."""


@functools.cache
def _local() -> bool:
    """Whether this machine has its own ogr2ogr; asked once, at the first run."""
    return isinstance(_run_to_end("ogr2ogr", ("--version",)), Ok)


def run(arguments: Sequence[str], folders: Sequence[str]) -> Result[str, str]:
    """Runs ogr2ogr with `arguments` and returns what it wrote to its output.

    `folders` are the folders Docker must see, when it runs the image.
    """
    if _local():
        return _run_to_end("ogr2ogr", tuple(arguments))
    mounts = [part for folder in dict.fromkeys(folders) for part in ("--volume", f"{folder}:{folder}")]
    return _run_to_end("docker", ("run", "--rm", *mounts, IMAGE, "ogr2ogr", *arguments))


def _run_to_end(program: str, arguments: Sequence[str]) -> Result[str, str]:
    """Reads the program's output while it runs, so the pipe, the buffer of fixed size that carries
    the output here, never fills and stalls the program."""
    try:
        done = subprocess.run([program, *arguments], capture_output=True, check=False)
    except FileNotFoundError:
        return Err(f"{program} is not installed.")
    if done.returncode == 0:
        return Ok(done.stdout.decode("utf-8", "replace"))
    return Err(f"{program} failed: {done.stderr.decode('utf-8', 'replace').strip()}")
