"""
Unit tests for merge-graph.py graph merger.

Tests the GraphMerger class which combines backend and frontend graphs
and detects cross-layer inconsistencies.
"""

import json
import pytest
import tempfile
from pathlib import Path
import sys

# Add parent directory to path to import merge-graph module
sys.path.insert(0, str(Path(__file__).parent.parent))

# Import merge-graph.py using importlib since it has hyphens
import importlib.util
merge_graph_path = Path(__file__).parent.parent / "merge-graph.py"
spec = importlib.util.spec_from_file_location("merge_graph", merge_graph_path)
merge_graph = importlib.util.module_from_spec(spec)
spec.loader.exec_module(merge_graph)
GraphMerger = merge_graph.GraphMerger


class TestLoadGraphs:
    """Test graph loading functionality."""

    def test_load_graphs_success(self, tmp_path, sample_backend_graph, sample_frontend_graph):
        """Test successful loading of valid graph files."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text(json.dumps(sample_backend_graph))
        frontend_file.write_text(json.dumps(sample_frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        result = merger.load_graphs()

        assert result is True
        assert merger.backend_graph == sample_backend_graph
        assert merger.frontend_graph == sample_frontend_graph

    def test_load_graphs_backend_not_found(self, tmp_path, sample_frontend_graph):
        """Test error handling when backend file doesn't exist."""
        backend_file = tmp_path / "nonexistent.json"
        frontend_file = tmp_path / "frontend.json"
        frontend_file.write_text(json.dumps(sample_frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        result = merger.load_graphs()

        assert result is False

    def test_load_graphs_frontend_not_found(self, tmp_path, sample_backend_graph):
        """Test error handling when frontend file doesn't exist."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "nonexistent.json"
        backend_file.write_text(json.dumps(sample_backend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        result = merger.load_graphs()

        assert result is False

    def test_load_graphs_invalid_json_backend(self, tmp_path, sample_frontend_graph):
        """Test error handling when backend file has invalid JSON."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text("{invalid json}")
        frontend_file.write_text(json.dumps(sample_frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        result = merger.load_graphs()

        assert result is False

    def test_load_graphs_invalid_json_frontend(self, tmp_path, sample_backend_graph):
        """Test error handling when frontend file has invalid JSON."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text(json.dumps(sample_backend_graph))
        frontend_file.write_text("{invalid json}")

        merger = GraphMerger(str(backend_file), str(frontend_file))
        result = merger.load_graphs()

        assert result is False

    def test_load_graphs_empty_files(self, tmp_path):
        """Test handling of empty JSON files."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text("{}")
        frontend_file.write_text("{}")

        merger = GraphMerger(str(backend_file), str(frontend_file))
        result = merger.load_graphs()

        assert result is True
        assert merger.backend_graph == {}
        assert merger.frontend_graph == {}


class TestNormalizeDtoName:
    """Test DTO name normalization."""

    def test_normalize_dto_name_with_dto_suffix(self, tmp_path):
        """Test normalization of DTO name with 'Dto' suffix."""
        merger = GraphMerger("backend.json", "frontend.json")

        assert merger._normalize_dto_name("PersonDto") == "Person"
        assert merger._normalize_dto_name("GroupMemberDto") == "GroupMember"

    def test_normalize_dto_name_without_dto_suffix(self, tmp_path):
        """Test normalization of DTO name without 'Dto' suffix."""
        merger = GraphMerger("backend.json", "frontend.json")

        assert merger._normalize_dto_name("Person") == "Person"
        assert merger._normalize_dto_name("GroupMember") == "GroupMember"

    def test_normalize_dto_name_edge_cases(self, tmp_path):
        """Test edge cases in DTO name normalization."""
        merger = GraphMerger("backend.json", "frontend.json")

        assert merger._normalize_dto_name("Dto") == ""
        assert merger._normalize_dto_name("") == ""


class TestFindMatchingType:
    """Test finding matching frontend types for backend DTOs."""

    def test_find_matching_type_exact_match(self, tmp_path, sample_backend_graph, sample_frontend_graph):
        """Test exact match between DTO and type names."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        # Modify graphs to have exact match
        sample_frontend_graph["types"]["PersonDto"] = {
            "name": "PersonDto",
            "kind": "interface",
            "properties": {},
            "path": "types.ts"
        }

        backend_file.write_text(json.dumps(sample_backend_graph))
        frontend_file.write_text(json.dumps(sample_frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()

        assert merger._find_matching_type("PersonDto") == "PersonDto"

    def test_find_matching_type_normalized_match(self, tmp_path, sample_backend_graph, sample_frontend_graph):
        """Test normalized match (PersonDto -> Person)."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text(json.dumps(sample_backend_graph))
        frontend_file.write_text(json.dumps(sample_frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()

        assert merger._find_matching_type("PersonDto") == "Person"

    def test_find_matching_type_no_match(self, tmp_path, sample_backend_graph, sample_frontend_graph):
        """Test when no matching type exists."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text(json.dumps(sample_backend_graph))
        frontend_file.write_text(json.dumps(sample_frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()

        assert merger._find_matching_type("NonExistentDto") is None

    def test_find_matching_type_case_insensitive(self, tmp_path, sample_backend_graph, sample_frontend_graph):
        """Test case-insensitive matching."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        # Add a type with different casing
        sample_frontend_graph["types"]["person"] = {
            "name": "person",
            "kind": "interface",
            "properties": {},
            "path": "types.ts"
        }

        backend_file.write_text(json.dumps(sample_backend_graph))
        frontend_file.write_text(json.dumps(sample_frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()

        # Should match case-insensitively
        result = merger._find_matching_type("PersonDto")
        assert result is not None
        assert result.lower() == "person"


class TestFindMatchingDto:
    """Test finding matching backend DTOs for frontend types."""

    def test_find_matching_dto_exact_match(self, tmp_path, sample_backend_graph, sample_frontend_graph):
        """Test exact match between type and DTO names."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text(json.dumps(sample_backend_graph))
        frontend_file.write_text(json.dumps(sample_frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()

        assert merger._find_matching_dto("PersonDto") == "PersonDto"

    def test_find_matching_dto_with_suffix(self, tmp_path, sample_backend_graph, sample_frontend_graph):
        """Test matching type to DTO with 'Dto' suffix."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text(json.dumps(sample_backend_graph))
        frontend_file.write_text(json.dumps(sample_frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()

        assert merger._find_matching_dto("Person") == "PersonDto"

    def test_find_matching_dto_no_match(self, tmp_path, sample_backend_graph, sample_frontend_graph):
        """Test when no matching DTO exists."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text(json.dumps(sample_backend_graph))
        frontend_file.write_text(json.dumps(sample_frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()

        assert merger._find_matching_dto("NonExistentType") is None


class TestCompareProperties:
    """Test property comparison between DTOs and types."""

    def test_compare_properties_no_mismatches(self, tmp_path, sample_backend_graph, sample_frontend_graph):
        """Test property comparison with matching properties."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text(json.dumps(sample_backend_graph))
        frontend_file.write_text(json.dumps(sample_frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()

        mismatches = merger._compare_properties("PersonDto", "Person")

        # No mismatches - all DTO properties exist in type (with camelCase conversion)
        assert len(mismatches) == 0

    def test_compare_properties_with_mismatches(self, tmp_path, sample_backend_graph_with_mismatch, sample_frontend_graph_with_mismatch):
        """Test property comparison with missing properties."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text(json.dumps(sample_backend_graph_with_mismatch))
        frontend_file.write_text(json.dumps(sample_frontend_graph_with_mismatch))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()

        mismatches = merger._compare_properties("PersonDto", "Person")

        # PhoneNumber should be missing
        assert len(mismatches) == 1
        assert "PersonDto.PhoneNumber missing in Person" in mismatches

    def test_compare_properties_nonexistent_dto(self, tmp_path, sample_backend_graph, sample_frontend_graph):
        """Test property comparison with non-existent DTO."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text(json.dumps(sample_backend_graph))
        frontend_file.write_text(json.dumps(sample_frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()

        mismatches = merger._compare_properties("NonExistentDto", "Person")

        assert len(mismatches) == 0

    def test_compare_properties_nonexistent_type(self, tmp_path, sample_backend_graph, sample_frontend_graph):
        """Test property comparison with non-existent type."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text(json.dumps(sample_backend_graph))
        frontend_file.write_text(json.dumps(sample_frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()

        mismatches = merger._compare_properties("PersonDto", "NonExistentType")

        assert len(mismatches) == 0

    def test_compare_properties_empty_properties(self, tmp_path):
        """Test property comparison with DTOs/types that have no properties."""
        backend_graph = {
            "dtos": {
                "EmptyDto": {
                    "name": "EmptyDto",
                    "properties": {}
                }
            }
        }
        frontend_graph = {
            "types": {
                "EmptyType": {
                    "name": "EmptyType",
                    "properties": {}
                }
            }
        }

        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text(json.dumps(backend_graph))
        frontend_file.write_text(json.dumps(frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()

        mismatches = merger._compare_properties("EmptyDto", "EmptyType")

        assert len(mismatches) == 0


class TestDetectEndpointCoverage:
    """Test detection of endpoints without frontend API functions."""

    def test_detect_endpoint_coverage_all_covered(self, tmp_path, sample_backend_graph, sample_frontend_graph):
        """Test when all endpoints have corresponding API functions."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text(json.dumps(sample_backend_graph))
        frontend_file.write_text(json.dumps(sample_frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()

        missing = merger._detect_endpoint_coverage()

        # OrphanEndpoint is not covered
        assert len(missing) == 1
        assert any("POST" in m and "orphan" in m for m in missing)

    def test_detect_endpoint_coverage_none_covered(self, tmp_path, sample_backend_graph):
        """Test when no endpoints have corresponding API functions."""
        frontend_graph = {
            "types": {},
            "api_functions": {},
            "hooks": {},
            "components": {}
        }

        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text(json.dumps(sample_backend_graph))
        frontend_file.write_text(json.dumps(frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()

        missing = merger._detect_endpoint_coverage()

        # All 3 endpoints should be missing
        assert len(missing) == 3

    def test_detect_endpoint_coverage_empty_controllers(self, tmp_path, sample_frontend_graph):
        """Test with no controllers in backend."""
        backend_graph = {
            "entities": {},
            "dtos": {},
            "services": {},
            "controllers": {}
        }

        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text(json.dumps(backend_graph))
        frontend_file.write_text(json.dumps(sample_frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()

        missing = merger._detect_endpoint_coverage()

        assert len(missing) == 0


class TestRoutesMatch:
    """Test route matching logic."""

    def test_routes_match_exact(self):
        """Test exact route matching."""
        assert GraphMerger._routes_match("/api/v1/people", "/api/v1/people") is True
        assert GraphMerger._routes_match("api/v1/people", "api/v1/people") is True

    def test_routes_match_with_slashes(self):
        """Test route matching with different slash patterns."""
        assert GraphMerger._routes_match("/api/v1/people/", "api/v1/people") is True
        assert GraphMerger._routes_match("api/v1/people", "/api/v1/people/") is True
        assert GraphMerger._routes_match("//api//v1//people", "/api/v1/people") is True

    def test_routes_match_with_parameters(self):
        """Test route matching with parameters."""
        assert GraphMerger._routes_match("/api/v1/people/{idKey}", "/api/v1/people/{idKey}") is True
        assert GraphMerger._routes_match("/api/v1/people/{id}", "/api/v1/people/{idKey}") is True
        assert GraphMerger._routes_match("/api/v1/people/{param1}", "/api/v1/people/{param2}") is True

    def test_routes_match_different_routes(self):
        """Test route matching with different routes."""
        assert GraphMerger._routes_match("/api/v1/people", "/api/v1/groups") is False
        assert GraphMerger._routes_match("/api/v1/people/{id}", "/api/v1/groups/{id}") is False

    def test_routes_match_mixed_parameters(self):
        """Test route matching with mixed parameter patterns."""
        assert GraphMerger._routes_match(
            "/api/v1/people/{idKey}/groups/{groupId}",
            "/api/v1/people/{id}/groups/{gid}"
        ) is True


class TestDetectInconsistencies:
    """Test cross-layer inconsistency detection."""

    def test_detect_inconsistencies_all_types(self, tmp_path, sample_backend_graph, sample_frontend_graph):
        """Test detection of all inconsistency types."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text(json.dumps(sample_backend_graph))
        frontend_file.write_text(json.dumps(sample_frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()
        merger.detect_inconsistencies()

        # OrphanDto has no matching type
        assert "OrphanDto" in merger.inconsistencies["dtos_without_types"]

        # OrphanType has no matching DTO
        assert "OrphanType" in merger.inconsistencies["types_without_dtos"]

        # OrphanEndpoint has no API function
        assert len(merger.inconsistencies["missing_endpoints"]) > 0

    def test_detect_inconsistencies_property_mismatches(self, tmp_path, sample_backend_graph_with_mismatch, sample_frontend_graph_with_mismatch):
        """Test detection of property mismatches."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text(json.dumps(sample_backend_graph_with_mismatch))
        frontend_file.write_text(json.dumps(sample_frontend_graph_with_mismatch))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()
        merger.detect_inconsistencies()

        # PhoneNumber should be in property mismatches
        assert len(merger.inconsistencies["property_mismatches"]) > 0
        assert any("PhoneNumber" in m for m in merger.inconsistencies["property_mismatches"])

    def test_detect_inconsistencies_no_issues(self, tmp_path):
        """Test with perfectly matching graphs."""
        backend_graph = {
            "dtos": {
                "PersonDto": {
                    "name": "PersonDto",
                    "properties": {
                        "FirstName": "string"
                    }
                }
            },
            "controllers": {}
        }
        frontend_graph = {
            "types": {
                "Person": {
                    "name": "Person",
                    "properties": {
                        "firstName": "string"
                    }
                }
            },
            "api_functions": {}
        }

        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text(json.dumps(backend_graph))
        frontend_file.write_text(json.dumps(frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()
        merger.detect_inconsistencies()

        assert len(merger.inconsistencies["dtos_without_types"]) == 0
        assert len(merger.inconsistencies["types_without_dtos"]) == 0
        assert len(merger.inconsistencies["property_mismatches"]) == 0
        assert len(merger.inconsistencies["missing_endpoints"]) == 0


class TestMerge:
    """Test graph merging functionality."""

    def test_merge_success(self, tmp_path, sample_backend_graph, sample_frontend_graph):
        """Test successful graph merge."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text(json.dumps(sample_backend_graph))
        frontend_file.write_text(json.dumps(sample_frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()
        result = merger.merge()

        assert result is True
        assert "version" in merger.merged_graph
        assert "generated_at" in merger.merged_graph

    def test_merge_includes_all_sections(self, tmp_path, sample_backend_graph, sample_frontend_graph):
        """Test that merge includes all required sections."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text(json.dumps(sample_backend_graph))
        frontend_file.write_text(json.dumps(sample_frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()
        merger.merge()

        # Check backend sections
        assert "entities" in merger.merged_graph
        assert "dtos" in merger.merged_graph
        assert "services" in merger.merged_graph
        assert "controllers" in merger.merged_graph

        # Check frontend sections
        assert "hooks" in merger.merged_graph
        assert "api_functions" in merger.merged_graph
        assert "components" in merger.merged_graph

        # Check merged sections
        assert "edges" in merger.merged_graph
        assert "cross_layer_mappings" in merger.merged_graph
        assert "stats" in merger.merged_graph

    def test_merge_combines_edges(self, tmp_path, sample_backend_graph, sample_frontend_graph):
        """Test that edges from both graphs are combined."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text(json.dumps(sample_backend_graph))
        frontend_file.write_text(json.dumps(sample_frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()
        merger.merge()

        backend_edge_count = len(sample_backend_graph["edges"])
        frontend_edge_count = len(sample_frontend_graph["edges"])
        total_edges = len(merger.merged_graph["edges"])

        assert total_edges == backend_edge_count + frontend_edge_count

    def test_merge_creates_cross_layer_mappings(self, tmp_path, sample_backend_graph, sample_frontend_graph):
        """Test that cross-layer mappings are created."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text(json.dumps(sample_backend_graph))
        frontend_file.write_text(json.dumps(sample_frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()
        merger.merge()

        mappings = merger.merged_graph["cross_layer_mappings"]

        # PersonDto should map to Person type
        assert "dto:PersonDto" in mappings
        assert mappings["dto:PersonDto"] == "type:Person"

    def test_merge_calculates_stats(self, tmp_path, sample_backend_graph, sample_frontend_graph):
        """Test that statistics are calculated correctly."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text(json.dumps(sample_backend_graph))
        frontend_file.write_text(json.dumps(sample_frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()
        merger.merge()

        stats = merger.merged_graph["stats"]

        assert stats["entities"] == len(sample_backend_graph["entities"])
        assert stats["dtos"] == len(sample_backend_graph["dtos"])
        assert stats["types"] == len(sample_frontend_graph["types"])
        assert stats["components"] == len(sample_frontend_graph["components"])
        assert stats["total_edges"] == len(merger.merged_graph["edges"])

    def test_merge_with_empty_graphs(self, tmp_path):
        """Test merging empty graphs."""
        backend_graph = {
            "entities": {},
            "dtos": {},
            "services": {},
            "controllers": {},
            "edges": []
        }
        frontend_graph = {
            "types": {},
            "api_functions": {},
            "hooks": {},
            "components": {},
            "edges": []
        }

        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text(json.dumps(backend_graph))
        frontend_file.write_text(json.dumps(frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()
        result = merger.merge()

        assert result is True
        assert merger.merged_graph["stats"]["entities"] == 0
        assert merger.merged_graph["stats"]["total_edges"] == 0


class TestSave:
    """Test saving merged graph to file."""

    def test_save_success(self, tmp_path, sample_backend_graph, sample_frontend_graph):
        """Test successful save of merged graph."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"
        output_file = tmp_path / "output.json"

        backend_file.write_text(json.dumps(sample_backend_graph))
        frontend_file.write_text(json.dumps(sample_frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()
        merger.merge()
        result = merger.save(str(output_file))

        assert result is True
        assert output_file.exists()

        # Verify content is valid JSON
        with open(output_file) as f:
            saved_data = json.load(f)
        assert saved_data == merger.merged_graph

    def test_save_creates_directories(self, tmp_path, sample_backend_graph, sample_frontend_graph):
        """Test that save creates parent directories."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"
        output_file = tmp_path / "nested" / "dir" / "output.json"

        backend_file.write_text(json.dumps(sample_backend_graph))
        frontend_file.write_text(json.dumps(sample_frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()
        merger.merge()
        result = merger.save(str(output_file))

        assert result is True
        assert output_file.exists()
        assert output_file.parent.exists()

    def test_save_invalid_path(self, tmp_path, sample_backend_graph, sample_frontend_graph):
        """Test save with invalid path."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text(json.dumps(sample_backend_graph))
        frontend_file.write_text(json.dumps(sample_frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()
        merger.merge()

        # Try to save to a path that will fail (e.g., to a directory that can't be created)
        result = merger.save("/dev/null/invalid/path/output.json")

        assert result is False


class TestPrintInconsistencyReport:
    """Test inconsistency report printing."""

    def test_print_inconsistency_report_with_issues(self, tmp_path, sample_backend_graph, sample_frontend_graph, capsys):
        """Test printing report when inconsistencies exist."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text(json.dumps(sample_backend_graph))
        frontend_file.write_text(json.dumps(sample_frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()
        merger.detect_inconsistencies()
        merger.print_inconsistency_report()

        captured = capsys.readouterr()

        assert "Cross-Layer Inconsistency Report" in captured.out
        assert "OrphanDto" in captured.out
        assert "OrphanType" in captured.out

    def test_print_inconsistency_report_no_issues(self, tmp_path, capsys):
        """Test printing report when no inconsistencies exist."""
        backend_graph = {
            "dtos": {},
            "controllers": {}
        }
        frontend_graph = {
            "types": {},
            "api_functions": {}
        }

        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"

        backend_file.write_text(json.dumps(backend_graph))
        frontend_file.write_text(json.dumps(frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))
        merger.load_graphs()
        merger.detect_inconsistencies()
        merger.print_inconsistency_report()

        captured = capsys.readouterr()

        assert "No inconsistencies detected!" in captured.out


class TestIntegration:
    """Integration tests for complete workflow."""

    def test_complete_workflow(self, tmp_path, sample_backend_graph, sample_frontend_graph):
        """Test complete workflow: load -> detect -> merge -> save."""
        backend_file = tmp_path / "backend.json"
        frontend_file = tmp_path / "frontend.json"
        output_file = tmp_path / "merged.json"

        backend_file.write_text(json.dumps(sample_backend_graph))
        frontend_file.write_text(json.dumps(sample_frontend_graph))

        merger = GraphMerger(str(backend_file), str(frontend_file))

        # Load
        assert merger.load_graphs() is True

        # Detect inconsistencies
        merger.detect_inconsistencies()
        assert len(merger.inconsistencies["dtos_without_types"]) > 0

        # Merge
        assert merger.merge() is True

        # Save
        assert merger.save(str(output_file)) is True

        # Verify output
        assert output_file.exists()
        with open(output_file) as f:
            merged = json.load(f)

        assert "version" in merged
        assert "entities" in merged
        assert "dtos" in merged
        assert "components" in merged
        assert "hooks" in merged
        assert "cross_layer_mappings" in merged
        assert "stats" in merged
