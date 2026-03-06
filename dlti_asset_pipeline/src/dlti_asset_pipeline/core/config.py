from __future__ import annotations

import json
import os
from pathlib import Path
from typing import Any

import yaml

from dlti_asset_pipeline.core.models import PipelineConfig


def _expand_env(value: Any) -> Any:
    if isinstance(value, dict):
        return {key: _expand_env(item) for key, item in value.items()}
    if isinstance(value, list):
        return [_expand_env(item) for item in value]
    if isinstance(value, str):
        return os.path.expandvars(value)
    return value


def load_config(path: str | Path) -> PipelineConfig:
    config_path = Path(path)
    raw = config_path.read_text()
    if config_path.suffix.lower() in {".yaml", ".yml"}:
        data = yaml.safe_load(raw) or {}
    else:
        data = json.loads(raw)
    return PipelineConfig.model_validate(_expand_env(data))
