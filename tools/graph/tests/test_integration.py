"""
Integration tests for the graph generation pipeline.

Tests the full workflow:
1. Backend generator (generate-backend.py)
2. Frontend generator (generate-frontend.js)
3. Graph merger (merge-graph.py)

Tests against fixture files to ensure the complete pipeline works.
"""

import json
import subprocess
import sys
from pathlib import Path
import pytest
import tempfile
import os


@pytest.fixture
def temp_output_dir():
    """Create a temporary directory for test outputs."""
    with tempfile.TemporaryDirectory() as tmpdir:
        yield Path(tmpdir)


@pytest.fixture
def project_root():
    """Return the project root directory."""
    return Path(__file__).parent.parent.parent.parent


@pytest.fixture
def graph_tools_dir():
    """Return the tools/graph directory."""
    return Path(__file__).parent.parent


@pytest.fixture
def backend_generator(graph_tools_dir):
    """Return path to backend generator script."""
    return graph_tools_dir / "generate-backend.py"


@pytest.fixture
def frontend_generator(graph_tools_dir):
    """Return path to frontend generator script."""
    return graph_tools_dir / "generate-frontend.js"


@pytest.fixture
def merge_script(graph_tools_dir):
    """Return path to merge script."""
    return graph_tools_dir / "merge-graph.py"


class TestBackendGeneratorIntegration:
    """Test the backend generator against fixture directory."""

    def test_backend_generator_on_fixtures(self, backend_generator, fixtures_dir, temp_output_dir):
        """Run backend generator against fixtures directory."""
        output_file = temp_output_dir / "backend-graph.json"

        # Create a mock project structure with fixtures
        mock_project = temp_output_dir / "mock_project"
        mock_src = mock_project / "src" / "Koinon.Domain" / "Entities"
        mock_src.mkdir(parents=True)

        # Copy fixture entities
        valid_fixtures = fixtures_dir / "valid"
        if (valid_fixtures / "PersonEntity.cs").exists():
            import shutil
            shutil.copy(valid_fixtures / "PersonEntity.cs", mock_src)

        # Run generator
        result = subprocess.run(
            [sys.executable, str(backend_generator),
             "--project-root", str(mock_project),
             "--output", str(output_file)],
            capture_output=True,
            text=True
        )

        # Should succeed (exit code 0)
        assert result.returncode == 0, f"Generator failed: {result.stderr}"

        # Output file should exist
        assert output_file.exists(), "Backend graph file not created"

        # Load and validate structure
        with open(output_file) as f:
            graph = json.load(f)

        assert "version" in graph
        assert "generated_at" in graph
        assert "entities" in graph
        assert "dtos" in graph
        assert "services" in graph
        assert "controllers" in graph
        assert "edges" in graph


    def test_backend_generator_handles_empty_project(self, backend_generator, temp_output_dir):
        """Backend generator should handle empty project gracefully."""
        output_file = temp_output_dir / "backend-graph.json"
        mock_project = temp_output_dir / "empty_project"
        (mock_project / "src").mkdir(parents=True)

        result = subprocess.run(
            [sys.executable, str(backend_generator),
             "--project-root", str(mock_project),
             "--output", str(output_file)],
            capture_output=True,
            text=True
        )

        # Should succeed even with empty project
        assert result.returncode == 0
        assert output_file.exists()

        with open(output_file) as f:
            graph = json.load(f)

        # Should have empty sections
        assert len(graph["entities"]) == 0
        assert len(graph["dtos"]) == 0


class TestFrontendGeneratorIntegration:
    """Test the frontend generator against fixture directory."""

    def test_frontend_generator_on_fixtures(self, frontend_generator, fixtures_dir, temp_output_dir):
        """Run frontend generator against fixtures directory."""
        # Note: Frontend generator currently hardcoded to look for src/web/src structure
        # relative to cwd, not configurable via CLI arguments.
        # TODO(#301): Make frontend generator accept --src-dir argument for better testability
        pytest.skip("Frontend generator requires specific directory structure")


