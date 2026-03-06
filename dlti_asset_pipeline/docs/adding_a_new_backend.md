# Adding a New Backend

## Interface Overview

Image backends subclass `dlti_asset_pipeline.generators.base.ImageGenerator`. Implement `generate`, `supported_backend`, and `health_check`.

Mesh reconstruction backends subclass `dlti_asset_pipeline.reconstructors.base.MeshReconstructor`. Implement `reconstruct`, `supported_backend`, `health_check`, and `hardware_requirements`.

## Add an Image Generator

1. Create a new module in `src/dlti_asset_pipeline/generators/`.
2. Subclass `ImageGenerator`.
3. Return a fully populated `GeneratedImage`.
4. Register the backend in the registry/orchestrator bootstrap.
5. Extend config handling if the backend needs new options.

## Add a Mesh Reconstructor

1. Create a new module in `src/dlti_asset_pipeline/reconstructors/`.
2. Subclass `MeshReconstructor`.
3. Return a fully populated `ReconstructionResult`.
4. Register the backend in the registry/orchestrator bootstrap.
5. Add hardware and model-path requirements to config docs.

## Test in Isolation

- Write a unit test for `health_check`.
- Feed a fixture `AssetRequest` or `GeneratedImage`.
- Verify that output files exist and models serialize to JSON.

## Switch the Pipeline

Change the configured backend in `default_config.yaml` or your environment-specific config file. No orchestrator restructuring should be required.
