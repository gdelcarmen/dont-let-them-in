from __future__ import annotations

from datetime import datetime, timezone
from pathlib import Path
from typing import Any
from uuid import uuid4

from pydantic import BaseModel, ConfigDict, Field, field_validator, model_validator

from dlti_asset_pipeline.core.types import (
    AssetCategory,
    BackgroundPreference,
    CameraAngle,
    DevicePreference,
    ExperimentOutcome,
    ImageGeneratorBackend,
    MeshFormat,
    ReconstructorBackend,
    RegistryStatus,
)


def utc_now() -> datetime:
    return datetime.now(timezone.utc)


class BasePathModel(BaseModel):
    model_config = ConfigDict(use_enum_values=True, extra="forbid")


class CategoryBudgetConfig(BaseModel):
    model_config = ConfigDict(use_enum_values=True, extra="forbid")

    small_prop_tris: int = 2_000
    character_tris: int = 5_000
    texture_size: int = 1024


class ImageGeneratorConfig(BasePathModel):
    backend: ImageGeneratorBackend = ImageGeneratorBackend.MOCK
    api_key_env_var: str | None = "OPENAI_API_KEY"
    default_resolution: tuple[int, int] = (1024, 1024)
    default_quality: str = "medium"


class ReconstructorConfig(BasePathModel):
    backend: ReconstructorBackend = ReconstructorBackend.MOCK
    model_path: str | None = None
    device_preference: DevicePreference = DevicePreference.CPU
    output_format: MeshFormat = MeshFormat.GLB


class CategoryTarget(BasePathModel):
    triangle_budget: int
    texture_size: int
    size_bounds_meters: tuple[float, float] = (0.1, 8.0)
    target_dimensions_meters: tuple[float, float, float] = (1.0, 1.0, 1.0)


class PostProcessingConfig(BasePathModel):
    targets: dict[AssetCategory, CategoryTarget] = Field(default_factory=dict)
    run_uv_repacking: bool = False
    automatic_decimation: bool = True
    operation_sequence: list[str] = Field(
        default_factory=lambda: [
            "orientation_fix",
            "scale_normalization",
            "decimation",
            "texture_resize",
            "format_conversion",
        ]
    )


class StorageConfig(BasePathModel):
    base_output_directory: Path = Path("output")
    organize_by_category: bool = True
    image_format: str = "PNG"
    registry_directory: Path = Path("output/registry")
    experiment_log_path: Path = Path("output/experiment_log.jsonl")
    viewer_directory: Path = Path("output/viewers")


class QualityGateConfig(BasePathModel):
    minimum_uv_coverage_ratio: float = 0.45
    maximum_triangle_count: dict[AssetCategory, int] = Field(default_factory=dict)
    required_texture_dimensions: dict[AssetCategory, int] = Field(default_factory=dict)
    enforced: bool = False
    file_size_budget_bytes: int = 2 * 1024 * 1024


class UnityIntegrationConfig(BasePathModel):
    assets_directory: Path | None = None
    unity_mcp_available: bool = False


class PipelineConfig(BasePathModel):
    image_generator: ImageGeneratorConfig = Field(default_factory=ImageGeneratorConfig)
    reconstructor: ReconstructorConfig = Field(default_factory=ReconstructorConfig)
    postprocessing: PostProcessingConfig = Field(default_factory=PostProcessingConfig)
    storage: StorageConfig = Field(default_factory=StorageConfig)
    quality_gate: QualityGateConfig = Field(default_factory=QualityGateConfig)
    unity_integration: UnityIntegrationConfig = Field(default_factory=UnityIntegrationConfig)
    default_palette_keywords: dict[str, str] = Field(
        default_factory=lambda: {
            "warm": "warm cream tones, soft wood colors, warm lamplight yellow, cozy domestic",
            "cold": "cold blue-white glow, bioluminescent green accents, cold purple teal",
        }
    )

    @model_validator(mode="after")
    def populate_category_defaults(self) -> "PipelineConfig":
        default_targets = {
            AssetCategory.FURNITURE: CategoryTarget(
                triangle_budget=2_000,
                texture_size=512,
                target_dimensions_meters=(2.0, 1.2, 1.0),
            ),
            AssetCategory.DEFENSE: CategoryTarget(
                triangle_budget=2_500,
                texture_size=512,
                target_dimensions_meters=(1.2, 1.2, 1.2),
            ),
            AssetCategory.ENEMY: CategoryTarget(
                triangle_budget=5_000,
                texture_size=1024,
                target_dimensions_meters=(0.8, 1.8, 0.8),
            ),
            AssetCategory.ENVIRONMENTAL: CategoryTarget(
                triangle_budget=2_000,
                texture_size=512,
                target_dimensions_meters=(2.0, 2.5, 0.25),
            ),
            AssetCategory.VFX: CategoryTarget(
                triangle_budget=500,
                texture_size=512,
                target_dimensions_meters=(0.5, 0.5, 0.5),
            ),
        }
        for category, target in default_targets.items():
            self.postprocessing.targets.setdefault(category, target)
            self.quality_gate.maximum_triangle_count.setdefault(category, target.triangle_budget)
            self.quality_gate.required_texture_dimensions.setdefault(category, target.texture_size)
        return self


