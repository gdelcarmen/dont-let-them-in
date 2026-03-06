from __future__ import annotations

from pathlib import Path

import numpy as np
import trimesh

from dlti_asset_pipeline.core.models import PipelineConfig, QualityCheckResult, QualityReport
from dlti_asset_pipeline.core.types import AssetCategory


def _load_mesh(path: Path) -> trimesh.Trimesh:
    loaded = trimesh.load(path, force="mesh")
    if isinstance(loaded, trimesh.Scene):
        return loaded.dump(concatenate=True)
    return loaded


def triangle_count_check(path: Path, max_triangles: int) -> QualityCheckResult:
    mesh = _load_mesh(path)
    tri_count = len(mesh.faces)
    return QualityCheckResult(
        name="triangle_count",
        passed=tri_count <= max_triangles,
        measured_value=tri_count,
        threshold=max_triangles,
        note=f"Triangle count is {tri_count} against budget {max_triangles}.",
        weight=2.0,
    )


def texture_dimension_check(texture_dimensions: tuple[int, int] | None, max_dimension: int) -> QualityCheckResult:
    measured = texture_dimensions or (0, 0)
    passed = max(measured) <= max_dimension
    return QualityCheckResult(
        name="texture_dimension",
        passed=passed,
        measured_value=measured,
        threshold=max_dimension,
        note=f"Texture dimensions {measured} against max {max_dimension}.",
        weight=1.5,
    )


def bounding_box_check(path: Path, size_bounds: tuple[float, float]) -> QualityCheckResult:
    mesh = _load_mesh(path)
    extents = mesh.bounding_box.extents
    low, high = size_bounds
    within = bool(np.all(extents >= low) and np.all(extents <= high))
    return QualityCheckResult(
        name="bounding_box",
        passed=within,
        measured_value=tuple(float(value) for value in extents),
        threshold=size_bounds,
        note=f"Bounding box extents {tuple(float(v) for v in extents)} expected within {size_bounds}.",
        weight=1.0,
    )


def uv_coverage_check(path: Path, minimum_ratio: float) -> QualityCheckResult:
    mesh = _load_mesh(path)
    uv = getattr(getattr(mesh.visual, "uv", None), "copy", lambda: None)()
    coverage = 0.0
    note = "Mesh has no UVs."
    if uv is not None and len(uv) > 0:
        mins = np.min(uv, axis=0)
        maxs = np.max(uv, axis=0)
        coverage = float(np.clip(np.prod(maxs - mins), 0.0, 1.0))
        note = f"UV coverage approximated as {coverage:.3f}."
    return QualityCheckResult(
        name="uv_coverage",
        passed=coverage >= minimum_ratio,
        measured_value=round(coverage, 3),
        threshold=minimum_ratio,
        note=note,
        weight=1.0,
    )


def watertight_check(path: Path) -> QualityCheckResult:
    mesh = _load_mesh(path)
    return QualityCheckResult(
        name="watertight",
        passed=bool(mesh.is_watertight),
        measured_value=bool(mesh.is_watertight),
        threshold=True,
        note="Watertight meshes are preferred but not always required for runtime assets.",
        weight=0.5,
    )


def file_size_check(path: Path, max_bytes: int) -> QualityCheckResult:
    size = path.stat().st_size
    return QualityCheckResult(
        name="file_size",
        passed=size <= max_bytes,
        measured_value=size,
        threshold=max_bytes,
        note=f"File size is {size} bytes against budget {max_bytes}.",
        weight=1.0,
    )


def aggregate_quality_report(
    path: Path,
    category: AssetCategory,
    config: PipelineConfig,
    texture_dimensions: tuple[int, int] | None = None,
) -> QualityReport:
    target = config.postprocessing.targets[category]
    checks = {
        "triangle_count": triangle_count_check(path, config.quality_gate.maximum_triangle_count[category]),
        "texture_dimension": texture_dimension_check(texture_dimensions, config.quality_gate.required_texture_dimensions[category]),
        "bounding_box": bounding_box_check(path, target.size_bounds_meters),
        "uv_coverage": uv_coverage_check(path, config.quality_gate.minimum_uv_coverage_ratio),
        "watertight": watertight_check(path),
        "file_size": file_size_check(path, config.quality_gate.file_size_budget_bytes),
    }
    total_weight = sum(check.weight for check in checks.values())
    score = sum((1.0 if check.passed else 0.0) * check.weight for check in checks.values()) / total_weight
    warnings = [check.note for check in checks.values() if not check.passed]
    critical = ["triangle_count", "file_size", "bounding_box"]
    passed = all(checks[name].passed for name in critical) if config.quality_gate.enforced else True
    return QualityReport(
        asset_path=path,
        passed=passed,
        checks=checks,
        overall_quality_score=round(score, 3),
        warnings=warnings,
    )
