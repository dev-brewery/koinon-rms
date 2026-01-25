#!/usr/bin/env python3
"""
Backend Graph Generator for Koinon RMS

Parses C# source files to extract entities, DTOs, services, and controllers
into a JSON graph representation.

Usage:
    python3 generate-backend.py [--output OUTPUT_PATH]
"""

import json
import os
import re
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Dict, List, Optional, Any, Tuple, Set


class CSharpParser:
    """Parses C# source files using regex patterns."""

    def __init__(self, base_path: str):
        self.base_path = Path(base_path)

    def read_file(self, file_path: Path) -> str:
        """Read file contents with error handling."""
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                return f.read()
        except Exception as e:
            print(f"Warning: Could not read {file_path}: {e}", file=sys.stderr)
            return ""

    def extract_namespace(self, content: str) -> Optional[str]:
        """Extract namespace from C# file."""
        match = re.search(r'namespace\s+([A-Za-z0-9._]+)\s*[;{]', content)
        return match.group(1) if match else None

    def extract_class_name(self, content: str) -> Optional[str]:
        """Extract primary class name from C# file."""
        # Match class definition (not nested classes, interfaces, or records unless specified)
        match = re.search(r'(?:public\s+)?(?:abstract\s+)?(?:sealed\s+)?(?:partial\s+)?(?:record\s+|class\s+)([A-Za-z0-9_]+)', content)
        return match.group(1) if match else None

    def extract_class_name_from_file(self, file_path: Path) -> Optional[str]:
        """Extract class name from file name (PascalCase without .cs)."""
        return file_path.stem

    def extract_properties(self, content: str) -> Dict[str, str]:
        """Extract public properties from class."""
        properties = {}
        # Match public property declarations: public Type PropertyName { get; set; }
        # Handle 'required' keyword and generic types with spaces (e.g., IDictionary<string, string>)
        pattern = r'public\s+(?:required\s+)?(.+?)\s+([A-Za-z_][A-Za-z0-9_]*)\s*[{;]'
        for match in re.finditer(pattern, content):
            prop_type = match.group(1).strip()
            prop_name = match.group(2)
            # Skip if it's a method or other construct
            if prop_type not in ['class', 'record', 'enum', 'struct', 'interface']:
                properties[prop_name] = prop_type
        return properties

    def extract_class_body(self, content: str, class_name: str) -> Optional[str]:
        """Extract the body of a specific class/record from content.

        Returns the content between the opening and closing braces of the
        specified class/record definition, or None if not found.
        """
        # Match the class/record definition: record ClassName(...) { ... } or class ClassName { ... }
        # Handle primary constructors: record ClassName(Type Prop, ...) { }
        # Handle inheritance: class ClassName : BaseClass { }
        pattern = rf'(?:public\s+)?(?:sealed\s+)?(?:abstract\s+)?(?:partial\s+)?(?:record|class|struct)\s+{re.escape(class_name)}\s*(?:\([^)]*\))?\s*(?::[^{{]+)?\s*{{'

        match = re.search(pattern, content)
        if not match:
            return None

        # Find matching closing brace
        start_pos = match.end()
        brace_count = 1
        pos = start_pos

        while pos < len(content) and brace_count > 0:
            if content[pos] == '{':
                brace_count += 1
            elif content[pos] == '}':
                brace_count -= 1
            pos += 1

        if brace_count == 0:
            return content[start_pos:pos - 1]

        return None

    def _extract_balanced_parens(self, content: str, start_pos: int) -> str:
        """Extract content between balanced parentheses starting at start_pos.

        start_pos should point to the opening parenthesis.
        Returns the content between the parens (not including the parens themselves).
        """
        if start_pos >= len(content) or content[start_pos] != '(':
            return ""

        paren_count = 1
        pos = start_pos + 1

        while pos < len(content) and paren_count > 0:
            if content[pos] == '(':
                paren_count += 1
            elif content[pos] == ')':
                paren_count -= 1
            pos += 1

        if paren_count == 0:
            return content[start_pos + 1:pos - 1]
        return ""

    def _strip_comments(self, text: str) -> str:
        """Remove C# single-line comments from text."""
        lines = text.split('\n')
        cleaned = []
        for line in lines:
            # Remove // comments
            comment_idx = line.find('//')
            if comment_idx >= 0:
                line = line[:comment_idx]
            cleaned.append(line)
        return '\n'.join(cleaned)

    def extract_record_properties(self, content: str, class_name: str) -> Dict[str, str]:
        """Extract properties from a specific record/class.

        For records with primary constructors (record Foo(Type Prop)), extracts
        the constructor parameters as properties. Also extracts body properties.
        """
        properties = {}

        # Pattern 1: Primary constructor parameters for records
        # Find the record definition and extract balanced parentheses
        record_start_pattern = rf'(?:public\s+)?(?:sealed\s+)?record\s+{re.escape(class_name)}\s*\('
        match = re.search(record_start_pattern, content)
        if match:
            # Find the opening paren position
            paren_pos = match.end() - 1
            params_str = self._extract_balanced_parens(content, paren_pos)
            # Strip comments before parsing
            params_str = self._strip_comments(params_str)
            params = self._split_parameters(params_str)
            for param in params:
                param = param.strip()
                if not param:
                    continue
                # Parse: Type PropName or required Type PropName
                # Handle nullable types like string? and generic types like List<string>
                param_match = re.match(r'(?:required\s+)?([A-Za-z0-9_?<>,\[\]\s]+?)\s+([A-Za-z_][A-Za-z0-9_]*)\s*(?:=.*)?$', param)
                if param_match:
                    prop_type = param_match.group(1).strip()
                    prop_name = param_match.group(2).strip()
                    if prop_type and prop_name:
                        properties[prop_name] = prop_type

        # Pattern 2: Body properties (public Type PropName { get; set; })
        class_body = self.extract_class_body(content, class_name)
        if class_body:
            body_props = self.extract_properties(class_body)
            properties.update(body_props)

        return properties

    def extract_navigations(self, content: str) -> List[Dict[str, str]]:
        """Extract navigation properties from entity."""
        navigations = []
        # Match navigation properties: public ICollection<Entity> or public Entity navigation
        pattern = r'public\s+(?:ICollection<([A-Za-z0-9_]+)>|([A-Za-z0-9_]+))\s+([A-Za-z_][A-Za-z0-9_]*)\s*[{;]'
        for match in re.finditer(pattern, content):
            collection_type = match.group(1)
            single_type = match.group(2)
            nav_name = match.group(3)

            if collection_type:
                navigations.append({
                    'name': nav_name,
                    'target_entity': collection_type,
                    'type': 'many'
                })
            elif single_type and single_type[0].isupper():  # Only if starts with uppercase (likely entity)
                navigations.append({
                    'name': nav_name,
                    'target_entity': single_type,
                    'type': 'one'
                })
        return navigations

    def camel_to_snake(self, name: str) -> str:
        """Convert CamelCase to snake_case."""
        # Insert underscore before uppercase letters
        s1 = re.sub('(.)([A-Z][a-z]+)', r'\1_\2', name)
        # Insert underscore before sequences of uppercase letters
        return re.sub('([a-z0-9])([A-Z])', r'\1_\2', s1).lower()

    def extract_table_name(self, class_name: str, content: str) -> str:
        """Extract table name from Table attribute or derive from class name."""
        # Look for [Table("table_name")]
        match = re.search(r'Table\s*\(\s*["\']([^"\']+)["\']', content)
        if match:
            return match.group(1)
        # Default to snake_case of class name
        return self.camel_to_snake(class_name)

    def extract_methods(self, content: str) -> List[Dict[str, Any]]:
        """Extract public methods from class."""
        methods = []
        # Match public methods - improved to handle async and various return types
        pattern = r'public\s+(?:async\s+)?(?:override\s+)?(?:virtual\s+)?([^\s(]+(?:<[^>]+(?:<[^>]+>)?[^>]*>)?)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\('
        for match in re.finditer(pattern, content):
            return_type = match.group(1).strip()
            method_name = match.group(2)
            is_async = 'async' in content[max(0, match.start()-50):match.start()]
            if method_name not in ['get', 'set']:  # Skip property accessors
                methods.append({
                    'name': method_name,
                    'return_type': return_type,
                    'is_async': is_async
                })
        return methods

    def extract_constructor_dependencies(self, content: str) -> List[str]:
        """Extract constructor parameters (dependencies)."""
        dependencies = []
        # Handle primary constructor syntax: class MyClass(IService a, IService b)
        pattern = r'(?:public\s+)?(?:class|record|struct)\s+\w+\s*\(([^)]+)\)'
        match = re.search(pattern, content)
        if match:
            params_str = match.group(1)
            # Split by comma but respect generic types
            params = self._split_parameters(params_str)
            for param in params:
                param = param.strip()
                if param:
                    # Extract type (first word or full generic type)
                    type_match = re.match(r'(?:required\s+)?([A-Za-z0-9._<>,\[\]\s]+?)\s+[a-z_]', param)
                    if type_match:
                        param_type = type_match.group(1).strip()
                        if param_type:
                            dependencies.append(param_type)
        return dependencies

    def _split_parameters(self, params_str: str) -> List[str]:
        """Split parameters respecting generic type brackets."""
        params = []
        current = []
        bracket_count = 0
        for char in params_str:
            if char in '<[':
                bracket_count += 1
                current.append(char)
            elif char in '>]':
                bracket_count -= 1
                current.append(char)
            elif char == ',' and bracket_count == 0:
                params.append(''.join(current))
                current = []
            else:
                current.append(char)
        if current:
            params.append(''.join(current))
        return params

    def extract_route_attribute(self, content: str) -> Optional[str]:
        """Extract [Route(...)] attribute from controller."""
        match = re.search(r'\[Route\s*\(\s*["\']([^"\']+)["\']', content)
        if match:
            return match.group(1)
        return None

    def extract_endpoints(self, content: str) -> List[Dict[str, Any]]:
        """Extract HTTP endpoints from controller."""
        endpoints = []
        # Find all endpoint methods
        http_methods = ['HttpGet', 'HttpPost', 'HttpPut', 'HttpPatch', 'HttpDelete', 'HttpHead', 'HttpOptions']

        # More precise pattern: look for attribute followed by method signature
        for http_method in http_methods:
            # Find all [HttpXxx(...)] attributes and their following method
            pattern = rf'\[{http_method}\s*\(\s*["\']?([^"\'\]]*)["\']?\s*\)\]'
            for match in re.finditer(pattern, content):
                route_part = match.group(1).strip() or ''
                # Find the method name after the attribute
                start_pos = match.end()
                # Look for method signature within next 500 chars
                method_pattern = r'(?:public\s+(?:async\s+)?(?:IActionResult|ActionResult|Task|Result)[^\(]*?([A-Za-z_][A-Za-z0-9_]*)\s*\()'
                next_method = re.search(method_pattern, content[start_pos:start_pos+1000])
                if next_method:
                    method_name = next_method.group(1)
                    endpoints.append({
                        'name': method_name,
                        'method': http_method.replace('Http', '').upper(),
                        'route': route_part,
                        'request_type': None,
                        'response_type': None,
                        'requires_auth': True,
                        'required_roles': []
                    })
        return endpoints

    def extract_patterns(self, content: str) -> Dict[str, bool]:
        """Extract architectural patterns used in controller."""
        patterns = {
            'response_envelope': bool(re.search(r'new\s*\{\s*(?:data|Data)\s*=', content)),
            'idkey_routes': bool(re.search(r'\{idKey\}', content)),
            'problem_details': bool(re.search(r'Problem\(|ProblemDetails', content)),
            'result_pattern': bool(re.search(r'Result<', content))
        }
        return patterns


