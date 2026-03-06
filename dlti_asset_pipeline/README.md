# DLTI Asset Pipeline

Standalone Python infrastructure for the image generation to 3D asset pipeline used by **Don't Let Them In**. The package sits beside the Unity project and produces Unity-friendly mock assets, quality reports, registry entries, and browser previews without installing any ML runtimes or model weights.

## Quick Start

```bash
cd /Users/gabrieldelcarmen/Desktop/dontletthemin/dlti_asset_pipeline
python3 -m venv .venv
source .venv/bin/activate
pip install -e .[dev]
dlti-pipeline --config src/dlti_asset_pipeline/resources/configs/default_config.yaml generate couch
```

## Architecture

```text
[Text Prompt + Style Config]
        ->
[Image Generator] -> styled 2D concept image
        ->
[3D Reconstructor] -> raw mesh + texture
        ->
[Post-Processor] -> normalization, decimation, conversion
        ->
[Quality Gate] -> automated checks + score
        ->
[Asset Registry] -> JSON catalog + experiment log
        ->
[Unity Import] -> configured Assets/GeneratedAssets target
```

## Commands

- `dlti-pipeline generate <asset_name>`
- `dlti-pipeline generate-all --category <category>`
- `dlti-pipeline status`
- `dlti-pipeline preview <asset_id>`
- `dlti-pipeline gallery --category <category>`
- `dlti-pipeline validate <path_to_glb>`
- `dlti-pipeline dry-run <asset_name>`

## Documentation

- `docs/adding_a_new_backend.md`
- `docs/gpu_environment_setup.md`
- `docs/unity_integration.md`

## Current Status

- Image generators:
  - `mock`: implemented
  - `openai`, `stable_diffusion_local`, `comfyui`: stubbed
- Mesh reconstructors:
  - `mock`: implemented
  - `triposr`, `instantmesh`, `triposf`, `tripo_api`: stubbed
