from __future__ import annotations

from pathlib import Path

import pytest

from dlti_asset_pipeline.core import load_config


@pytest.fixture()
def config_path() -> Path:
    return Path(__file__).parent / "fixtures" / "sample_config.yaml"


@pytest.fixture()
def config(config_path):
    return load_config(config_path)