class BackendGraphGenerator:
    """Generates backend graph from C# source files."""

    def __init__(self, project_root: str):
        self.root = Path(project_root)
        self.parser = CSharpParser(project_root)
        self.entities = {}
        self.dtos = {}
        self.services = {}
        self.controllers = {}
        self.edges: List[Dict[str, str]] = []

    def process_entities(self):
        """Process entities from Domain layer."""
        entity_dir = self.root / 'src/Koinon.Domain/Entities'
        if not entity_dir.exists():
            print(f"Warning: Entity directory not found: {entity_dir}", file=sys.stderr)
            return

        for file_path in sorted(entity_dir.glob('*.cs')):
            # Skip interfaces (IEntity.cs, IAuditable.cs, etc.)
            # But allow entities starting with I (ImportJob.cs, ImportTemplate.cs)
            class_name = self.parser.extract_class_name_from_file(file_path)
            if class_name.startswith('I') and len(class_name) > 1 and class_name[1].isupper():
                # Interface pattern: IEntity, IAuditable (I + uppercase letter)
                continue
            content = self.parser.read_file(file_path)
            if not content:
                continue

            class_name = self.parser.extract_class_name_from_file(file_path)
            namespace = self.parser.extract_namespace(content)
            if not namespace:
                continue

            properties = self.parser.extract_properties(content)
            navigations = self.parser.extract_navigations(content)
            table_name = self.parser.extract_table_name(class_name, content)

            self.entities[class_name] = {
                'name': class_name,
                'namespace': namespace,
                'table': table_name,
                'properties': properties,
                'navigations': navigations
            }

    def process_dtos(self):
        """Process DTOs from Application layer."""
        dto_dir = self.root / 'src/Koinon.Application/DTOs'
        if not dto_dir.exists():
            print(f"Warning: DTO directory not found: {dto_dir}", file=sys.stderr)
            return

        for file_path in sorted(dto_dir.glob('**/*.cs')):
            content = self.parser.read_file(file_path)
            if not content:
                continue

            namespace = self.parser.extract_namespace(content)
            if not namespace:
                continue

            # Handle multiple DTOs in one file (e.g., PersonDto, PersonSummaryDto)
            # Match any record/class ending in Dto, Request, or Response
            pattern = r'(?:public\s+)?(?:sealed\s+)?(?:record|class|struct)\s+([A-Za-z0-9]+(?:Dto|Request|Response))\b'
            for match in re.finditer(pattern, content):
                dto_name = match.group(1)

                # Extract properties only for this specific DTO
                properties = self.parser.extract_record_properties(content, dto_name)
                linked_entity = self._infer_entity_from_dto_name(dto_name)

                self.dtos[dto_name] = {
                    'name': dto_name,
                    'namespace': namespace,
                    'properties': properties
                }
                if linked_entity:
                    self.dtos[dto_name]['linked_entity'] = linked_entity
                    self.edges.append({
                        'source': dto_name,
                        'target': linked_entity,
                        'relationship': 'maps_to'
                    })

    def _infer_entity_from_dto_name(self, dto_name: str) -> Optional[str]:
        """Infer entity name from DTO name."""
        # Manual mappings for DTOs that don't follow naming conventions
        # Issue #466: Link Person-related DTOs
        # Issue #472: Link utility DTOs (Auth, Search, Import, Export, Label)
        manual_mappings = {
            # Person-related (Issue #466)
            'MyProfileDto': 'Person',
            'UpdateMyProfileRequest': 'Person',
            'DuplicateMatchDto': 'Person',
            'FirstTimeVisitorDto': 'Person',
            'UpdateFollowUpStatusRequest': 'Person',
            'AssignFollowUpRequest': 'Person',
            'CreatePhoneNumberRequest': 'Person',
            'UpdatePhoneNumberRequest': 'Person',

            # Auth DTOs (Issue #472)
            'LoginRequest': 'UserSession',
            'TokenResponse': 'UserSession',
            'ChangePasswordRequest': 'Person',
            'TwoFactorVerifyRequest': 'TwoFactorConfig',
            'TwoFactorSetupDto': 'TwoFactorConfig',
            'TwoFactorStatusDto': 'TwoFactorConfig',

            # Search DTOs (Issue #472) - Cross-cutting
            'GlobalSearchResultDto': 'System',
            'GlobalSearchResponse': 'System',

            # Import/Export DTOs (Issue #472)
            'CsvPreviewDto': 'ImportJob',
            'ImportJobDto': 'ImportJob',
            'ImportTemplateDto': 'ImportTemplate',
            'CreateImportTemplateRequest': 'ImportTemplate',
            'StartImportRequest': 'ImportJob',
            'ValidateImportRequest': 'ImportJob',
            'ExportFieldDto': 'ExportJob',
            'StartExportRequest': 'ExportJob',
            'AuditLogExportRequest': 'AuditLog',

            # Label DTOs (Issue #472)
            'LabelSetDto': 'LabelTemplate',
            'LabelDto': 'LabelTemplate',
            'LabelRequestDto': 'LabelTemplate',
            'BatchLabelRequestDto': 'LabelTemplate',
            'LabelPreviewRequestDto': 'LabelTemplate',
            'LabelPreviewDto': 'LabelTemplate',
            'MergeFieldDto': 'LabelTemplate',

            # Dashboard/Stats DTOs (Issue #472)
            'DashboardStatsDto': 'System',
            'DashboardBatchDto': 'ContributionBatch',
            'UpcomingScheduleDto': 'Schedule',

            # File DTOs (Issue #472)
            'FileMetadataDto': 'BinaryFile',
            'UploadFileRequest': 'BinaryFile',

            # Pickup DTOs (Issue #472)
            'PickupVerificationResultDto': 'PickupLog',
            'VerifyPickupRequest': 'PickupLog',
            'RecordPickupRequest': 'PickupLog',
        }

        if dto_name in manual_mappings:
            entity = manual_mappings[dto_name]
            # Allow "System" as a special marker for cross-cutting DTOs
            if entity == 'System' or entity in self.entities:
                return entity

        # Handle Request pattern: Create{Entity}Request, Update{Entity}Request, etc.
        if dto_name.endswith('Request'):
            base_name = dto_name[:-7]  # Remove 'Request'
            # Try prefixes: Create, Update, Add, Remove, Delete, etc.
            for prefix in ['Create', 'Update', 'Add', 'Remove', 'Delete', 'Upload', 'Import', 'Export', 'Validate']:
                if base_name.startswith(prefix):
                    entity_candidate = base_name[len(prefix):]
                    if entity_candidate in self.entities:
                        return entity_candidate
                    # Try without additional suffix (e.g., CreatePhoneNumberRequest -> PhoneNumber -> Person)
                    for entity_name in self.entities:
                        if entity_candidate.startswith(entity_name):
                            return entity_name

        # Handle Dto suffix
        if dto_name.endswith('Dto'):
            base_name = dto_name[:-3]
            # Check if entity exists
            if base_name in self.entities:
                return base_name
            # Try without suffix (e.g., PersonSummaryDto -> Person)
            for entity_name in self.entities:
                if base_name.startswith(entity_name) and entity_name != base_name:
                    return entity_name

        return None

    def process_services(self):
        """Process services from Application layer."""
        service_dir = self.root / 'src/Koinon.Application/Services'
        interfaces_dir = self.root / 'src/Koinon.Application/Interfaces'

        for directory in [service_dir, interfaces_dir]:
            if not directory.exists():
                continue

            for file_path in sorted(directory.glob('*.cs')):
                content = self.parser.read_file(file_path)
                if not content:
                    continue

                class_name = self.parser.extract_class_name_from_file(file_path)
                namespace = self.parser.extract_namespace(content)
                if not namespace or not class_name.endswith('Service'):
                    continue

                methods = self.parser.extract_methods(content)
                dependencies = self.parser.extract_constructor_dependencies(content)

                self.services[class_name] = {
                    'name': class_name,
                    'namespace': namespace,
                    'methods': methods,
                    'dependencies': dependencies
                }

    def process_controllers(self):
        """Process controllers from API layer."""
        controller_dir = self.root / 'src/Koinon.Api/Controllers'
        if not controller_dir.exists():
            print(f"Warning: Controller directory not found: {controller_dir}", file=sys.stderr)
            return

        for file_path in sorted(controller_dir.glob('*.cs')):
            content = self.parser.read_file(file_path)
            if not content:
                continue

            class_name = self.parser.extract_class_name_from_file(file_path)
            namespace = self.parser.extract_namespace(content)
            if not namespace or not class_name.endswith('Controller'):
                continue

            route = self.parser.extract_route_attribute(content)
            if not route:
                # Try to infer from class name
                controller_base = class_name[:-10]  # Remove 'Controller'
                route = f"api/v1/{self.parser.camel_to_snake(controller_base)}"

            endpoints = self.parser.extract_endpoints(content)
            patterns = self.parser.extract_patterns(content)
            dependencies = self.parser.extract_constructor_dependencies(content)

            self.controllers[class_name] = {
                'name': class_name,
                'namespace': namespace,
                'route': route,
                'endpoints': endpoints,
                'patterns': patterns,
                'dependencies': dependencies
            }

    def build_edges(self):
        """Build relationship edges between nodes."""
        # Controller -> Service dependencies
        for controller_name, controller in self.controllers.items():
            for dep in controller.get('dependencies', []):
                # Try to find matching service
                if dep in self.services:
                    self.edges.append({
                        'source': controller_name,
                        'target': dep,
                        'relationship': 'depends_on'
                    })
                # Also check for interface implementations (e.g., IPersonService -> PersonService)
                else:
                    for service_name in self.services:
                        if dep.lstrip('I') == service_name or f'I{service_name}' == dep:
                            self.edges.append({
                                'source': controller_name,
                                'target': service_name,
                                'relationship': 'depends_on'
                            })
                            break

        # Service -> DTO usage (inferred from return types and parameters)
        for service_name, service in self.services.items():
            for method in service.get('methods', []):
                return_type = method.get('return_type', '')
                # Check if return type contains DTO name
                for dto_name in self.dtos:
                    if dto_name in return_type:
                        self.edges.append({
                            'source': service_name,
                            'target': dto_name,
                            'relationship': 'returns'
                        })
                        break

        # Service -> Entity usage
        for service_name, service in self.services.items():
            for dep in service.get('dependencies', []):
                # Look for repository or entity references
                for entity_name in self.entities:
                    if entity_name in dep:
                        self.edges.append({
                            'source': service_name,
                            'target': entity_name,
                            'relationship': 'uses'
                        })
                        break

    def generate(self) -> Dict[str, Any]:
        """Generate complete backend graph."""
        print("Processing entities...", file=sys.stderr)
        self.process_entities()
        print(f"  Found {len(self.entities)} entities", file=sys.stderr)

        print("Processing DTOs...", file=sys.stderr)
        self.process_dtos()
        print(f"  Found {len(self.dtos)} DTOs", file=sys.stderr)

        print("Processing services...", file=sys.stderr)
        self.process_services()
        print(f"  Found {len(self.services)} services", file=sys.stderr)

        print("Processing controllers...", file=sys.stderr)
        self.process_controllers()
        print(f"  Found {len(self.controllers)} controllers", file=sys.stderr)

        print("Building relationships...", file=sys.stderr)
        self.build_edges()
        print(f"  Found {len(self.edges)} relationships", file=sys.stderr)

        return {
            'version': '1.0',
            'generated_at': datetime.now(timezone.utc).isoformat(),
            'entities': self.entities,
            'dtos': self.dtos,
            'services': self.services,
            'controllers': self.controllers,
            'edges': self.edges,
            'summary': {
                'total_entities': len(self.entities),
                'total_dtos': len(self.dtos),
                'total_services': len(self.services),
                'total_controllers': len(self.controllers),
                'total_relationships': len(self.edges)
            }
        }


