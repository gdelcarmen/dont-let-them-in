"""
Dependencies:
- torch
- transformers

Expected VRAM:
- At least 12GB CUDA GPU

Model weights:
- SparseFlex / TripoSF weights from the official VAST AI release
"""

from __future__ import annotations

from dlti_asset_pipeline.core.models import GeneratedImage, HardwareRequirements, ReconstructionResult
from dlti_asset_pipeline.core.types import ReconstructorBackend
from dlti_asset_pipeline.reconstructors.base import MeshReconstructor


class TripoSFReconstructor(MeshReconstructor):
    """Stub for the TripoSF / SparseFlex reconstruction backend."""

    def reconstruct(self, generated_image: GeneratedImage) -> ReconstructionResult:
        """Run TripoSF inference and emit a high-resolution raw mesh result."""
        raise NotImplementedError("Requires GPU. Install TripoSF dependencies and configure model paths in config.")

    def supported_backend(self) -> ReconstructorBackend:
        return ReconstructorBackend.TRIPOSF

    def health_check(self) -> bool:
        """Verify CUDA availability and SparseFlex checkpoint presence."""
        raise NotImplementedError("Requires GPU. Verify CUDA and TripoSF model availability.")

    def hardware_requirements(self) -> HardwareRequirements:
        """Report the TripoSF baseline VRAM requirement."""
        return HardwareRequirements(minimum_vram_gb=12.0, cuda_required=True, notes="Highest quality but heaviest local backend")