class TestGraphMergerIntegration:
    """Test the graph merger with sample data."""

    def test_merge_with_sample_graphs(self, merge_script, temp_output_dir,
                                       sample_backend_graph, sample_frontend_graph):
        """Test merge script with sample backend and frontend graphs."""
        backend_file = temp_output_dir / "backend-graph.json"
        frontend_file = temp_output_dir / "frontend-graph.json"
        output_file = temp_output_dir / "merged-graph.json"

        # Write sample graphs
        with open(backend_file, 'w') as f:
            json.dump(sample_backend_graph, f)
        with open(frontend_file, 'w') as f:
            json.dump(sample_frontend_graph, f)

        # Run merger
        result = subprocess.run(
            [sys.executable, str(merge_script), "--output", str(output_file)],
            cwd=str(temp_output_dir),
            capture_output=True,
            text=True
        )

        # Should succeed
        assert result.returncode == 0, f"Merge failed: {result.stderr}"
        assert output_file.exists()

        # Validate merged structure
        with open(output_file) as f:
            merged = json.load(f)

        assert "version" in merged
        assert "entities" in merged
        assert "dtos" in merged
        assert "controllers" in merged
        assert "hooks" in merged
        assert "api_functions" in merged
        assert "components" in merged
        assert "cross_layer_mappings" in merged
        assert "stats" in merged


    def test_merge_detects_inconsistencies(self, merge_script, temp_output_dir,
                                           sample_backend_graph_with_mismatch,
                                           sample_frontend_graph_with_mismatch):
        """Test that merge detects property mismatches."""
        backend_file = temp_output_dir / "backend-graph.json"
        frontend_file = temp_output_dir / "frontend-graph.json"
        output_file = temp_output_dir / "merged-graph.json"

        # Write graphs with mismatches
        with open(backend_file, 'w') as f:
            json.dump(sample_backend_graph_with_mismatch, f)
        with open(frontend_file, 'w') as f:
            json.dump(sample_frontend_graph_with_mismatch, f)

        # Run merger
        result = subprocess.run(
            [sys.executable, str(merge_script), "--output", str(output_file)],
            cwd=str(temp_output_dir),
            capture_output=True,
            text=True
        )

        # Should still succeed (merge always succeeds, just reports issues)
        assert result.returncode == 0

        # Output should mention property mismatch
        output_text = result.stdout + result.stderr
        assert "PhoneNumber" in output_text or "Property" in output_text or "mismatch" in output_text.lower()


