from __future__ import annotations

from pathlib import Path

from dlti_asset_pipeline.core import AssetCategory, AssetRegistryEntry, AssetRequest
from dlti_asset_pipeline.registry import AssetRegistry


def test_registry_add_query_update(tmp_path: Path):
    registry = AssetRegistry(tmp_path / "registry")
    entry = AssetRegistryEntry(
        asset_id="asset_1",
        request=AssetRequest(asset_name="couch", description="a couch", category=AssetCategory.FURNITURE),
        asset_category=AssetCategory.FURNITURE,
    )
    registry.add(entry)
    assert registry.get("asset_1").asset_id == "asset_1"
    assert len(registry.query(category=AssetCategory.FURNITURE)) == 1
