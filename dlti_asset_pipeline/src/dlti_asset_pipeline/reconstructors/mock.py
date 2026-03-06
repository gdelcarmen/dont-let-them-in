from __future__ import annotations

from pathlib import Path

import trimesh
from trimesh.visual.material import PBRMaterial
from trimesh.visual.texture import TextureVisuals

from dlti_asset_pipeline.core.models import GeneratedImage, HardwareRequirements, PipelineConfig, ReconstructionResult
from dlti_asset_pipeline.core.types import ReconstructorBackend
from dlti_asset_pipeline.reconstructors.base import MeshReconstructor
from dlti_asset_pipeline.storage import StorageManager


class MockMeshReconstructor(MeshReconstructor):
    def __init__(self, config: PipelineConfig, storage: StorageManager) -> None:
        self.config = config
        self.storage = storage

    def supported_backend(self) -> ReconstructorBackend:
        return ReconstructorBackend.MOCK

    def health_check(self) -> bool:
        return True

    def hardware_requirements(self) -> HardwareRequirements:
        return HardwareRequirements(minimum_vram_gb=0.0, cuda_required=False, notes="CPU-only mock backend")

    def reconstruct(self, generated_image: GeneratedImage) -> ReconstructionResult:
        request_id = generated_image.file_path.stem
        mesh = self._build_mesh(request_id)
        output_dir = self.storage.base_output_directory / "meshes"
        output_dir.mkdir(parents=True, exist_ok=True)
        output_path = output_dir / f"{request_id}.glb"
        mesh.export(output_path)
        extents = tuple(float(value) for value in mesh.bounding_box.extents)
        return ReconstructionResult(
            file_path=output_path,
            source_image_path=generated_image.file_path,
            reconstructor_backend=self.supported_backend(),
            triangle_count=len(mesh.faces),
            vertex_count=len(mesh.vertices),
            textures_generated=False,
            bounding_box_dimensions=extents,
        )

    def _build_mesh(self, key: str) -> trimesh.Trimesh:
        selector = sum(ord(char) for char in key) % 3
        if selector == 0:
            mesh = trimesh.creation.box(extents=(1.2, 0.9, 1.0))
        elif selector == 1:
            mesh = trimesh.creation.icosphere(subdivisions=3, radius=0.55)
        else:
            mesh = trimesh.creation.cylinder(radius=0.45, height=1.3, sections=32)
        material = PBRMaterial(baseColorFactor=[140, 207, 184, 255], emissiveFactor=[0.05, 0.1, 0.08])
        mesh.visual = TextureVisuals(material=material)
        return mesh
