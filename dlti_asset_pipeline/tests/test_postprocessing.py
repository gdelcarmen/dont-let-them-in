from __future__ import annotations

from pathlib import Path

import trimesh

from dlti_asset_pipeline.postprocessing.operations import MeshPostProcessor


def test_decimation_writes_mesh(config, tmp_path: Path):
    processor = MeshPostProcessor(config)
    mesh = trimesh.creation.icosphere(subdivisions=4, radius=1.0)
    source = tmp_path / "source.glb"
    mesh.export(source)
    target = tmp_path / "decimated.glb"
    output, note = processor.decimation(source, target, 100)
    assert output.exists()
    assert note.startswith("decimated_to_")
