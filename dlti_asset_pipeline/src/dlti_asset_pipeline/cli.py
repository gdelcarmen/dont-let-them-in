from __future__ import annotations

import argparse
import asyncio
from pathlib import Path

from dlti_asset_pipeline.core import AssetRequest, load_config
from dlti_asset_pipeline.pipeline import AssetPipelineOrchestrator, build_requests_from_catalog


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(prog="dlti-pipeline")
    parser.add_argument("--config", default="src/dlti_asset_pipeline/resources/configs/default_config.yaml")
    subparsers = parser.add_subparsers(dest="command", required=True)

    generate = subparsers.add_parser("generate")
    generate.add_argument("asset_name")

    generate_all = subparsers.add_parser("generate-all")
    generate_all.add_argument("--category")

    status = subparsers.add_parser("status")

    preview = subparsers.add_parser("preview")
    preview.add_argument("asset_id")

    gallery = subparsers.add_parser("gallery")
    gallery.add_argument("--category")

    validate = subparsers.add_parser("validate")
    validate.add_argument("path_to_glb")

    dry_run = subparsers.add_parser("dry-run")
    dry_run.add_argument("asset_name")
    return parser


async def _run(args: argparse.Namespace) -> int:
    config = load_config(Path(args.config))
    orchestrator = AssetPipelineOrchestrator(config)
    catalog_requests = {request.asset_name: request for request in build_requests_from_catalog(config)}

    if args.command == "generate":
        entry = await orchestrator.run_request(catalog_requests[args.asset_name])
        print(f"{entry.request.asset_name}: quality={entry.quality_report.overall_quality_score if entry.quality_report else 'n/a'} status={entry.status}")
        return 0
    if args.command == "generate-all":
        requests = build_requests_from_catalog(config, category=args.category)
        results = await orchestrator.run_batch(requests)
        print(f"Generated {len(results)} assets")
        return 0
    if args.command == "status":
        print(orchestrator.registry.export_summary())
        return 0
    if args.command == "preview":
        print(orchestrator.build_viewer(args.asset_id))
        return 0
    if args.command == "gallery":
        entries = orchestrator.registry.query(category=args.category) if args.category else orchestrator.registry.all_entries()
        glbs = [entry.postprocessed_asset.file_path for entry in entries if entry.postprocessed_asset]
        html = orchestrator.viewer.generate_gallery(glbs, config.storage.viewer_directory / "gallery.html")
        print(html)
        return 0
    if args.command == "validate":
        from dlti_asset_pipeline.quality.checks import aggregate_quality_report
        from dlti_asset_pipeline.core.types import AssetCategory

        report = aggregate_quality_report(Path(args.path_to_glb), AssetCategory.FURNITURE, config)
        print(report.model_dump(mode="json"))
        return 0
    if args.command == "dry-run":
        entry = await orchestrator.run_request(catalog_requests[args.asset_name], dry_run=True)
        print(entry.model_dump(mode="json"))
        return 0
    return 1


def main() -> None:
    parser = build_parser()
    args = parser.parse_args()
    raise SystemExit(asyncio.run(_run(args)))
