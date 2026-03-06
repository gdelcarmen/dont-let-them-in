"""
Dependencies:
- torch
- diffusers
- transformers

Expected VRAM:
- Minimum 6GB CUDA GPU

Model weights:
- Stable Diffusion XL or equivalent diffusers-compatible checkpoint
- Download from Hugging Face and point config to the local cache path
"""

from __future__ import annotations

from dlti_asset_pipeline.core.models import AssetRequest, GeneratedImage, StyleConfig
from dlti_asset_pipeline.core.types import ImageGeneratorBackend
from dlti_asset_pipeline.generators.base import ImageGenerator


class StableDiffusionLocalImageGenerator(ImageGenerator):
    """Stub for a local diffusers-backed Stable Diffusion image generator."""

    def generate(self, request: AssetRequest, style: StyleConfig) -> GeneratedImage:
        """Run local diffusion inference and persist a PNG concept image."""
        raise NotImplementedError("Requires GPU. Install diffusers and configure local Stable Diffusion weights.")

    def supported_backend(self) -> ImageGeneratorBackend:
        return ImageGeneratorBackend.STABLE_DIFFUSION_LOCAL

    def health_check(self) -> bool:
        """Verify CUDA availability and model checkpoint accessibility."""
        raise NotImplementedError("Requires GPU. Verify CUDA and Stable Diffusion checkpoint availability.")
