from __future__ import annotations

from collections.abc import Callable
from typing import Any

from dlti_asset_pipeline.generators.comfyui_backend import ComfyUIImageGenerator
from dlti_asset_pipeline.generators.mock import MockImageGenerator
from dlti_asset_pipeline.generators.openai_backend import OpenAIImageGenerator
from dlti_asset_pipeline.generators.stable_diffusion_backend import StableDiffusionLocalImageGenerator
from dlti_asset_pipeline.reconstructors.instantmesh_backend import InstantMeshReconstructor
from dlti_asset_pipeline.reconstructors.mock import MockMeshReconstructor
from dlti_asset_pipeline.reconstructors.tripo_api_backend import TripoAPIReconstructor
from dlti_asset_pipeline.reconstructors.triposf_backend import TripoSFReconstructor
from dlti_asset_pipeline.reconstructors.triposr_backend import TripoSRMeshReconstructor


class BackendRegistry:
    def __init__(self) -> None:
        self._factories: dict[str, Callable[..., Any]] = {}

    def register(self, name: str, factory: Callable[..., Any]) -> None:
        self._factories[name] = factory

    def create(self, name: str, *args: Any, **kwargs: Any) -> Any:
        if name not in self._factories:
            raise KeyError(f"Backend '{name}' is not registered")
        return self._factories[name](*args, **kwargs)

    def available(self) -> list[str]:
        return sorted(self._factories)


image_generator_registry = BackendRegistry()
mesh_reconstructor_registry = BackendRegistry()


def register_default_backends() -> None:
    if not image_generator_registry.available():
        image_generator_registry.register("mock", MockImageGenerator)
        image_generator_registry.register("openai", OpenAIImageGenerator)
        image_generator_registry.register("stable_diffusion_local", StableDiffusionLocalImageGenerator)
        image_generator_registry.register("comfyui", ComfyUIImageGenerator)
    if not mesh_reconstructor_registry.available():
        mesh_reconstructor_registry.register("mock", MockMeshReconstructor)
        mesh_reconstructor_registry.register("triposr", TripoSRMeshReconstructor)
        mesh_reconstructor_registry.register("instantmesh", InstantMeshReconstructor)
        mesh_reconstructor_registry.register("triposf", TripoSFReconstructor)
        mesh_reconstructor_registry.register("tripo_api", TripoAPIReconstructor)
