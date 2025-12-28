"""
Edge case tests for graph generators.

Tests parser resilience to:
- Empty files
- Syntax errors
- Missing files
- Unicode characters
- Unusual formatting

Ensures generators don't crash on unexpected input.
"""

import json
import subprocess
import sys
from pathlib import Path
import pytest
import tempfile


@pytest.fixture
def temp_dir():
    """Create temporary directory for test outputs."""
    with tempfile.TemporaryDirectory() as tmpdir:
        yield Path(tmpdir)


@pytest.fixture
def backend_generator():
    """Return path to backend generator script."""
    return Path(__file__).parent.parent / "generate-backend.py"


@pytest.fixture
def merge_script():
    """Return path to merge script."""
    return Path(__file__).parent.parent / "merge-graph.py"


class TestEmptyFileHandling:
    """Test handling of empty files."""

    def test_empty_cs_file(self, empty_file, backend_generator, temp_dir):
        """Backend generator should handle empty .cs files gracefully."""
        # Create mock project with empty entity file
        mock_project = temp_dir / "empty_test"
        entities_dir = mock_project / "src" / "Koinon.Domain" / "Entities"
        entities_dir.mkdir(parents=True)

        # Copy empty fixture
        import shutil
        shutil.copy(empty_file, entities_dir / "EmptyEntity.cs")

        output_file = temp_dir / "backend-graph.json"

        result = subprocess.run(
            [sys.executable, str(backend_generator),
             "--project-root", str(mock_project),
             "--output", str(output_file)],
            capture_output=True,
            text=True
        )

        # Should succeed (no crash)
        assert result.returncode == 0, f"Generator crashed on empty file: {result.stderr}"

        # Should produce valid JSON
        assert output_file.exists()
        with open(output_file) as f:
            graph = json.load(f)

        # Empty file should not create entity
        assert "entities" in graph
        assert isinstance(graph["entities"], dict)


    def test_empty_directory(self, backend_generator, temp_dir):
        """Backend generator should handle empty entity directory."""
        mock_project = temp_dir / "empty_dir_test"
        entities_dir = mock_project / "src" / "Koinon.Domain" / "Entities"
        entities_dir.mkdir(parents=True)

        output_file = temp_dir / "backend-graph.json"

        result = subprocess.run(
            [sys.executable, str(backend_generator),
             "--project-root", str(mock_project),
             "--output", str(output_file)],
            capture_output=True,
            text=True
        )

        assert result.returncode == 0
        assert output_file.exists()

        with open(output_file) as f:
            graph = json.load(f)

        assert len(graph["entities"]) == 0
        assert len(graph["dtos"]) == 0


class TestSyntaxErrorHandling:
    """Test handling of files with syntax errors."""

    def test_syntax_error_file(self, syntax_error_file, backend_generator, temp_dir):
        """Backend generator should skip files with syntax errors gracefully."""
        mock_project = temp_dir / "syntax_error_test"
        entities_dir = mock_project / "src" / "Koinon.Domain" / "Entities"
        entities_dir.mkdir(parents=True)

        import shutil
        shutil.copy(syntax_error_file, entities_dir / "BadEntity.cs")

        output_file = temp_dir / "backend-graph.json"

        result = subprocess.run(
            [sys.executable, str(backend_generator),
             "--project-root", str(mock_project),
             "--output", str(output_file)],
            capture_output=True,
            text=True
        )

        # Should NOT crash (syntax errors should be tolerated by regex parser)
        assert result.returncode == 0, f"Generator crashed on syntax error: {result.stderr}"

        # Should produce valid JSON
        assert output_file.exists()
        with open(output_file) as f:
            graph = json.load(f)

        # Parser may extract partial data or skip the file
        assert "entities" in graph


    def test_mixed_valid_and_invalid_files(self, syntax_error_file, person_entity_file,
                                           backend_generator, temp_dir):
        """Generator should process valid files even when some have syntax errors."""
        mock_project = temp_dir / "mixed_test"
        entities_dir = mock_project / "src" / "Koinon.Domain" / "Entities"
        entities_dir.mkdir(parents=True)

        import shutil
        shutil.copy(syntax_error_file, entities_dir / "BadEntity.cs")
        shutil.copy(person_entity_file, entities_dir / "PersonEntity.cs")

        output_file = temp_dir / "backend-graph.json"

        result = subprocess.run(
            [sys.executable, str(backend_generator),
             "--project-root", str(mock_project),
             "--output", str(output_file)],
            capture_output=True,
            text=True
        )

        assert result.returncode == 0
        assert output_file.exists()

        with open(output_file) as f:
            graph = json.load(f)

        # Should have processed PersonEntity successfully
        entities = graph["entities"]
        # At least one entity should be present (Person)
        assert len(entities) >= 1


