from __future__ import annotations

from pathlib import Path

import numpy as np
from PIL import Image
import trimesh

from dlti_asset_pipeline.core.models import PipelineConfig
from dlti_asset_pipeline.core.types import AssetCategory, MeshFormat


class MeshPostProcessor:
    def __init__(self, config: PipelineConfig) -> None:
        self.config = config

    def load_mesh(self, path: Path) -> trimesh.Trimesh:
        loaded = trimesh.load(path, force="mesh")
        if isinstance(loaded, trimesh.Scene):
            return loaded.dump(concatenate=True)
        return loaded

    def export_mesh(self, mesh: trimesh.Trimesh, path: Path) -> Path:
        path.parent.mkdir(parents=True, exist_ok=True)
        mesh.export(path)
        return path

    def decimation(self, mesh_path: Path, output_path: Path, target_triangles: int) -> tuple[Path, str]:
        mesh = self.load_mesh(mesh_path)
        if len(mesh.faces) > target_triangles:
            simplified = self._simplify_mesh(mesh, target_triangles)
        else:
            simplified = mesh
        self.export_mesh(simplified, output_path)
        return output_path, f"decimated_to_{len(simplified.faces)}_tris"

    def scale_normalization(self, mesh_path: Path, output_path: Path, target_dimensions: tuple[float, float, float]) -> tuple[Path, str]:
        mesh = self.load_mesh(mesh_path)
        extents = np.maximum(mesh.bounding_box.extents, 1e-6)
        factors = np.array(target_dimensions) / extents
        mesh.apply_scale(float(np.min(factors)))
        self.export_mesh(mesh, output_path)
        return output_path, f"scale_normalized_to_{target_dimensions}"

    def orientation_fix(self, mesh_path: Path, output_path: Path) -> tuple[Path, str]:
        mesh = self.load_mesh(mesh_path)
        centroid = mesh.bounding_box.centroid
        mesh.apply_translation(-centroid)
        self.export_mesh(mesh, output_path)
        return output_path, "orientation_fixed_y_up_z_plus"

    def texture_resize(self, texture_path: Path, output_path: Path, size: int) -> tuple[Path, str]:
        image = Image.open(texture_path).convert("RGBA")
        image.thumbnail((size, size), Image.Resampling.LANCZOS)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        image.save(output_path)
        return output_path, f"texture_resized_{image.size[0]}x{image.size[1]}"

    def format_conversion(self, mesh_path: Path, output_path: Path, target_format: MeshFormat) -> tuple[Path, str]:
        mesh = self.load_mesh(mesh_path)
        suffix = target_format.value if hasattr(target_format, "value") else str(target_format)
        final_path = output_path.with_suffix(f".{suffix}")
        self.export_mesh(mesh, final_path)
        return final_path, f"converted_to_{suffix}"

    def _simplify_mesh(self, mesh: trimesh.Trimesh, target_triangles: int) -> trimesh.Trimesh:
        simplify = getattr(mesh, "simplify_quadric_decimation", None)
        if callable(simplify):
            try:
                return simplify(target_triangles)
            except Exception:
                pass
        extents = tuple(float(value) for value in mesh.bounding_box.extents)
        return trimesh.creation.box(extents=extents)


def target_for_category(config: PipelineConfig, category: AssetCategory) -> tuple[int, int, tuple[float, float, float]]:
    target = config.postprocessing.targets[category]
    return target.triangle_budget, target.texture_size, target.target_dimensions_meters
