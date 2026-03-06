from __future__ import annotations

from abc import ABC, abstractmethod

from dlti_asset_pipeline.core.models import AssetRequest, GeneratedImage, StyleConfig
from dlti_asset_pipeline.core.types import ImageGeneratorBackend


class ImageGenerator(ABC):
    @abstractmethod
    def generate(self, request: AssetRequest, style: StyleConfig) -> GeneratedImage:
        raise NotImplementedError

    @abstractmethod
    def supported_backend(self) -> ImageGeneratorBackend:
        raise NotImplementedError

    @abstractmethod
    def health_check(self) -> bool:
        raise NotImplementedError
