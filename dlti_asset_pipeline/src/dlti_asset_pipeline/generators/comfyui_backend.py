"""
Dependencies:
- requests or httpx

Expected usage:
- Connect to an existing ComfyUI server over HTTP

Model weights:
- Managed by the external ComfyUI instance
"""

from __future__ import annotations

from dlti_asset_pipeline.core.models import AssetRequest, GeneratedImage, StyleConfig
from dlti_asset_pipeline.core.types import ImageGeneratorBackend
from dlti_asset_pipeline.generators.base import ImageGenerator


class ComfyUIImageGenerator(ImageGenerator):
    """Stub for a ComfyUI HTTP API image generator."""

    def generate(self, request: AssetRequest, style: StyleConfig) -> GeneratedImage:
        """Submit a workflow to ComfyUI and persist the returned image artifact."""
        raise NotImplementedError("Requires a running ComfyUI server and configured workflow/API details.")

    def supported_backend(self) -> ImageGeneratorBackend:
        return ImageGeneratorBackend.COMFYUI

    def health_check(self) -> bool:
        """Verify the configured ComfyUI server is reachable."""
        raise NotImplementedError("Requires ComfyUI API endpoint configuration.")