class AssetRequest(BasePathModel):
    description: str
    category: AssetCategory
    asset_name: str
    style_override: "StyleConfig | None" = None
    desired_triangle_budget_override: int | None = None
    request_id: str = Field(default_factory=lambda: f"req_{uuid4().hex}")


class StyleConfig(BasePathModel):
    base_prompt_template: str
    negative_prompt: str
    color_palette_keywords: list[str]
    art_style_keywords: list[str]
    background_preference: BackgroundPreference = BackgroundPreference.WHITE
    camera_angle_preference: CameraAngle = CameraAngle.THREE_QUARTER_TOPDOWN
    output_dimensions: tuple[int, int] = (1024, 1024)
    style_name: str = "dlti_base"

    @field_validator("color_palette_keywords", "art_style_keywords")
    @classmethod
    def non_empty_lists(cls, value: list[str]) -> list[str]:
        if not value:
            raise ValueError("list must not be empty")
        return value

    def render_prompt(self, asset_description: str) -> str:
        return self.base_prompt_template.format(
            asset_description=asset_description,
            palette_keywords=", ".join(self.color_palette_keywords),
            art_style_keywords=", ".join(self.art_style_keywords),
            camera_angle=self.camera_angle_preference,
            background=self.background_preference,
        )


class GeneratedImage(BasePathModel):
    file_path: Path
    prompt: str
    generator_backend: ImageGeneratorBackend
    generation_timestamp: datetime = Field(default_factory=utc_now)
    image_dimensions: tuple[int, int]
    image_hash: str


class ReconstructionResult(BasePathModel):
    file_path: Path
    source_image_path: Path
    reconstructor_backend: ReconstructorBackend
    triangle_count: int
    vertex_count: int
    textures_generated: bool
    bounding_box_dimensions: tuple[float, float, float]
    reconstruction_timestamp: datetime = Field(default_factory=utc_now)


class PostProcessedAsset(BasePathModel):
    file_path: Path
    original_reconstruction_path: Path
    operations_applied: list[str]
    final_triangle_count: int
    final_texture_dimensions: tuple[int, int] | None = None
    file_size_bytes: int


class QualityCheckResult(BasePathModel):
    name: str
    passed: bool
    measured_value: Any
    threshold: Any
    note: str
    weight: float = 1.0


class QualityReport(BasePathModel):
    asset_path: Path
    passed: bool
    checks: dict[str, QualityCheckResult]
    overall_quality_score: float
    warnings: list[str] = Field(default_factory=list)


class AssetRegistryEntry(BasePathModel):
    asset_id: str
    request: AssetRequest
    generated_image: GeneratedImage | None = None
    reconstruction: ReconstructionResult | None = None
    postprocessed_asset: PostProcessedAsset | None = None
    quality_report: QualityReport | None = None
    asset_category: AssetCategory
    human_tags: list[str] = Field(default_factory=list)
    creation_timestamp: datetime = Field(default_factory=utc_now)
    status: RegistryStatus = RegistryStatus.GENERATED


class ExperimentLogEntry(BasePathModel):
    timestamp: datetime = Field(default_factory=utc_now)
    pipeline_stage: str
    action_description: str
    tool_used: str
    outcome: ExperimentOutcome
    retry_count: int = 0
    human_intervention_required: bool = False
    intervention_reason: str | None = None
    notes: str | None = None


class HardwareRequirements(BasePathModel):
    minimum_vram_gb: float = 0.0
    cuda_required: bool = False
    notes: str = ""


class CatalogAssetDefinition(BasePathModel):
    asset_name: str
    category: AssetCategory
    description: str
    style_overrides: dict[str, Any] = Field(default_factory=dict)
    target_triangle_budget: int | None = None
    postprocessing_notes: list[str] = Field(default_factory=list)
    tags: list[str] = Field(default_factory=list)
