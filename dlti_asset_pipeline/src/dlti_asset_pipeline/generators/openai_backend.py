"""
Dependencies:
- openai>=1.0

Expected usage:
- Remote API backend, no GPU required.
- Configure OPENAI_API_KEY through environment variables.
"""

from __future__ import annotations

from dlti_asset_pipeline.core.models import AssetRequest, GeneratedImage, StyleConfig
from dlti_asset_pipeline.core.types import ImageGeneratorBackend
from dlti_asset_pipeline.generators.base import ImageGenerator


class OpenAIImageGenerator(ImageGenerator):
    """Stub for the OpenAI image generation backend."""

    def generate(self, request: AssetRequest, style: StyleConfig) -> GeneratedImage:
        """Generate a styled concept image from an asset request."""
        raise NotImplementedError("Requires API access. Configure OPENAI_API_KEY and implement OpenAI image calls.")

    def supported_backend(self) -> ImageGeneratorBackend:
        return ImageGeneratorBackend.OPENAI

    def health_check(self) -> bool:
        """Verify API credentials and remote service reachability."""
        raise NotImplementedError("Requires API access. Check OpenAI credentials and connectivity.")
