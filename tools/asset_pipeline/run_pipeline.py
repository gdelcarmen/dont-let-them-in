import argparse
import json
import os
import shutil
import subprocess
import time
from pathlib import Path

from PIL import Image
from rembg import remove


ROOT = Path(__file__).resolve().parent
MANIFEST = ROOT / "manifest.json"
PROMPTS = ROOT / "prompts.json"
OUT = ROOT / "output"
RAW = OUT / "raw"
RGBA = OUT / "rgba"
MESH = OUT / "mesh"
PROCESSED = OUT / "processed"
SPRITES = OUT / "sprites"
SPRITE_REF = SPRITES / "reference"
SPRITE_TOPDOWN = SPRITES / "topdown"
LOG = OUT / "dlti_generation_log.json"
TOPDOWN_SUFFIX = ", viewed from directly above at a slight angle, top-down 3/4 perspective, game sprite, visible from overhead"
REMBG_MODEL = Path.home() / ".u2net" / "u2net.onnx"


def load_manifest():
    data = json.loads(MANIFEST.read_text())
    prompts = json.loads(PROMPTS.read_text()) if PROMPTS.exists() else {}
    return data["assets"], prompts


def ensure_dirs():
    for path in [OUT, RAW, RGBA, MESH, PROCESSED, SPRITES, SPRITE_REF, SPRITE_TOPDOWN]:
        path.mkdir(parents=True, exist_ok=True)
    if not LOG.exists():
        LOG.write_text("[]")


def append_log(entry):
    data = json.loads(LOG.read_text())
    data.append(entry)
    LOG.write_text(json.dumps(data, indent=2))


def run(cmd, cwd=None, env=None):
    completed = subprocess.run(cmd, cwd=cwd, env=env, text=True, capture_output=True)
    return completed.returncode, completed.stdout, completed.stderr


def remove_background(input_path, output_path):
    if REMBG_MODEL.exists():
        try:
            output_path.write_bytes(remove(input_path.read_bytes()))
            return "rembg"
        except Exception:
            pass

    image = Image.open(input_path).convert("RGBA")
    pixels = image.load()
    width, height = image.size
    for x in range(width):
        for y in range(height):
            r, g, b, a = pixels[x, y]
            if r >= 245 and g >= 245 and b >= 245:
                pixels[x, y] = (r, g, b, 0)
    image.save(output_path)
    return "white_key"


def run_z_image(prompt, output_path, seed):
    return run(
        ["uv", "run", "z-image-mps", "-p", prompt, "--aspect", "1:1", "--seed", str(seed), "-o", str(output_path)],
        cwd=ROOT / "deps" / "z-image-mps",
    )


def has_3d_backend():
    if Path("/Applications/Meshfinity.app").exists():
        return True, "meshfinity"
    if shutil.which("docker"):
        return True, "docker"
    if os.environ.get("TRIPO_API_KEY"):
        return True, "tripo_api"
    return False, "2d_fallback"


