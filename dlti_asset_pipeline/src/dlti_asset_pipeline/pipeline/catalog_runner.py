from __future__ import annotations

from dlti_asset_pipeline.core.models import AssetRequest, PipelineConfig
from dlti_asset_pipeline.styles import load_catalog


def build_requests_from_catalog(config: PipelineConfig, category: str | None = None) -> list[AssetRequest]:
    catalog = load_catalog()
    requests = []
    for definition in catalog.values():
        if category and definition.category != category:
            continue
        requests.append(
            AssetRequest(
                description=definition.description,
                category=definition.category,
                asset_name=definition.asset_name,
                desired_triangle_budget_override=definition.target_triangle_budget,
            )
        )
    return requests
