from __future__ import annotations

from pathlib import Path

import pytest

from dlti_asset_pipeline.core import AssetCategory, AssetRequest
from dlti_asset_pipeline.pipeline import AssetPipelineOrchestrator, build_requests_from_catalog


@pytest.mark.asyncio
async def test_full_mock_pipeline_run(config, tmp_path: Path):
    config.storage.base_output_directory = tmp_path / "output"
    config.storage.registry_directory = tmp_path / "output" / "registry"
    config.storage.experiment_log_path = tmp_path / "output" / "experiment_log.jsonl"
    config.storage.viewer_directory = tmp_path / "output" / "viewers"
    orchestrator = AssetPipelineOrchestrator(config)
    request = AssetRequest(asset_name="couch", description="a couch", category=AssetCategory.FURNITURE)
    entry = await orchestrator.run_request(request)
    assert entry.generated_image is not None
    assert entry.reconstruction is not None
    assert entry.postprocessed_asset is not None
    assert entry.quality_report is not None
    assert entry.postprocessed_asset.file_path.exists()
    assert config.storage.experiment_log_path.exists()


@pytest.mark.asyncio
async def test_catalog_batch_run(config, tmp_path: Path):
    config.storage.base_output_directory = tmp_path / "batch_output"
    config.storage.registry_directory = tmp_path / "batch_output" / "registry"
    config.storage.experiment_log_path = tmp_path / "batch_output" / "experiment_log.jsonl"
    config.storage.viewer_directory = tmp_path / "batch_output" / "viewers"
    orchestrator = AssetPipelineOrchestrator(config)
    requests = build_requests_from_catalog(config)[:3]
    results = await orchestrator.run_batch(requests)
    assert len(results) == 3
    summary = orchestrator.registry.export_summary()
    assert summary["total_assets"] == 3
