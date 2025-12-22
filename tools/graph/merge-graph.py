#!/usr/bin/env python3
"""
Graph Merger Tool

Combines backend-graph.json and frontend-graph.json into a unified graph-baseline.json,
detecting inconsistencies between backend and frontend layers.

Usage:
    python3 merge-graph.py [--output path/to/output.json]

Exit codes:
    0: Success
    1: Merge failed (invalid input, I/O error, etc.)
"""

import json
import sys
import os
from pathlib import Path
from datetime import datetime, timezone
from typing import Dict, List, Set, Tuple, Optional


class GraphMerger:
    """Merges backend and frontend graphs and detects inconsistencies."""

    def __init__(self, backend_path: str, frontend_path: str):
        """Initialize merger with paths to input graphs."""
        self.backend_path = backend_path
        self.frontend_path = frontend_path
        self.backend_graph: Dict = {}
        self.frontend_graph: Dict = {}
        self.merged_graph: Dict = {}
        self.inconsistencies: Dict[str, List[str]] = {
            "dtos_without_types": [],
            "types_without_dtos": [],
            "property_mismatches": [],
            "missing_endpoints": [],
        }

    def load_graphs(self) -> bool:
        """Load backend and frontend graphs from JSON files."""
        try:
            with open(self.backend_path, "r") as f:
                self.backend_graph = json.load(f)
            with open(self.frontend_path, "r") as f:
                self.frontend_graph = json.load(f)
            return True
        except FileNotFoundError as e:
            print(f"Error: File not found: {e}", file=sys.stderr)
            return False
        except json.JSONDecodeError as e:
            print(f"Error: Invalid JSON: {e}", file=sys.stderr)
            return False

    def _normalize_dto_name(self, dto_name: str) -> str:
        """Normalize DTO name for matching with frontend types."""
        # Remove 'Dto' suffix for matching
        if dto_name.endswith("Dto"):
            return dto_name[:-3]
        return dto_name

    def _normalize_type_name(self, type_name: str) -> str:
        """Normalize TypeScript type name for matching with backend DTOs."""
        return type_name

    def _find_matching_type(self, dto_name: str) -> Optional[str]:
        """Find matching frontend type for a given DTO."""
        normalized_dto = self._normalize_dto_name(dto_name)
        types = self.frontend_graph.get("types", {})

        # Exact match
        if dto_name in types:
            return dto_name

        # Match by normalized name
        for type_name in types:
            if self._normalize_type_name(type_name).lower() == normalized_dto.lower():
                return type_name

        return None

    def _find_matching_dto(self, type_name: str) -> Optional[str]:
        """Find matching backend DTO for a given frontend type."""
        dtos = self.backend_graph.get("dtos", {})

        # Exact match
        if type_name in dtos:
            return type_name

        # Try matching with Dto suffix
        dto_name = type_name + "Dto"
        if dto_name in dtos:
            return dto_name

        # Match by normalized name
        for dto in dtos:
            if (
                self._normalize_dto_name(dto).lower()
                == type_name.lower()
            ):
                return dto

        return None

    def _compare_properties(
        self, dto_name: str, type_name: str
    ) -> List[str]:
        """Compare properties between DTO and type."""
        mismatches = []
        dtos = self.backend_graph.get("dtos", {})
        types = self.frontend_graph.get("types", {})

        if dto_name not in dtos or type_name not in types:
            return mismatches

        dto_props = dtos[dto_name].get("properties", {})
        type_props = types[type_name].get("properties", {})

        # Check for missing properties in type
        for prop_name in dto_props:
            # Convert to camelCase for comparison (C# PascalCase vs TS camelCase)
            ts_prop_name = prop_name[0].lower() + prop_name[1:] if prop_name else ""
            if ts_prop_name not in type_props and prop_name not in type_props:
                mismatches.append(
                    f"{dto_name}.{prop_name} missing in {type_name}"
                )

        return mismatches

    def _detect_endpoint_coverage(self) -> List[str]:
        """Detect API endpoints without corresponding frontend functions."""
        missing = []
        controllers = self.backend_graph.get("controllers", {})
        api_functions = self.frontend_graph.get("api_functions", {})

        for controller_name, controller in controllers.items():
            for endpoint in controller.get("endpoints", []):
                endpoint_method = endpoint.get("method", "").upper()
                endpoint_route = endpoint.get("route", "")
                base_route = controller.get("route", "")

                # Build full route
                full_route = f"{base_route}/{endpoint_route}".replace("//", "/")

                # Check if endpoint is covered by api_functions
                endpoint_covered = False
                for func_name, func in api_functions.items():
                    if func.get("method", "").upper() == endpoint_method:
                        func_endpoint = func.get("endpoint", "")
                        # Normalize for comparison (IdKey handling)
                        if self._routes_match(full_route, func_endpoint):
                            endpoint_covered = True
                            break

                if not endpoint_covered:
                    missing.append(
                        f"{endpoint_method} {full_route} (no frontend api_function)"
                    )

        return missing

    @staticmethod
    def _routes_match(backend_route: str, frontend_route: str) -> bool:
        """Check if backend and frontend routes match."""
        # Normalize: {idKey} in backend == {idKey} in frontend
        backend_normalized = backend_route.replace("//", "/").strip("/")
        frontend_normalized = frontend_route.replace("//", "/").strip("/")

        # Exact match
        if backend_normalized == frontend_normalized:
            return True

        # Handle parameter variations
        import re

        # Convert routes to regex patterns
        def route_to_pattern(route: str) -> str:
            # Match {idKey}, {id}, {param}, etc.
            return re.sub(r"\{[^}]+\}", "{param}", route)

        return route_to_pattern(backend_normalized) == route_to_pattern(
            frontend_normalized
        )

    def detect_inconsistencies(self) -> None:
        """Detect inconsistencies between backend and frontend graphs."""
        dtos = self.backend_graph.get("dtos", {})
        types = self.frontend_graph.get("types", {})

        # Map DTOs to types
        dto_to_type: Dict[str, Optional[str]] = {}
        for dto_name in dtos:
            matching_type = self._find_matching_type(dto_name)
            dto_to_type[dto_name] = matching_type
            if not matching_type:
                self.inconsistencies["dtos_without_types"].append(dto_name)

        # Find types without DTOs
        for type_name in types:
            if not self._find_matching_dto(type_name):
                self.inconsistencies["types_without_dtos"].append(type_name)

        # Compare properties
        for dto_name, type_name in dto_to_type.items():
            if type_name:
                mismatches = self._compare_properties(dto_name, type_name)
                self.inconsistencies["property_mismatches"].extend(mismatches)

        # Check endpoint coverage
        missing_endpoints = self._detect_endpoint_coverage()
        self.inconsistencies["missing_endpoints"].extend(missing_endpoints)

    def merge(self) -> bool:
        """Merge backend and frontend graphs into unified baseline."""
        # Copy backend sections
        self.merged_graph["version"] = "1.0"
        self.merged_graph["generated_at"] = datetime.now(
            timezone.utc
        ).isoformat()

        # Copy all backend sections
        self.merged_graph["entities"] = self.backend_graph.get("entities", {})
        self.merged_graph["dtos"] = self.backend_graph.get("dtos", {})
        self.merged_graph["services"] = self.backend_graph.get(
            "services", {}
        )
        self.merged_graph["controllers"] = self.backend_graph.get(
            "controllers", {}
        )

        # Copy all frontend sections
        self.merged_graph["hooks"] = self.frontend_graph.get("hooks", {})
        self.merged_graph["api_functions"] = self.frontend_graph.get(
            "api_functions", {}
        )
        self.merged_graph["components"] = self.frontend_graph.get(
            "components", {}
        )

        # Copy and extend edges from both graphs
        merged_edges = []
        merged_edges.extend(self.backend_graph.get("edges", []))
        merged_edges.extend(self.frontend_graph.get("edges", []))
        self.merged_graph["edges"] = merged_edges

        # Build cross-layer mappings
        cross_layer_mappings: Dict[str, str] = {}
        for dto_name in self.backend_graph.get("dtos", {}):
            matching_type = self._find_matching_type(dto_name)
            if matching_type:
                cross_layer_mappings[f"dto:{dto_name}"] = (
                    f"type:{matching_type}"
                )

        self.merged_graph["cross_layer_mappings"] = cross_layer_mappings

        # Calculate statistics
        self.merged_graph["stats"] = {
            "entities": len(self.backend_graph.get("entities", {})),
            "dtos": len(self.backend_graph.get("dtos", {})),
            "services": len(self.backend_graph.get("services", {})),
            "controllers": len(self.backend_graph.get("controllers", {})),
            "types": len(self.frontend_graph.get("types", {})),
            "api_functions": len(
                self.frontend_graph.get("api_functions", {})
            ),
            "hooks": len(self.frontend_graph.get("hooks", {})),
            "components": len(self.frontend_graph.get("components", {})),
            "total_edges": len(self.merged_graph["edges"]),
        }

        return True

    def print_inconsistency_report(self) -> None:
        """Print inconsistency report to stdout."""
        print("\n" + "=" * 60)
        print("Cross-Layer Inconsistency Report")
        print("=" * 60 + "\n")

        # DTOs without frontend types
        if self.inconsistencies["dtos_without_types"]:
            print(
                f"DTOs without frontend types "
                f"({len(self.inconsistencies['dtos_without_types'])}):"
            )
            for dto in self.inconsistencies["dtos_without_types"]:
                print(f"  - {dto}")
            print()

        # Types without backend DTOs
        if self.inconsistencies["types_without_dtos"]:
            print(
                f"Frontend types without backend DTOs "
                f"({len(self.inconsistencies['types_without_dtos'])}):"
            )
            for type_name in self.inconsistencies["types_without_dtos"]:
                print(f"  - {type_name}")
            print()

        # Property mismatches
        if self.inconsistencies["property_mismatches"]:
            print(
                f"Property mismatches "
                f"({len(self.inconsistencies['property_mismatches'])}):"
            )
            for mismatch in self.inconsistencies["property_mismatches"]:
                print(f"  - {mismatch}")
            print()

        # Missing endpoint coverage
        if self.inconsistencies["missing_endpoints"]:
            print(
                f"Backend endpoints without frontend API functions "
                f"({len(self.inconsistencies['missing_endpoints'])}):"
            )
            for endpoint in self.inconsistencies["missing_endpoints"]:
                print(f"  - {endpoint}")
            print()

        # Summary
        total_issues = sum(
            len(v) for v in self.inconsistencies.values()
        )
        print("=" * 60)
        if total_issues == 0:
            print("No inconsistencies detected!")
        else:
            print(f"Total issues: {total_issues}")
        print("=" * 60 + "\n")

    def save(self, output_path: str) -> bool:
        """Save merged graph to JSON file."""
        try:
            output_dir = Path(output_path).parent
            output_dir.mkdir(parents=True, exist_ok=True)

            with open(output_path, "w") as f:
                json.dump(self.merged_graph, f, indent=2)
            return True
        except IOError as e:
            print(f"Error: Failed to write output: {e}", file=sys.stderr)
            return False


def main() -> int:
    """Main entry point."""
    # Determine paths
    script_dir = Path(__file__).parent
    backend_path = script_dir / "backend-graph.json"
    frontend_path = script_dir / "frontend-graph.json"
    output_path = script_dir / "graph-baseline.json"

    # Parse arguments
    for i, arg in enumerate(sys.argv[1:], 1):
        if arg == "--output" and i < len(sys.argv) - 1:
            output_path = sys.argv[i + 1]

    # Create merger
    merger = GraphMerger(str(backend_path), str(frontend_path))

    # Load graphs
    if not merger.load_graphs():
        return 1

    # Detect inconsistencies
    merger.detect_inconsistencies()

    # Merge graphs
    if not merger.merge():
        return 1

    # Save merged graph
    if not merger.save(str(output_path)):
        return 1

    # Print report
    merger.print_inconsistency_report()

    print(f"Merged graph saved to: {output_path}")
    stats = merger.merged_graph.get("stats", {})
    print(
        f"Summary: {stats.get('entities', 0)} entities, "
        f"{stats.get('dtos', 0)} DTOs, {stats.get('types', 0)} types, "
        f"{stats.get('components', 0)} components"
    )

    return 0


if __name__ == "__main__":
    sys.exit(main())
