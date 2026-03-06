import argparse
import bpy
from pathlib import Path


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def import_obj(path: Path):
    bpy.ops.wm.obj_import(filepath=str(path))


def clean_meshes():
    for obj in bpy.data.objects:
        if obj.type != "MESH":
            continue
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.mesh.remove_doubles()
        bpy.ops.mesh.delete_loose()
        bpy.ops.mesh.dissolve_degenerate()
        bpy.ops.mesh.normals_make_consistent(inside=False)
        bpy.ops.object.mode_set(mode="OBJECT")
        obj.select_set(False)


def export_lod(obj, ratio: float, out_path: Path):
    dup = obj.copy()
    dup.data = obj.data.copy()
    bpy.context.collection.objects.link(dup)
    bpy.context.view_layer.objects.active = dup
    dup.select_set(True)
    mod = dup.modifiers.new(name="Decimate", type="DECIMATE")
    mod.ratio = ratio
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.ops.export_scene.fbx(
        filepath=str(out_path),
        use_selection=True,
        axis_forward="-Z",
        axis_up="Y",
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        bake_space_transform=False,
        path_mode="COPY",
        embed_textures=False,
    )
    tri_count = sum(len(p.vertices) - 2 for p in dup.data.polygons if len(p.vertices) >= 3)
    bpy.data.objects.remove(dup, do_unlink=True)
    return tri_count


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--input", required=True)
    parser.add_argument("--output_dir", required=True)
    parser.add_argument("--lod0", type=int, default=5000)
    parser.add_argument("--lod1", type=int, default=2000)
    parser.add_argument("--lod2", type=int, default=500)
    args, _ = parser.parse_known_args()

    input_path = Path(args.input)
    output_dir = Path(args.output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)

    clear_scene()
    import_obj(input_path)
    clean_meshes()
    meshes = [obj for obj in bpy.data.objects if obj.type == "MESH"]
    if not meshes:
        raise RuntimeError("No mesh objects found after import")

    obj = meshes[0]
    bpy.context.view_layer.objects.active = obj
    base_tris = max(sum(len(p.vertices) - 2 for p in obj.data.polygons if len(p.vertices) >= 3), 1)

    lod0_ratio = min(1.0, args.lod0 / base_tris)
    lod1_ratio = min(1.0, args.lod1 / base_tris)
    lod2_ratio = min(1.0, args.lod2 / base_tris)

    tris = {
        "lod0": export_lod(obj, lod0_ratio, output_dir / f"{input_path.stem}_LOD0.fbx"),
        "lod1": export_lod(obj, lod1_ratio, output_dir / f"{input_path.stem}_LOD1.fbx"),
        "lod2": export_lod(obj, lod2_ratio, output_dir / f"{input_path.stem}_LOD2.fbx"),
    }
    print(tris)


if __name__ == "__main__":
    main()
