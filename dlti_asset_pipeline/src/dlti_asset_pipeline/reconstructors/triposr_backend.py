"""
Dependencies:
- torch
- transformers
- einops
- xatlas

Expected VRAM:
- Approximately 8GB CUDA GPU

Model weights:
- stabilityai/TripoSR from Hugging Face
- Download to a local model directory and point config.reconstructor.model_path to it
"""

from __future__ import annotations

from dlti_asset_pipeline.core.models import GeneratedImage, HardwareRequirements, ReconstructionResult
from dlti_asset_pipeline.core.types import ReconstructorBackend
from dlti_asset_pipeline.reconstructors.base import MeshReconstructor


class TripoSRMeshReconstructor(MeshReconstructor):
    """Stub for a TripoSR-backed single-image 3D reconstruction implementation."""

    def reconstruct(self, generated_image: GeneratedImage) -> ReconstructionResult:
        """Run TripoSR inference and export a raw GLB or glTF reconstruction artifact."""
        raise NotImplementedError("Requires GPU. Install TripoSR and configure TRIPOSR model path in config.")

    def supported_backend(self) -> ReconstructorBackend:
        return ReconstructorBackend.TRIPOSR

    def health_check(self) -> bool:
        """Verify CUDA availability and TripoSR checkpoint presence."""
        raise NotImplementedError("Requires GPU. Verify CUDA and TripoSR checkpoint availability.")

    def hardware_requirements(self) -> HardwareRequirements:
        """Report the baseline TripoSR VRAM requirement."""
        return HardwareRequirements(minimum_vram_gb=8.0, cuda_required=True, notes="Fast single-image reconstruction backend")
