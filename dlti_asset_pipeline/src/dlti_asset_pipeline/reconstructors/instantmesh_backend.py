"""
Dependencies:
- torch
- diffusers
- transformers

Expected VRAM:
- Approximately 12GB CUDA GPU

Model weights:
- InstantMesh weights from the official repository or Hugging Face release
"""

from __future__ import annotations

from dlti_asset_pipeline.core.models import GeneratedImage, HardwareRequirements, ReconstructionResult
from dlti_asset_pipeline.core.types import ReconstructorBackend
from dlti_asset_pipeline.reconstructors.base import MeshReconstructor


class InstantMeshReconstructor(MeshReconstructor):
    """Stub for the InstantMesh reconstruction backend."""

    def reconstruct(self, generated_image: GeneratedImage) -> ReconstructionResult:
        """Run InstantMesh multi-view reconstruction from a styled concept image."""
        raise NotImplementedError("Requires GPU. Install InstantMesh and configure model paths in config.")

    def supported_backend(self) -> ReconstructorBackend:
        return ReconstructorBackend.INSTANTMESH

    def health_check(self) -> bool:
        """Verify CUDA availability and InstantMesh checkpoint presence."""
        raise NotImplementedError("Requires GPU. Verify CUDA and InstantMesh model availability.")

    def hardware_requirements(self) -> HardwareRequirements:
        """Report the InstantMesh baseline VRAM requirement."""
        return HardwareRequirements(minimum_vram_gb=12.0, cuda_required=True, notes="Higher quality geometry for free-style images")
