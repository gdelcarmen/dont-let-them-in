from __future__ import annotations

from enum import Enum


class AssetCategory(str, Enum):
    FURNITURE = "furniture"
    DEFENSE = "defense"
    ENEMY = "enemy"
    ENVIRONMENTAL = "environmental"
    VFX = "vfx"


class BackgroundPreference(str, Enum):
    WHITE = "white"
    TRANSPARENT = "transparent"
    CONTEXTUAL = "contextual"


class CameraAngle(str, Enum):
    THREE_QUARTER_FRONT = "3/4 front view, slightly above eye level"
    THREE_QUARTER_TOPDOWN = "3/4 top-down view"
    TOP_DOWN = "directly top-down"
    FRONT_FACING = "front-facing straight on"


class ImageGeneratorBackend(str, Enum):
    MOCK = "mock"
    OPENAI = "openai"
    STABLE_DIFFUSION_LOCAL = "stable_diffusion_local"
    COMFYUI = "comfyui"


class ReconstructorBackend(str, Enum):
    MOCK = "mock"
    TRIPOSR = "triposr"
    INSTANTMESH = "instantmesh"
    TRIPOSF = "triposf"
    SHAP_E = "shap_e"
    TRIPO_API = "tripo_api"


class DevicePreference(str, Enum):
    CUDA = "cuda"
    CPU = "cpu"
    MPS = "mps"


class MeshFormat(str, Enum):
    GLB = "glb"
    GLTF = "gltf"
    FBX = "fbx"
    OBJ = "obj"


class RegistryStatus(str, Enum):
    GENERATED = "generated"
    VALIDATED = "validated"
    IMPORTED_TO_UNITY = "imported_to_unity"
    FLAGGED_FOR_REVIEW = "flagged_for_review"


class ExperimentOutcome(str, Enum):
    SUCCESS = "success"
    PARTIAL = "partial"
    FAILURE = "failure"
