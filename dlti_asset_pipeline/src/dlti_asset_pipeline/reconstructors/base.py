from __future__ import annotations

from abc import ABC, abstractmethod

from dlti_asset_pipeline.core.models import GeneratedImage, HardwareRequirements, ReconstructionResult
from dlti_asset_pipeline.core.types import ReconstructorBackend


class MeshReconstructor(ABC):
    @abstractmethod
    def reconstruct(self, generated_image: GeneratedImage) -> ReconstructionResult:
        raise NotImplementedError

    @abstractmethod
    def supported_backend(self) -> ReconstructorBackend:
        raise NotImplementedError

    @abstractmethod
    def health_check(self) -> bool:
        raise NotImplementedError

    @abstractmethod
    def hardware_requirements(self) -> HardwareRequirements:
        raise NotImplementedError
