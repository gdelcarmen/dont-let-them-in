from __future__ import annotations

from pathlib import Path

from dlti_asset_pipeline.core.models import PipelineConfig, PostProcessedAsset, ReconstructionResult
from dlti_asset_pipeline.core.types import AssetCategory
from dlti_asset_pipeline.postprocessing.operations import MeshPostProcessor, target_for_category


class PostProcessingPipeline:
    def __init__(self, config: PipelineConfig) -> None:
        self.config = config
        self.processor = MeshPostProcessor(config)

    def run(self, reconstruction: ReconstructionResult, category: AssetCategory, output_root: Path) -> PostProcessedAsset:
        target_tris, _, target_dimensions = target_for_category(self.config, category)
        current_path = reconstruction.file_path
        operations: list[str] = []
        stage_dir = output_root / reconstruction.file_path.stem
        stage_dir.mkdir(parents=True, exist_ok=True)

        for operation_name in self.config.postprocessing.operation_sequence:
            if operation_name == "orientation_fix":
                current_path, note = self.processor.orientation_fix(current_path, stage_dir / "oriented.glb")
            elif operation_name == "scale_normalization":
                current_path, note = self.processor.scale_normalization(current_path, stage_dir / "scaled.glb", target_dimensions)
            elif operation_name == "decimation":
                current_path, note = self.processor.decimation(current_path, stage_dir / "decimated.glb", target_tris)
            elif operation_name == "format_conversion":
                current_path, note = self.processor.format_conversion(current_path, stage_dir / "final", self.config.reconstructor.output_format)
            else:
                continue
            operations.append(note)

        mesh = self.processor.load_mesh(current_path)
        file_size = current_path.stat().st_size
        return PostProcessedAsset(
            file_path=current_path,
            original_reconstruction_path=reconstruction.file_path,
            operations_applied=operations,
            final_triangle_count=len(mesh.faces),
            final_texture_dimensions=None,
            file_size_bytes=file_size,
        )