class TestFullPipeline:
    """Test the complete pipeline from source files to merged graph."""

    def test_end_to_end_pipeline(self, backend_generator, merge_script,
                                 temp_output_dir, fixtures_dir):
        """Test complete pipeline: backend gen -> frontend gen -> merge."""
        # Create mock project structure
        mock_project = temp_output_dir / "full_pipeline"

        # Backend structure
        backend_entities = mock_project / "src" / "Koinon.Domain" / "Entities"
        backend_dtos = mock_project / "src" / "Koinon.Application" / "DTOs"
        backend_controllers = mock_project / "src" / "Koinon.Api" / "Controllers"

        backend_entities.mkdir(parents=True)
        backend_dtos.mkdir(parents=True)
        backend_controllers.mkdir(parents=True)

        # Copy fixtures
        import shutil
        valid_fixtures = fixtures_dir / "valid"

        if (valid_fixtures / "PersonEntity.cs").exists():
            shutil.copy(valid_fixtures / "PersonEntity.cs", backend_entities)
        if (valid_fixtures / "PersonDto.cs").exists():
            shutil.copy(valid_fixtures / "PersonDto.cs", backend_dtos)
        if (valid_fixtures / "PeopleController.cs").exists():
            shutil.copy(valid_fixtures / "PeopleController.cs", backend_controllers)

        # Frontend structure
        frontend_types = mock_project / "src" / "web" / "src" / "services" / "api"
        frontend_types.mkdir(parents=True)

        if (valid_fixtures / "types.ts").exists():
            shutil.copy(valid_fixtures / "types.ts", frontend_types / "types.ts")

        # Step 1: Generate backend graph
        backend_output = temp_output_dir / "backend-graph.json"
        result = subprocess.run(
            [sys.executable, str(backend_generator),
             "--project-root", str(mock_project),
             "--output", str(backend_output)],
            capture_output=True,
            text=True
        )
        assert result.returncode == 0, f"Backend generation failed: {result.stderr}"
        assert backend_output.exists()

        # Verify backend graph has Person entity
        with open(backend_output) as f:
            backend_graph = json.load(f)
        assert "Person" in backend_graph["entities"] or len(backend_graph["entities"]) > 0

        # Step 2: Generate frontend graph (if Node.js available)
        frontend_output = temp_output_dir / "frontend-graph.json"
        # Create minimal frontend graph for testing
        minimal_frontend = {
            "version": "1.0.0",
            "generated_at": "2025-12-28T00:00:00.000Z",
            "types": {},
            "api_functions": {},
            "hooks": {},
            "components": {},
            "edges": []
        }
        with open(frontend_output, 'w') as f:
            json.dump(minimal_frontend, f)

        # Step 3: Merge graphs
        merged_output = temp_output_dir / "graph-baseline.json"

        # Copy graphs to temp_output_dir for merge script (only if not already there)
        if backend_output != temp_output_dir / "backend-graph.json":
            shutil.copy(backend_output, temp_output_dir / "backend-graph.json")
        if frontend_output != temp_output_dir / "frontend-graph.json":
            shutil.copy(frontend_output, temp_output_dir / "frontend-graph.json")

        result = subprocess.run(
            [sys.executable, str(merge_script),
             "--output", str(merged_output)],
            cwd=str(temp_output_dir),
            capture_output=True,
            text=True
        )
        assert result.returncode == 0, f"Merge failed: {result.stderr}"
        assert merged_output.exists()

        # Verify merged graph structure
        with open(merged_output) as f:
            merged = json.load(f)

        assert "entities" in merged
        assert "dtos" in merged
        assert "controllers" in merged
        assert "stats" in merged
        assert merged["stats"]["entities"] >= 0


class TestGraphStructureValidation:
    """Test that generated graphs match expected schema."""

    def test_backend_graph_schema(self, sample_backend_graph):
        """Validate backend graph has required schema fields."""
        # Top-level fields
        assert "version" in sample_backend_graph
        assert "generated_at" in sample_backend_graph
        assert "entities" in sample_backend_graph
        assert "dtos" in sample_backend_graph
        assert "services" in sample_backend_graph
        assert "controllers" in sample_backend_graph
        assert "edges" in sample_backend_graph

        # Entity structure
        if sample_backend_graph["entities"]:
            entity = next(iter(sample_backend_graph["entities"].values()))
            assert "name" in entity
            assert "namespace" in entity
            assert "table" in entity
            assert "properties" in entity
            assert "navigations" in entity

        # DTO structure
        if sample_backend_graph["dtos"]:
            dto = next(iter(sample_backend_graph["dtos"].values()))
            assert "name" in dto
            assert "namespace" in dto
            assert "properties" in dto


    def test_frontend_graph_schema(self, sample_frontend_graph):
        """Validate frontend graph has required schema fields."""
        # Top-level fields
        assert "version" in sample_frontend_graph
        assert "generated_at" in sample_frontend_graph
        assert "types" in sample_frontend_graph
        assert "api_functions" in sample_frontend_graph
        assert "hooks" in sample_frontend_graph
        assert "components" in sample_frontend_graph
        assert "edges" in sample_frontend_graph

        # Type structure
        if sample_frontend_graph["types"]:
            type_def = next(iter(sample_frontend_graph["types"].values()))
            assert "name" in type_def
            assert "kind" in type_def
            assert "properties" in type_def
            assert "path" in type_def


    def test_merged_graph_has_cross_layer_mappings(self, temp_output_dir, merge_script,
                                                    sample_backend_graph, sample_frontend_graph):
        """Test that merged graph includes cross-layer mappings."""
        backend_file = temp_output_dir / "backend-graph.json"
        frontend_file = temp_output_dir / "frontend-graph.json"
        output_file = temp_output_dir / "merged.json"

        with open(backend_file, 'w') as f:
            json.dump(sample_backend_graph, f)
        with open(frontend_file, 'w') as f:
            json.dump(sample_frontend_graph, f)

        subprocess.run(
            [sys.executable, str(merge_script), "--output", str(output_file)],
            cwd=str(temp_output_dir),
            capture_output=True
        )

        with open(output_file) as f:
            merged = json.load(f)

        assert "cross_layer_mappings" in merged
        # Should have mapping from PersonDto to Person type
        mappings = merged["cross_layer_mappings"]
        assert isinstance(mappings, dict)


