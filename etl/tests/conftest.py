"""Fixtures the tests share: folders made ready for a run, deleted after each test."""

import shutil
from pathlib import Path

import pytest
from staging import FIXTURES


@pytest.fixture
def geodatabase(tmp_path: Path) -> Path:
    """A folder holding a copy of places.gdb: seven cafés with one fault each, and two parks."""
    shutil.copytree(FIXTURES / "sources" / "places.gdb", tmp_path / "places.gdb")
    return tmp_path
