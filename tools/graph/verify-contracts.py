#!/usr/bin/env python3
"""
Contract verification script for the Koinon RMS architecture graph.

Performs consistency checks on the graph baseline to ensure:
1. Response envelopes are used in controllers
2. DTOs do not expose integer IDs
3. API routes use idKey not id
4. React components wrap API calls through hooks
5. Frontend types align with backend DTOs

Exit codes:
  0 - All checks passed
  1 - One or more blocking checks failed
  2 - Script error (invalid input, missing file)
"""

import sys
import json
import re
from pathlib import Path
from typing import Dict, List, Tuple, Optional, Set


class ContractViolation:
    """Represents a single contract violation."""

    def __init__(self, check_name: str, severity: str, item: str, detail: str):
        self.check_name = check_name
        self.severity = severity  # FAIL, WARN
        self.item = item  # The thing being checked (controller name, DTO name, etc)
        self.detail = detail  # Specific violation message

    def __str__(self):
        icon = "✗" if self.severity == "FAIL" else "⚠"
        return f"  {icon} {self.item}: {self.detail}"


class ContractVerifier:
    """Verifies contract consistency in the architecture graph."""

    # DTOs that intentionally don't have frontend types (internal-only DTOs)
    # or have frontend types with different names
    FRONTEND_ALLOWLIST = {
        # Internal-only DTOs (not exposed to frontend)
        "CreateNotificationDto",
        "BatchCheckinResultDto",
        "BulkUpdatePreferencesDto",
        "BulkMarkAttendanceResultDto",
        "BatchLabelRequestDto",
        "CapacityOverrideRequestDto",
        "AttendanceAnalyticsDto",
        "AttendanceTrendDto",
        "AttendanceByGroupDto",
        "AttendanceSummaryDto",
        "CreateGroupMemberDto",
        "UpdateGroupMemberDto",
        "ImportFamilyResultDto",
        "OfflineCheckinDto",
        "SystemStatisticsDto",
        # Admin export/audit (admin-only operations)
        "AuditLogExportRequest",
        "ExportJobDto",
        "StartExportRequest",
        # Email/SMS internal handling
        "EmailAttachmentDto",
        "QueuedSmsDto",
        "TwilioWebhookDto",
        # Giving batch operations (admin-only)
        "BatchStatementRequest",
        "BatchFilterRequest",
        # Reporting subsystem (admin-only, not yet implemented in frontend)
        "ReportDefinitionDto",
        "CreateReportDefinitionRequest",
        "UpdateReportDefinitionRequest",
        "ReportRunDto",
        "RunReportRequest",
        "ReportScheduleDto",
        "CreateReportScheduleRequest",
        "UpdateReportScheduleRequest",
        # Security role management (admin-only)
        "SecurityRoleDto",
        # Check-in supervisor operations
        "SupervisorReprintRequest",
        # DTOs with different-named frontend equivalents
        # MyFamilyMemberDto → FamilyMemberDto (profile.ts)
        "MyFamilyMemberDto",
        # MyInvolvementGroupDto → GroupMembershipDto (profile.ts)
        "MyInvolvementGroupDto",
        # PersonGroupMembershipDto → GroupMembershipDto (profile.ts)
        "PersonGroupMembershipDto",
        # DashboardBatchDto → embedded in DashboardStatsDto, not exposed separately
        "DashboardBatchDto",
        # CreateFamilyAddressRequest → CreateAddressRequest (different name in frontend)
        "CreateFamilyAddressRequest",
    }

    def __init__(self, graph_path: str):
        self.graph_path = Path(graph_path)
        self.graph = None
        self.violations: Dict[str, List[ContractViolation]] = {
            "check_1": [],
            "check_2": [],
            "check_3": [],
            "check_4": [],
            "check_5": [],
        }
        self.load_graph()

    def load_graph(self):
        """Load and validate the graph JSON file."""
        if not self.graph_path.exists():
            print(f"ERROR: Graph file not found: {self.graph_path}")
            sys.exit(2)

        try:
            with open(self.graph_path, 'r') as f:
                self.graph = json.load(f)
        except json.JSONDecodeError as e:
            print(f"ERROR: Invalid JSON in {self.graph_path}: {e}")
            sys.exit(2)

        # Verify required sections exist
        required = ["controllers", "dtos", "components", "hooks"]
        for section in required:
            if section not in self.graph:
                print(f"ERROR: Graph missing required section: {section}")
                sys.exit(2)

    def check_response_envelopes(self):
        """
        Check 1: Controllers should declare response envelope pattern.

        Verifies that controllers consistently mark their response_envelope pattern
        in the patterns object. This documents whether responses use { data: T }
        or direct responses.
        """
        print("\nCheck 1: Response Envelope Documentation")
        print("-" * 50)

        controllers = self.graph.get("controllers", {})
        controllers_with_pattern = 0

        for controller_name, controller in controllers.items():
            patterns = controller.get("patterns", {})
            has_envelope_doc = "response_envelope" in patterns

            if not has_envelope_doc:
                violation = ContractViolation(
                    "Check 1",
                    "FAIL",
                    controller_name,
                    "Missing response_envelope documentation in patterns"
                )
                self.violations["check_1"].append(violation)
            else:
                controllers_with_pattern += 1

        self._print_results("check_1", "Response envelope documentation")

    def check_no_integer_ids_in_dtos(self):
        """
        Check 2: DTOs should never expose integer ID properties.

        DTOs should use IdKey (string) not Id (int) for public ID exposure.
        This check identifies any DTO with a property that looks like an exposed integer ID.
        """
        print("\nCheck 2: No Integer IDs in DTOs")
        print("-" * 50)

        dtos = self.graph.get("dtos", {})

        # Pattern to detect integer ID properties
        # Check for properties named "Id" with type containing "int"
        for dto_name, dto in dtos.items():
            properties = dto.get("properties", {})

            for prop_name, prop_type in properties.items():
                # Flag public integer Id property
                if prop_name == "Id" and isinstance(prop_type, str):
                    if "int" in prop_type.lower():
                        violation = ContractViolation(
                            "Check 2",
                            "FAIL",
                            dto_name,
                            f"Exposes integer ID: {prop_name}: {prop_type} (should use IdKey string)"
                        )
                        self.violations["check_2"].append(violation)

        self._print_results("check_2", "Integer IDs in DTOs")

    def check_idkey_routes(self):
        """
        Check 3: API routes must use {idKey} not {id}.

        All routes that include ID parameters should use the idKey pattern
        for consistency with the API design standard.
        """
        print("\nCheck 3: IdKey Routes")
        print("-" * 50)

        controllers = self.graph.get("controllers", {})

        for controller_name, controller in controllers.items():
            endpoints = controller.get("endpoints", [])

            for endpoint in endpoints:
                route = endpoint.get("route", "")

                # Check for {id} pattern (incorrect)
                if "{id}" in route:
                    violation = ContractViolation(
                        "Check 3",
                        "FAIL",
                        f"{controller_name}.{endpoint.get('name')}",
                        f"Route uses {{id}} instead of {{idKey}}: {route}"
                    )
                    self.violations["check_3"].append(violation)

        self._print_results("check_3", "IdKey route patterns")

    def check_hook_wrapping(self):
        """
        Check 4: React components should not directly call fetch/apiClient.

        Components must use custom hooks (from hooks/) that wrap API calls.
        The graph indicates this through the apiCallsDirectly flag on components.
        """
        print("\nCheck 4: Hook Wrapping")
        print("-" * 50)

        components = self.graph.get("components", {})
        direct_api_calls = []

        for component_name, component in components.items():
            api_calls_directly = component.get("apiCallsDirectly", False)

            if api_calls_directly:
                direct_api_calls.append(component_name)
                violation = ContractViolation(
                    "Check 4",
                    "FAIL",
                    component_name,
                    f"Component makes direct API calls instead of using hooks"
                )
                self.violations["check_4"].append(violation)

        self._print_results("check_4", "Hook wrapping pattern")

    def _pascal_to_camel_case(self, pascal: str) -> str:
        """Convert PascalCase to camelCase."""
        if not pascal:
            return pascal
        return pascal[0].lower() + pascal[1:]

    def _find_matching_type(self, dto_name: str, available_types: Set[str]) -> Optional[str]:
        """
        Find a matching frontend type for a DTO.

        Tries in order:
        1. Exact match (PersonDto -> PersonDto)
        2. Without Dto suffix (PersonDto -> Person)
        3. Case-insensitive fallback
        """
        # Try exact match
        if dto_name in available_types:
            return dto_name

        # Try without Dto suffix
        if dto_name.endswith("Dto"):
            without_suffix = dto_name[:-3]
            if without_suffix in available_types:
                return without_suffix

        # Try case-insensitive fallback (use normalized name without Dto suffix)
        base_name = dto_name[:-3] if dto_name.endswith("Dto") else dto_name
        base_lower = base_name.lower()
        for available in available_types:
            if available.lower() == base_lower:
                return available

        return None

    def _compare_properties(self, dto_name: str, dto_properties: Dict,
                           type_properties: Dict) -> List[str]:
        """
        Compare DTO and frontend type properties.

        Returns list of mismatches. Converts C# PascalCase to TypeScript camelCase.
        """
        mismatches = []

        # Check each DTO property exists in frontend type (accounting for case conversion)
        for dto_prop, dto_type in dto_properties.items():
            # Skip common base properties that may not be exposed
            if dto_prop in ["Id", "IdKey", "Guid", "CreatedDateTime", "ModifiedDateTime"]:
                continue

            # Skip backend-internal properties (file streams sent via FormData, not JSON)
            if dto_type in ["Stream", "FileStream", "IFormFile"]:
                continue

            # Convert to camelCase for comparison
            expected_ts_prop = self._pascal_to_camel_case(dto_prop)

            # Check if property exists in any form
            found = False
            for ts_prop in type_properties.keys():
                if ts_prop.lower() == expected_ts_prop.lower():
                    found = True
                    break

            if not found:
                mismatches.append(
                    f"Missing property: DTO has {dto_prop} ({dto_type}) but frontend type lacks {expected_ts_prop}"
                )

        return mismatches

    def check_type_alignment(self):
        """
        Check 5: Frontend types should exist for backend DTOs.

        Validates that:
        1. Backend DTOs have corresponding frontend types
        2. Frontend type properties match DTO properties (accounting for case conversion)
        3. Frontend types without DTOs are documented (warnings only)

        Gracefully handles when frontend types aren't available yet.
        """
        print("\nCheck 5: Type Alignment (Frontend ↔ Backend)")
        print("-" * 50)

        dtos = self.graph.get("dtos", {})
        types = self.graph.get("types", {})

        # If no types section exists, check is informational
        if not types:
            print(f"  [INFO] Frontend types not yet generated in graph")
            print(f"         Backend has {len(dtos)} DTOs")
            print("         Run 'npm run graph:frontend' to generate frontend type information.")
            return

        available_type_names: Set[str] = set(types.keys())
        dtos_without_types = []
        property_mismatches = []

        # Check each DTO has a matching frontend type
        for dto_name, dto_info in dtos.items():
            # Skip internal DTOs that intentionally don't have frontend types
            if dto_name in self.FRONTEND_ALLOWLIST:
                continue

            # Skip RequestDto and ResponseDto types (usually internal)
            if dto_name.endswith("RequestDto") or dto_name.endswith("ResponseDto"):
                continue

            matching_type = self._find_matching_type(dto_name, available_type_names)

            if not matching_type:
                violation = ContractViolation(
                    "Check 5",
                    "FAIL",
                    dto_name,
                    f"No corresponding frontend type found"
                )
                self.violations["check_5"].append(violation)
                dtos_without_types.append(dto_name)
            else:
                # Check property alignment
                dto_props = dto_info.get("properties", {})
                type_props = types[matching_type].get("properties", {})

                mismatches = self._compare_properties(dto_name, dto_props, type_props)

                if mismatches:
                    for mismatch in mismatches:
                        violation = ContractViolation(
                            "Check 5",
                            "FAIL",
                            f"{dto_name} ↔ {matching_type}",
                            mismatch
                        )
                        self.violations["check_5"].append(violation)
                    property_mismatches.append(dto_name)

        # Check for frontend types without corresponding DTOs (warning only)
        for type_name in available_type_names:
            # Skip utility types and enums
            if type_name.startswith("_") or "Enum" in type_name:
                continue

            matching_dto = self._find_matching_type(type_name, set(dtos.keys()))

            if not matching_dto:
                # This is expected for frontend-only types, just warn
                violation = ContractViolation(
                    "Check 5",
                    "WARN",
                    type_name,
                    f"Frontend type has no corresponding backend DTO (may be frontend-only)"
                )
                self.violations["check_5"].append(violation)

        # Print summary
        if dtos_without_types or property_mismatches:
            print(f"  [INFO] Type alignment analysis:")
            print(f"         DTOs checked: {len(dtos) - len([d for d in dtos if d in self.FRONTEND_ALLOWLIST or d.endswith('RequestDto') or d.endswith('ResponseDto')])}")
            print(f"         Frontend types available: {len(types)}")
            if dtos_without_types:
                print(f"         DTOs without frontend types: {len(dtos_without_types)}")
            if property_mismatches:
                print(f"         DTOs with property mismatches: {len(property_mismatches)}")

        self._print_results("check_5", "Type alignment")

    def _print_results(self, check_key: str, check_name: str, is_warning: bool = False):
        """Print results for a check."""
        violations = self.violations[check_key]

        if not violations:
            print(f"  ✓ {check_name}: PASS")
        else:
            status = "WARN" if is_warning else "FAIL"
            print(f"  ✗ {check_name}: {status} ({len(violations)} violations)")
            for violation in violations[:10]:  # Show first 10 violations
                print(f"    {violation}")
            if len(violations) > 10:
                print(f"    ... and {len(violations) - 10} more")

    def verify_all(self) -> int:
        """Run all contract verification checks."""
        print("=" * 70)
        print("KOINON RMS CONTRACT VERIFICATION")
        print("=" * 70)

        self.check_response_envelopes()
        self.check_no_integer_ids_in_dtos()
        self.check_idkey_routes()
        self.check_hook_wrapping()
        self.check_type_alignment()

        return self._summarize()

    def _summarize(self) -> int:
        """Summarize results and return exit code."""
        print("\n" + "=" * 70)
        print("SUMMARY")
        print("=" * 70)

        blocking_violations = sum(
            len([v for v in violations if v.severity == "FAIL"])
            for violations in self.violations.values()
        )

        warning_violations = sum(
            len([v for v in violations if v.severity == "WARN"])
            for violations in self.violations.values()
        )

        if blocking_violations > 0:
            print(f"\n✗ VERIFICATION FAILED")
            print(f"  Blocking violations: {blocking_violations}")
            if warning_violations > 0:
                print(f"  Warnings: {warning_violations}")
            return 1
        else:
            print(f"\n✓ VERIFICATION PASSED")
            if warning_violations > 0:
                print(f"  Warnings: {warning_violations}")
            return 0


def main():
    """Main entry point."""
    # Determine graph file path
    script_dir = Path(__file__).parent
    graph_path = script_dir / "graph-baseline.json"

    # Allow override via command line
    if len(sys.argv) > 1:
        graph_path = Path(sys.argv[1])

    verifier = ContractVerifier(str(graph_path))
    exit_code = verifier.verify_all()

    sys.exit(exit_code)


if __name__ == "__main__":
    main()