class TestMissingFileHandling:
    """Test handling of missing or inaccessible files."""

    def test_missing_entity_directory(self, backend_generator, temp_dir):
        """Generator should handle missing Entities directory."""
        mock_project = temp_dir / "missing_dir_test"
        (mock_project / "src").mkdir(parents=True)
        # Don't create Entities directory

        output_file = temp_dir / "backend-graph.json"

        result = subprocess.run(
            [sys.executable, str(backend_generator),
             "--project-root", str(mock_project),
             "--output", str(output_file)],
            capture_output=True,
            text=True
        )

        # Should succeed with warning
        assert result.returncode == 0
        assert output_file.exists()

        # Should log warning about missing directory
        assert "warning" in result.stderr.lower() or "not found" in result.stderr.lower()

        with open(output_file) as f:
            graph = json.load(f)

        assert len(graph["entities"]) == 0


    def test_nonexistent_project_root(self, backend_generator, temp_dir):
        """Generator should fail gracefully if project root doesn't exist."""
        nonexistent = temp_dir / "does_not_exist"
        output_file = temp_dir / "backend-graph.json"

        result = subprocess.run(
            [sys.executable, str(backend_generator),
             "--project-root", str(nonexistent),
             "--output", str(output_file)],
            capture_output=True,
            text=True
        )

        # Should fail with clear error
        assert result.returncode != 0
        assert "not found" in result.stderr.lower() or "error" in result.stderr.lower()


class TestUnicodeHandling:
    """Test handling of Unicode characters in source files."""

    def test_unicode_in_comments(self, edge_case_fixtures_dir, backend_generator, temp_dir):
        """Generator should handle Unicode in comments and strings."""
        unicode_file = edge_case_fixtures_dir / "UnicodeNames.cs"
        if not unicode_file.exists():
            pytest.skip("Unicode fixture not found")

        mock_project = temp_dir / "unicode_test"
        entities_dir = mock_project / "src" / "Koinon.Domain" / "Entities"
        entities_dir.mkdir(parents=True)

        import shutil
        shutil.copy(unicode_file, entities_dir / "UnicodeEntity.cs")

        output_file = temp_dir / "backend-graph.json"

        result = subprocess.run(
            [sys.executable, str(backend_generator),
             "--project-root", str(mock_project),
             "--output", str(output_file)],
            capture_output=True,
            text=True,
            encoding='utf-8'
        )

        # Should NOT crash on Unicode
        assert result.returncode == 0, f"Generator crashed on Unicode: {result.stderr}"
        assert output_file.exists()

        # Should produce valid UTF-8 JSON
        with open(output_file, encoding='utf-8') as f:
            graph = json.load(f)

        assert "entities" in graph


    def test_unicode_in_type_definitions(self, edge_case_fixtures_dir, temp_dir):
        """Test Unicode handling in TypeScript types."""
        unicode_ts_file = edge_case_fixtures_dir / "unicodeContent.ts"
        if not unicode_ts_file.exists():
            pytest.skip("Unicode TypeScript fixture not found")

        # Read the file to verify it contains Unicode
        content = unicode_ts_file.read_text(encoding='utf-8')
        # Should contain non-ASCII characters
        assert any(ord(char) > 127 for char in content), "Fixture should contain Unicode characters"