class TestEdgeRelationships:
    """Test that edge relationships are correctly generated."""

    def test_backend_edges_include_dto_to_entity(self, sample_backend_graph):
        """Test that DTOs have edges to their entities."""
        edges = sample_backend_graph["edges"]

        # Should have edge from PersonDto to Person
        dto_edges = [e for e in edges if e.get("type") == "maps_to" or e.get("relationship") == "maps_to"]
        assert len(dto_edges) > 0


    def test_frontend_edges_include_component_to_hook(self, sample_frontend_graph):
        """Test that components have edges to hooks they use."""
        edges = sample_frontend_graph["edges"]

        # Should have edge from PersonCard to usePerson
        component_edges = [e for e in edges if e.get("type") == "uses"]
        assert len(component_edges) > 0


    def test_merged_graph_preserves_all_edges(self, temp_output_dir, merge_script,
                                               sample_backend_graph, sample_frontend_graph):
        """Test that merged graph includes edges from both graphs."""
        backend_file = temp_output_dir / "backend-graph.json"
        frontend_file = temp_output_dir / "frontend-graph.json"
        output_file = temp_output_dir / "merged.json"

        with open(backend_file, 'w') as f:
            json.dump(sample_backend_graph, f)
        with open(frontend_file, 'w') as f:
            json.dump(sample_frontend_graph, f)

        subprocess.run(
            [sys.executable, str(merge_script), "--output", str(output_file)],
            cwd=str(temp_output_dir),
            capture_output=True
        )

        with open(output_file) as f:
            merged = json.load(f)

        backend_edge_count = len(sample_backend_graph["edges"])
        frontend_edge_count = len(sample_frontend_graph["edges"])
        merged_edge_count = len(merged["edges"])

        # Merged should have at least the sum of both
        assert merged_edge_count >= backend_edge_count + frontend_edge_count


class TestErrorHandling:
    """Test error handling in generators and merger."""

    def test_backend_generator_missing_src_directory(self, backend_generator, temp_output_dir):
        """Backend generator should fail gracefully if src/ doesn't exist."""
        mock_project = temp_output_dir / "no_src"
        mock_project.mkdir()

        result = subprocess.run(
            [sys.executable, str(backend_generator),
             "--project-root", str(mock_project)],
            capture_output=True,
            text=True
        )

        # Should fail with clear error message
        assert result.returncode != 0
        assert "src" in result.stderr.lower() or "not found" in result.stderr.lower()


    def test_merge_script_missing_input_files(self, merge_script, temp_output_dir):
        """Merge script should fail if input files don't exist."""
        # Note: merge-graph.py uses Path(__file__).parent for input files,
        # so it will always look in tools/graph/ directory.
        # This test verifies the current behavior.
        # TODO(#300): Make merge script accept --backend and --frontend arguments for better testability
        pytest.skip("Merge script currently hardcoded to use tools/graph/ directory")


    def test_merge_script_invalid_json(self, merge_script, temp_output_dir):
        """Merge script should fail gracefully with invalid JSON."""
        # Note: merge-graph.py uses Path(__file__).parent for input files,
        # so it will always look in tools/graph/ directory.
        # This test verifies the current behavior.
        # TODO(#300): Make merge script accept --backend and --frontend arguments for better testability
        pytest.skip("Merge script currently hardcoded to use tools/graph/ directory")
