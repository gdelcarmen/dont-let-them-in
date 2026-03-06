from __future__ import annotations

import json
from pathlib import Path

from dlti_asset_pipeline.core.models import AssetRegistryEntry
from dlti_asset_pipeline.core.types import AssetCategory, RegistryStatus


class AssetRegistry:
    def __init__(self, directory: Path) -> None:
        self.directory = directory
        self.entries_dir = directory / "entries"
        self.index_path = directory / "index.json"
        self.entries_dir.mkdir(parents=True, exist_ok=True)
        if not self.index_path.exists():
            self.index_path.write_text("{}")

    def _load_index(self) -> dict[str, str]:
        return json.loads(self.index_path.read_text())

    def _write_index(self, index: dict[str, str]) -> None:
        self.index_path.write_text(json.dumps(index, indent=2))

    def add(self, entry: AssetRegistryEntry) -> Path:
        path = self.entries_dir / f"{entry.asset_id}.json"
        path.write_text(entry.model_dump_json(indent=2))
        index = self._load_index()
        index[entry.asset_id] = str(path)
        self._write_index(index)
        return path

    def get(self, asset_id: str) -> AssetRegistryEntry:
        path = Path(self._load_index()[asset_id])
        return AssetRegistryEntry.model_validate_json(path.read_text())

    def all_entries(self) -> list[AssetRegistryEntry]:
        index = self._load_index()
        return [AssetRegistryEntry.model_validate_json(Path(path).read_text()) for path in index.values()]

    def query(
        self,
        category: AssetCategory | None = None,
        status: RegistryStatus | None = None,
        min_quality_score: float | None = None,
        max_quality_score: float | None = None,
    ) -> list[AssetRegistryEntry]:
        entries = self.all_entries()
        results = []
        for entry in entries:
            if category and entry.asset_category != category:
                continue
            if status and entry.status != status:
                continue
            if entry.quality_report:
                score = entry.quality_report.overall_quality_score
                if min_quality_score is not None and score < min_quality_score:
                    continue
                if max_quality_score is not None and score > max_quality_score:
                    continue
            results.append(entry)
        return results

    def update_status(self, asset_id: str, status: RegistryStatus) -> AssetRegistryEntry:
        entry = self.get(asset_id)
        entry.status = status
        self.add(entry)
        return entry

    def export_summary(self) -> dict:
        entries = self.all_entries()
        category_counts: dict[str, int] = {}
        scores: list[float] = []
        flagged = 0
        for entry in entries:
            category_counts[entry.asset_category] = category_counts.get(entry.asset_category, 0) + 1
            if entry.quality_report:
                scores.append(entry.quality_report.overall_quality_score)
            if entry.status == RegistryStatus.FLAGGED_FOR_REVIEW:
                flagged += 1
        return {
            "total_assets": len(entries),
            "assets_per_category": category_counts,
            "average_quality_score": round(sum(scores) / len(scores), 3) if scores else 0.0,
            "flagged_for_review": flagged,
        }
