"""
Dependencies:
- official Tripo Python SDK or HTTP client

Expected usage:
- Cloud backend, no local GPU required

Model weights:
- Managed by the Tripo cloud service
"""

from __future__ import annotations

from dlti_asset_pipeline.core.models import GeneratedImage, HardwareRequirements, ReconstructionResult
from dlti_asset_pipeline.core.types import ReconstructorBackend
from dlti_asset_pipeline.reconstructors.base import MeshReconstructor


class TripoAPIReconstructor(MeshReconstructor):
    """Stub for the Tripo cloud API reconstruction backend."""

    def reconstruct(self, generated_image: GeneratedImage) -> ReconstructionResult:
        """Submit the source image to the Tripo API and persist the returned mesh artifact."""
        raise NotImplementedError("Requires Tripo API credentials and an implemented API client.")

    def supported_backend(self) -> ReconstructorBackend:
        return ReconstructorBackend.TRIPO_API

    def health_check(self) -> bool:
        """Verify Tripo API credentials and service reachability."""
        raise NotImplementedError("Requires Tripo API configuration.")

    def hardware_requirements(self) -> HardwareRequirements:
        """Report that the backend is cloud-hosted and does not require local CUDA."""
        return HardwareRequirements(minimum_vram_gb=0.0, cuda_required=False, notes="Cloud-hosted alternative to local reconstruction")
