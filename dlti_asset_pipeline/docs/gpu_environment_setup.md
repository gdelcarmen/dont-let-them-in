# GPU Environment Setup

## Requirements

- Python 3.10+
- NVIDIA driver and CUDA toolkit matching the chosen PyTorch build
- At least 6GB VRAM for local image generation
- 8GB VRAM for TripoSR, 12GB for InstantMesh or TripoSF

## Install Steps

1. Create and activate a virtual environment.
2. Install PyTorch with the CUDA wheel matching the machine.
3. Install this package with `pip install -e .[dev]`.
4. Install only the backend-specific dependencies you plan to use.

## Configure Backends

- Set backend names in the config file.
- Point `model_path` to local checkpoints.
- Set any API keys via environment variables.

## Smoke Test

Run a single asset generation with a known catalog entry and inspect the registry, experiment log, and preview output.

## Troubleshooting

- CUDA OOM: reduce resolution or batch size, or switch to a lighter backend.
- Wrong PyTorch/CUDA version: reinstall the matching wheel.
- Missing model path: verify the configured checkpoint directory exists and is readable.
