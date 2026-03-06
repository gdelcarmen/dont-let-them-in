from __future__ import annotations

import json
from pathlib import Path

import aiofiles

from dlti_asset_pipeline.core.models import PipelineConfig
from dlti_asset_pipeline.core.types import AssetCategory


class StorageManager:
    def __init__(self, config: PipelineConfig) -> None:
        self.config = config
        self.base_output_directory = config.storage.base_output_directory
        self.registry_directory = config.storage.registry_directory
        self.viewer_directory = config.storage.viewer_directory

    def ensure_directories(self) -> None:
        for path in [
            self.base_output_directory,
            self.registry_directory,
            self.viewer_directory,
            self.base_output_directory / "images",
            self.base_output_directory / "meshes",
            self.base_output_directory / "postprocessed",
            self.base_output_directory / "textures",
        ]:
            path.mkdir(parents=True, exist_ok=True)

    def category_dir(self, stage: str, category: AssetCategory) -> Path:
        root = self.base_output_directory / stage
        if self.config.storage.organize_by_category:
            category_name = category.value if hasattr(category, "value") else str(category)
            root = root / category_name
        root.mkdir(parents=True, exist_ok=True)
        return root

    async def append_jsonl(self, path: Path, payload: dict) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        async with aiofiles.open(path, "a", encoding="utf-8") as handle:
            await handle.write(json.dumps(payload) + "\n")

    def write_json(self, path: Path, payload: dict) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(payload, indent=2, default=str))
