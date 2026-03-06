from __future__ import annotations

from pathlib import Path

from dlti_asset_pipeline.core.models import (
    AssetRegistryEntry,
    AssetRequest,
    ExperimentLogEntry,
    PipelineConfig,
)
from dlti_asset_pipeline.core.types import ExperimentOutcome, RegistryStatus
from dlti_asset_pipeline.pipeline.backend_registry import (
    image_generator_registry,
    mesh_reconstructor_registry,
    register_default_backends,
)
from dlti_asset_pipeline.postprocessing import PostProcessingPipeline
from dlti_asset_pipeline.quality import aggregate_quality_report
from dlti_asset_pipeline.registry import AssetRegistry
from dlti_asset_pipeline.storage import StorageManager
from dlti_asset_pipeline.styles import StyleResolver
from dlti_asset_pipeline.viewer import ViewerGenerator


class AssetPipelineOrchestrator:
    def __init__(self, config: PipelineConfig) -> None:
        self.config = config
        self.storage = StorageManager(config)
        self.storage.ensure_directories()
        self.style_resolver = StyleResolver()
        register_default_backends()
        self.image_generator = image_generator_registry.create(config.image_generator.backend, config, self.storage)
        self.reconstructor = mesh_reconstructor_registry.create(config.reconstructor.backend, config, self.storage)
        self.postprocessing = PostProcessingPipeline(config)
        self.registry = AssetRegistry(config.storage.registry_directory)
        self.viewer = ViewerGenerator()

    async def log(self, entry: ExperimentLogEntry) -> None:
        await self.storage.append_jsonl(self.config.storage.experiment_log_path, entry.model_dump(mode="json"))

    async def run_request(self, request: AssetRequest, dry_run: bool = False) -> AssetRegistryEntry:
        style, catalog_entry = self.style_resolver.resolve(request)
        if dry_run:
            entry = AssetRegistryEntry(
                asset_id=request.request_id,
                request=request,
                asset_category=request.category,
                human_tags=(catalog_entry.tags if catalog_entry else []),
                status=RegistryStatus.GENERATED,
            )
            await self.log(
                ExperimentLogEntry(
                    pipeline_stage="dry_run",
                    action_description=f"Resolved style for {request.asset_name}",
                    tool_used="style_resolver",
                    outcome=ExperimentOutcome.SUCCESS,
                )
            )
            return entry

        generated_image = self.image_generator.generate(request, style)
        await self.log(
            ExperimentLogEntry(
                pipeline_stage="image_generation",
                action_description=f"Generated concept image for {request.asset_name}",
                tool_used=self.image_generator.supported_backend(),
                outcome=ExperimentOutcome.SUCCESS,
            )
        )
        reconstruction = self.reconstructor.reconstruct(generated_image)
        await self.log(
            ExperimentLogEntry(
                pipeline_stage="reconstruction",
                action_description=f"Reconstructed mesh for {request.asset_name}",
                tool_used=self.reconstructor.supported_backend(),
                outcome=ExperimentOutcome.SUCCESS,
            )
        )
        postprocessed = self.postprocessing.run(
            reconstruction,
            request.category,
            self.config.storage.base_output_directory / "postprocessed",
        )
        quality_report = aggregate_quality_report(
            postprocessed.file_path,
            request.category,
            self.config,
            texture_dimensions=postprocessed.final_texture_dimensions,
        )
        status = RegistryStatus.VALIDATED
        if quality_report.warnings:
            status = RegistryStatus.FLAGGED_FOR_REVIEW
        entry = AssetRegistryEntry(
            asset_id=request.request_id,
            request=request,
            generated_image=generated_image,
            reconstruction=reconstruction,
            postprocessed_asset=postprocessed,
            quality_report=quality_report,
            asset_category=request.category,
            human_tags=(catalog_entry.tags if catalog_entry else []),
            status=status,
        )
        self.registry.add(entry)
        await self.log(
            ExperimentLogEntry(
                pipeline_stage="quality_gate",
                action_description=f"Validated {request.asset_name}",
                tool_used="quality_checks",
                outcome=ExperimentOutcome.PARTIAL if quality_report.warnings else ExperimentOutcome.SUCCESS,
                notes="; ".join(quality_report.warnings) if quality_report.warnings else None,
            )
        )
        return entry

    async def run_batch(self, requests: list[AssetRequest], dry_run: bool = False) -> list[AssetRegistryEntry]:
        results = []
        for request in requests:
            results.append(await self.run_request(request, dry_run=dry_run))
        return results

    def build_viewer(self, asset_id: str) -> Path:
        entry = self.registry.get(asset_id)
        if not entry.postprocessed_asset:
            raise ValueError(f"Asset '{asset_id}' has no postprocessed mesh")
        output_path = self.config.storage.viewer_directory / f"{asset_id}.html"
        return self.viewer.generate(entry.postprocessed_asset.file_path, output_path)