def generate_sprite_set(asset_name, prompt, seed, log_entry):
    ref_raw = RAW / f"{asset_name}_reference.png"
    top_raw = RAW / f"{asset_name}_topdown.png"
    ref_rgba = SPRITE_REF / f"{asset_name}.png"
    top_rgba = SPRITE_TOPDOWN / f"{asset_name}.png"

    code, _, err = run_z_image(prompt, ref_raw, seed)
    if code != 0:
        log_entry["flagged_for_review"] = True
        log_entry["flag_reason"] = f"z-image reference failed: {err.strip()}"
        return False

    code, _, err = run_z_image(f"{prompt}{TOPDOWN_SUFFIX}", top_raw, seed)
    if code != 0:
        log_entry["flagged_for_review"] = True
        log_entry["flag_reason"] = f"z-image topdown failed: {err.strip()}"
        return False

    try:
        bg_method = remove_background(ref_raw, ref_rgba)
        bg_method_top = remove_background(top_raw, top_rgba)
    except Exception as exc:
        log_entry["flagged_for_review"] = True
        log_entry["flag_reason"] = f"rembg failed: {str(exc).strip()}"
        return False

    log_entry["quality_notes"] = f"Generated as 2D fallback sprite set; bg={bg_method},{bg_method_top}"
    log_entry["quality_gate_passed"] = ref_rgba.exists() and top_rgba.exists()
    if not log_entry["quality_gate_passed"]:
        log_entry["flagged_for_review"] = True
        log_entry["flag_reason"] = "2D fallback output missing"
    return log_entry["quality_gate_passed"]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--batch")
    parser.add_argument("--asset")
    parser.add_argument("--skip-existing", action="store_true")
    parser.add_argument("--mode", choices=["auto", "3d", "2d"], default="auto")
    args = parser.parse_args()

    ensure_dirs()
    assets, prompts = load_manifest()
    backend_available, backend_name = has_3d_backend()
    selected = assets
    if args.batch:
        selected = [a for a in selected if a["batch"] == str(args.batch)]
    if args.asset:
        selected = [a for a in selected if a["name"] == args.asset or a["id"] == args.asset]

    for item in selected:
        variants = int(item.get("variants", 1))
        for variant_index in range(variants):
            variant_suffix = f"_v{variant_index + 1}" if variants > 1 else ""
            asset_name = f"{item['name']}{variant_suffix}"
            raw_png = RAW / f"{asset_name}.png"
            rgba_png = RGBA / f"{asset_name}.png"
            mesh_dir = MESH / asset_name
            processed_dir = PROCESSED / asset_name
            start = time.time()
            attempt = 1
            log_entry = {
                "asset_id": item["id"],
                "asset_name": asset_name,
                "timestamp": time.strftime("%Y-%m-%dT%H:%M:%S"),
                "attempt": attempt,
                "z_image_seed": variant_index,
                "triposr_resolution": 256,
                "quality_gate_passed": False,
                "quality_notes": "",
                "lod0_tris": 0,
                "lod1_tris": 0,
                "lod2_tris": 0,
                "output_mode": "3d",
                "flagged_for_review": False,
                "flag_reason": "",
                "generation_time_seconds": 0
            }

            if args.skip_existing and raw_png.exists():
                continue

            prompt = prompts.get(item["name"], item["description"])
            wants_3d = item["mode"] == "3d" and args.mode != "2d"
            can_do_3d = wants_3d and args.mode == "3d"
            if wants_3d and args.mode == "auto":
                can_do_3d = backend_available

            if wants_3d and not can_do_3d:
                log_entry["output_mode"] = "2d_fallback"
                if args.mode == "3d":
                    log_entry["flagged_for_review"] = True
                    log_entry["flag_reason"] = f"No supported 3D backend available on this machine"
                    log_entry["generation_time_seconds"] = round(time.time() - start, 2)
                    append_log(log_entry)
                    continue

                generate_sprite_set(asset_name, prompt, variant_index, log_entry)
                log_entry["quality_notes"] = (log_entry["quality_notes"] + f"; backend={backend_name}").strip("; ")
                log_entry["generation_time_seconds"] = round(time.time() - start, 2)
                append_log(log_entry)
                continue

            code, _, err = run_z_image(prompt, raw_png, variant_index)
            if code != 0:
                log_entry["flagged_for_review"] = True
                log_entry["flag_reason"] = f"z-image failed: {err.strip()}"
                log_entry["generation_time_seconds"] = round(time.time() - start, 2)
                append_log(log_entry)
                continue

            try:
                bg_method = remove_background(raw_png, rgba_png)
                if bg_method != "rembg":
                    log_entry["quality_notes"] = f"background removal used {bg_method}"
            except Exception as exc:
                shutil.copy2(raw_png, rgba_png)
                log_entry["quality_notes"] = f"rembg failed; using raw png. {str(exc).strip()}"

            if item["mode"] == "3d" and can_do_3d:
                mesh_dir.mkdir(parents=True, exist_ok=True)
                log_entry["flagged_for_review"] = True
                log_entry["flag_reason"] = f"3D backend '{backend_name}' is not implemented in runner yet"
                log_entry["generation_time_seconds"] = round(time.time() - start, 2)
                append_log(log_entry)
                continue

                if code != 0:
                    log_entry["flagged_for_review"] = True
                    log_entry["flag_reason"] = f"TripoSR failed: {err.strip()}"
                    log_entry["generation_time_seconds"] = round(time.time() - start, 2)
                    append_log(log_entry)
                    continue

                obj_files = list(mesh_dir.glob("**/*.obj"))
                if not obj_files:
                    log_entry["flagged_for_review"] = True
                    log_entry["flag_reason"] = "TripoSR produced no OBJ"
                    log_entry["generation_time_seconds"] = round(time.time() - start, 2)
                    append_log(log_entry)
                    continue

                processed_dir.mkdir(parents=True, exist_ok=True)
                blender_bin = "/Applications/Blender.app/Contents/MacOS/Blender"
                code, out, err = run([
                    blender_bin, "--background", "--python", str(ROOT / "process_asset.py"),
                    "--", "--input", str(obj_files[0]), "--output_dir", str(processed_dir),
                    "--lod0", "5000", "--lod1", "2000", "--lod2", "500"
                ])
                if code != 0:
                    log_entry["flagged_for_review"] = True
                    log_entry["flag_reason"] = f"Blender failed: {err.strip()}"
                    log_entry["generation_time_seconds"] = round(time.time() - start, 2)
                    append_log(log_entry)
                    continue

                try:
                    tri_info = json.loads(out.strip().splitlines()[-1].replace("'", '"'))
                except Exception:
                    tri_info = {}
                log_entry["lod0_tris"] = tri_info.get("lod0", 0)
                log_entry["lod1_tris"] = tri_info.get("lod1", 0)
                log_entry["lod2_tris"] = tri_info.get("lod2", 0)
                log_entry["quality_gate_passed"] = bool(log_entry["lod0_tris"] and log_entry["lod2_tris"] and log_entry["lod0_tris"] <= 10000 and log_entry["lod2_tris"] <= 1000)
                if not log_entry["quality_gate_passed"]:
                    log_entry["flagged_for_review"] = True
                    log_entry["flag_reason"] = "LOD triangle gate failed"
            else:
                log_entry["quality_gate_passed"] = rgba_png.exists()
                if not log_entry["quality_gate_passed"]:
                    log_entry["flagged_for_review"] = True
                    log_entry["flag_reason"] = "2D output missing"

            log_entry["generation_time_seconds"] = round(time.time() - start, 2)
            append_log(log_entry)


if __name__ == "__main__":
    main()
