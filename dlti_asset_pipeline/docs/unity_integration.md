# Unity Integration Guide

## Direct File Drop

Configure `unity_integration.assets_directory` to Unity's `Assets/GeneratedAssets` path. The pipeline can then emit generated meshes and companion files directly into Unity's asset tree for auto-import.

## Unity MCP

The current package does not call Unity MCP automatically, but the storage and registry layers are structured so an import step can be added after registry creation. A future integration can invoke `manage_asset` after successful generation.

## Materials

Generated GLB files carry simple default materials. For URP or HDRP, plan on converting them to project-standard materials and wiring emissive channels for alien glow elements.

## Prefabs

Wrap each imported asset in a prefab with a consistent component layout:

- `MeshFilter`
- `MeshRenderer`
- `Collider`

This keeps runtime systems decoupled from raw imported meshes.
