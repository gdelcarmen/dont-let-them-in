from __future__ import annotations

from pathlib import Path

import trimesh

from dlti_asset_pipeline.core.types import AssetCategory
from dlti_asset_pipeline.quality.checks import aggregate_quality_report


def test_quality_report_generated(config, tmp_path: Path):
    mesh = trimesh.creation.box(extents=(1, 1, 1))
    path = tmp_path / "cube.glb"
    mesh.export(path)
    report = aggregate_quality_report(path, AssetCategory.FURNITURE, config)
    assert "triangle_count" in report.checks
    assert 0.0 <= report.overall_quality_score <= 1.0
