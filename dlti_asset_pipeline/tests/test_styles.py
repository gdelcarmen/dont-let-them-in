from __future__ import annotations

from dlti_asset_pipeline.core import AssetCategory, AssetRequest, StyleConfig
from dlti_asset_pipeline.styles import StyleResolver


def test_style_resolver_merges_request_override():
    resolver = StyleResolver()
    request = AssetRequest(
        asset_name="couch",
        description="a couch",
        category=AssetCategory.FURNITURE,
        style_override=StyleConfig(
            base_prompt_template="{asset_description}",
            negative_prompt="none",
            color_palette_keywords=["warm"],
            art_style_keywords=["chunky"],
            output_dimensions=(1024, 1536),
        ),
    )
    style, catalog_entry = resolver.resolve(request)
    assert catalog_entry is not None
    assert style.output_dimensions == (1024, 1536)
