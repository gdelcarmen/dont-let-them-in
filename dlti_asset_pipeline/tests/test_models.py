from __future__ import annotations

from pathlib import Path

import pytest
from pydantic import ValidationError

from dlti_asset_pipeline.core import AssetCategory, AssetRequest, StyleConfig, load_config


def test_asset_request_requires_description():
    with pytest.raises(ValidationError):
        AssetRequest(asset_name="couch", category=AssetCategory.FURNITURE)


def test_style_config_rejects_empty_keywords():
    with pytest.raises(ValidationError):
        StyleConfig(
            base_prompt_template="{asset_description}",
            negative_prompt="none",
            color_palette_keywords=[],
            art_style_keywords=["stylized"],
        )


def test_config_loads_defaults(config_path: Path):
    config = load_config(config_path)
    assert config.image_generator.backend == "mock"
    assert config.postprocessing.targets[AssetCategory.ENEMY].triangle_budget == 5000