def main():
    """Main entry point."""
    import argparse

    parser = argparse.ArgumentParser(
        description='Generate backend architecture graph from C# source files'
    )
    parser.add_argument(
        '--output', '-o',
        default='tools/graph/backend-graph.json',
        help='Output file path (default: tools/graph/backend-graph.json)'
    )
    parser.add_argument(
        '--project-root', '-r',
        default='.',
        help='Project root directory (default: current directory)'
    )

    args = parser.parse_args()

    # Determine project root
    project_root = Path(args.project_root).resolve()
    if not (project_root / 'src').exists():
        print(f"Error: Project root not found at {project_root}", file=sys.stderr)
        print("Expected to find 'src' directory", file=sys.stderr)
        return 1

    # Generate graph
    generator = BackendGraphGenerator(str(project_root))
    graph = generator.generate()

    # Write output
    output_path = project_root / args.output
    output_path.parent.mkdir(parents=True, exist_ok=True)

    try:
        with open(output_path, 'w', encoding='utf-8') as f:
            json.dump(graph, f, indent=2)
        print(f"Graph written to {output_path}", file=sys.stderr)
        return 0
    except Exception as e:
        print(f"Error writing output: {e}", file=sys.stderr)
        return 1


if __name__ == '__main__':
    sys.exit(main())