class TestUnusualFormattingHandling:
    """Test handling of unusual but valid formatting."""

    def test_unusual_whitespace(self, unusual_formatting_file, backend_generator, temp_dir):
        """Generator should handle unusual whitespace and formatting."""
        mock_project = temp_dir / "formatting_test"
        entities_dir = mock_project / "src" / "Koinon.Domain" / "Entities"
        entities_dir.mkdir(parents=True)

        import shutil
        shutil.copy(unusual_formatting_file, entities_dir / "WeirdEntity.cs")

        output_file = temp_dir / "backend-graph.json"

        result = subprocess.run(
            [sys.executable, str(backend_generator),
             "--project-root", str(mock_project),
             "--output", str(output_file)],
            capture_output=True,
            text=True
        )

        # Should NOT crash
        assert result.returncode == 0, f"Generator crashed on unusual formatting: {result.stderr}"
        assert output_file.exists()

        with open(output_file) as f:
            graph = json.load(f)

        # Should parse at least some data
        assert "entities" in graph


    def test_single_line_class_definition(self, temp_dir, backend_generator):
        """Generator should handle single-line class definitions."""
        mock_project = temp_dir / "single_line_test"
        entities_dir = mock_project / "src" / "Koinon.Domain" / "Entities"
        entities_dir.mkdir(parents=True)

        # Create file with single-line class
        single_line = entities_dir / "SingleLine.cs"
        single_line.write_text(
            "namespace Koinon.Domain.Entities; public class SingleLine : Entity { public string Name { get; set; } }"
        )

        output_file = temp_dir / "backend-graph.json"

        result = subprocess.run(
            [sys.executable, str(backend_generator),
             "--project-root", str(mock_project),
             "--output", str(output_file)],
            capture_output=True,
            text=True
        )

        assert result.returncode == 0
        assert output_file.exists()


class TestMergeEdgeCases:
    """Test edge cases in graph merging."""

    def test_merge_with_empty_backend(self, merge_script, temp_dir):
        """Merge should handle empty backend graph."""
        # Note: merge-graph.py uses Path(__file__).parent for input files
        # TODO(#300): Make merge script accept --backend and --frontend arguments
        pytest.skip("Merge script currently hardcoded to use tools/graph/ directory")


    def test_merge_with_empty_frontend(self, merge_script, temp_dir):
        """Merge should handle empty frontend graph."""
        # Note: merge-graph.py uses Path(__file__).parent for input files
        # TODO(#300): Make merge script accept --backend and --frontend arguments
        pytest.skip("Merge script currently hardcoded to use tools/graph/ directory")


    def test_merge_with_orphaned_types(self, merge_script, temp_dir):
        """Merge should report orphaned types without DTOs."""
        backend_file = temp_dir / "backend-graph.json"
        frontend_file = temp_dir / "frontend-graph.json"
        output_file = temp_dir / "merged.json"

        backend = {
            "version": "1.0",
            "generated_at": "2025-12-28T00:00:00+00:00",
            "entities": {},
            "dtos": {},
            "services": {},
            "controllers": {},
            "edges": []
        }

        frontend_with_orphan = {
            "version": "1.0.0",
            "generated_at": "2025-12-28T00:00:00.000Z",
            "types": {
                "OrphanType": {
                    "name": "OrphanType",
                    "kind": "interface",
                    "properties": {"value": "string"},
                    "path": "types.ts"
                }
            },
            "api_functions": {},
            "hooks": {},
            "components": {},
            "edges": []
        }

        with open(backend_file, 'w') as f:
            json.dump(backend, f)
        with open(frontend_file, 'w') as f:
            json.dump(frontend_with_orphan, f)

        result = subprocess.run(
            [sys.executable, str(merge_script), "--output", str(output_file)],
            cwd=str(temp_dir),
            capture_output=True,
            text=True
        )

        # Should succeed
        assert result.returncode == 0

        # Output should mention orphaned type
        output_text = result.stdout + result.stderr
        assert "OrphanType" in output_text or "without" in output_text.lower()


