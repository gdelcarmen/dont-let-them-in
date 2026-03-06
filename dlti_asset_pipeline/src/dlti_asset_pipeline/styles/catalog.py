from __future__ import annotations

from importlib import resources
from pathlib import Path

import yaml

from dlti_asset_pipeline.core.models import AssetRequest, CatalogAssetDefinition, StyleConfig
from dlti_asset_pipeline.core.types import AssetCategory, CameraAngle


def _load_yaml_resource(resource_name: str) -> dict:
    data = resources.files("dlti_asset_pipeline.resources").joinpath(resource_name).read_text()
    return yaml.safe_load(data)


def load_base_style() -> StyleConfig:
    payload = _load_yaml_resource("styles/dlti_base_style.yaml")
    return StyleConfig.model_validate(payload)


def load_catalog(path: str | Path | None = None) -> dict[str, CatalogAssetDefinition]:
    if path is None:
        payload = _load_yaml_resource("catalogs/dlti_asset_catalog.yaml")
    else:
        payload = yaml.safe_load(Path(path).read_text())
    return {
        item["asset_name"]: CatalogAssetDefinition.model_validate(item)
        for item in payload["assets"]
    }


class StyleResolver:
    def __init__(self, catalog: dict[str, CatalogAssetDefinition] | None = None, base_style: StyleConfig | None = None) -> None:
        self.catalog = catalog or load_catalog()
        self.base_style = base_style or load_base_style()

    def resolve(self, request: AssetRequest) -> tuple[StyleConfig, CatalogAssetDefinition | None]:
        style = self.base_style.model_copy(deep=True)
        catalog_entry = self.catalog.get(request.asset_name)
        if catalog_entry:
            self._apply_catalog(style, catalog_entry, request.category)
        if request.style_override:
            style = style.model_copy(update=request.style_override.model_dump(exclude_unset=True))
        return style, catalog_entry

    def _apply_catalog(self, style: StyleConfig, entry: CatalogAssetDefinition, category: AssetCategory) -> None:
        overrides = entry.style_overrides
        if "color_palette_keywords" in overrides:
            style.color_palette_keywords = overrides["color_palette_keywords"]
        if "art_style_keywords" in overrides:
            style.art_style_keywords = overrides["art_style_keywords"]
        if "background_preference" in overrides:
            style.background_preference = overrides["background_preference"]
        if "camera_angle_preference" in overrides:
            style.camera_angle_preference = overrides["camera_angle_preference"]
        else:
            style.camera_angle_preference = self._camera_for_category(category)

    def _camera_for_category(self, category: AssetCategory) -> CameraAngle:
        mapping = {
            AssetCategory.FURNITURE: CameraAngle.THREE_QUARTER_TOPDOWN,
            AssetCategory.DEFENSE: CameraAngle.THREE_QUARTER_TOPDOWN,
            AssetCategory.ENEMY: CameraAngle.THREE_QUARTER_FRONT,
            AssetCategory.ENVIRONMENTAL: CameraAngle.TOP_DOWN,
            AssetCategory.VFX: CameraAngle.FRONT_FACING,
        }
        return mapping[category]
