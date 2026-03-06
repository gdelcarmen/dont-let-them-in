from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw

from dlti_asset_pipeline.core.models import AssetRequest, GeneratedImage, PipelineConfig, StyleConfig
from dlti_asset_pipeline.core.types import AssetCategory, BackgroundPreference, ImageGeneratorBackend
from dlti_asset_pipeline.core.utils import sha256_file
from dlti_asset_pipeline.generators.base import ImageGenerator
from dlti_asset_pipeline.storage import StorageManager


class MockImageGenerator(ImageGenerator):
    def __init__(self, config: PipelineConfig, storage: StorageManager) -> None:
        self.config = config
        self.storage = storage

    def supported_backend(self) -> ImageGeneratorBackend:
        return ImageGeneratorBackend.MOCK

    def health_check(self) -> bool:
        return True

    def generate(self, request: AssetRequest, style: StyleConfig) -> GeneratedImage:
        width, height = style.output_dimensions
        background = self._background_color(request.category, style.background_preference)
        image = Image.new("RGBA", (width, height), background)
        draw = ImageDraw.Draw(image)
        message = request.asset_name.replace("_", " ")
        draw.rounded_rectangle((64, 64, width - 64, height - 64), radius=32, outline=(255, 255, 255, 255), width=8)
        draw.text((96, height // 2 - 16), message, fill=(255, 255, 255, 255))

        output_dir = self.storage.category_dir("images", request.category)
        image_path = output_dir / f"{request.request_id}.png"
        image.save(image_path)
        return GeneratedImage(
            file_path=image_path,
            prompt=style.render_prompt(request.description),
            generator_backend=self.supported_backend(),
            image_dimensions=(width, height),
            image_hash=sha256_file(image_path),
        )

    def _background_color(
        self, category: AssetCategory, preference: BackgroundPreference
    ) -> tuple[int, int, int, int]:
        if preference == BackgroundPreference.TRANSPARENT:
            return (0, 0, 0, 0)
        palette = {
            AssetCategory.FURNITURE: (209, 162, 104, 255),
            AssetCategory.DEFENSE: (184, 123, 73, 255),
            AssetCategory.ENEMY: (48, 88, 120, 255),
            AssetCategory.ENVIRONMENTAL: (158, 137, 117, 255),
            AssetCategory.VFX: (88, 195, 147, 255),
        }
        return palette[category]