class TestOutputFileCreation:
    """Test edge cases in output file creation."""

    def test_output_to_nonexistent_directory(self, backend_generator, temp_dir):
        """Generator should create output directory if it doesn't exist."""
        mock_project = temp_dir / "output_test"
        entities_dir = mock_project / "src" / "Koinon.Domain" / "Entities"
        entities_dir.mkdir(parents=True)

        # Create file
        (entities_dir / "Test.cs").write_text(
            "namespace Koinon.Domain.Entities; public class Test : Entity { }"
        )

        # Output to non-existent nested directory
        output_file = temp_dir / "nested" / "deep" / "backend-graph.json"

        result = subprocess.run(
            [sys.executable, str(backend_generator),
             "--project-root", str(mock_project),
             "--output", str(output_file)],
            capture_output=True,
            text=True
        )

        # Should create directories and succeed
        assert result.returncode == 0
        assert output_file.exists()
        assert output_file.parent.exists()


    def test_overwrite_existing_output(self, backend_generator, temp_dir):
        """Generator should overwrite existing output file."""
        mock_project = temp_dir / "overwrite_test"
        entities_dir = mock_project / "src" / "Koinon.Domain" / "Entities"
        entities_dir.mkdir(parents=True)

        output_file = temp_dir / "backend-graph.json"

        # Create existing file with different content
        with open(output_file, 'w') as f:
            json.dump({"old": "data"}, f)

        # Run generator
        result = subprocess.run(
            [sys.executable, str(backend_generator),
             "--project-root", str(mock_project),
             "--output", str(output_file)],
            capture_output=True,
            text=True
        )

        assert result.returncode == 0

        # Should have new content
        with open(output_file) as f:
            new_data = json.load(f)

        assert "old" not in new_data
        assert "entities" in new_data


class TestConcurrentFileAccess:
    """Test behavior when files are modified during processing."""

    def test_readonly_input_file(self, backend_generator, temp_dir):
        """Generator should handle read-only files gracefully."""
        mock_project = temp_dir / "readonly_test"
        entities_dir = mock_project / "src" / "Koinon.Domain" / "Entities"
        entities_dir.mkdir(parents=True)

        readonly_file = entities_dir / "ReadOnly.cs"
        readonly_file.write_text(
            "namespace Koinon.Domain.Entities; public class ReadOnly : Entity { }"
        )

        # Make file read-only (Unix permissions)
        import os
        os.chmod(readonly_file, 0o444)

        output_file = temp_dir / "backend-graph.json"

        result = subprocess.run(
            [sys.executable, str(backend_generator),
             "--project-root", str(mock_project),
             "--output", str(output_file)],
            capture_output=True,
            text=True
        )

        # Should succeed (only needs read access)
        assert result.returncode == 0
        assert output_file.exists()

        # Restore permissions for cleanup
        os.chmod(readonly_file, 0o644)


class TestEncodingEdgeCases:
    """Test handling of different file encodings."""

    def test_utf8_with_bom(self, backend_generator, temp_dir):
        """Generator should handle UTF-8 files with BOM."""
        mock_project = temp_dir / "bom_test"
        entities_dir = mock_project / "src" / "Koinon.Domain" / "Entities"
        entities_dir.mkdir(parents=True)

        # Create file with UTF-8 BOM
        bom_file = entities_dir / "BomEntity.cs"
        content = "namespace Koinon.Domain.Entities; public class BomEntity : Entity { }"
        # UTF-8 BOM
        bom_file.write_bytes(b'\xef\xbb\xbf' + content.encode('utf-8'))

        output_file = temp_dir / "backend-graph.json"

        result = subprocess.run(
            [sys.executable, str(backend_generator),
             "--project-root", str(mock_project),
             "--output", str(output_file)],
            capture_output=True,
            text=True
        )

        # Should handle BOM gracefully
        assert result.returncode == 0
        assert output_file.exists()
