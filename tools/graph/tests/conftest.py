"""
Pytest configuration and fixtures for graph generator tests.

Provides fixtures to load test files from tools/graph/fixtures/.
"""

import pytest
from pathlib import Path


@pytest.fixture
def fixtures_dir():
    """Returns path to fixtures directory."""
    return Path(__file__).parent.parent / 'fixtures'


@pytest.fixture
def valid_fixtures_dir(fixtures_dir):
    """Returns path to valid fixtures directory."""
    return fixtures_dir / 'valid'


@pytest.fixture
def invalid_fixtures_dir(fixtures_dir):
    """Returns path to invalid fixtures directory."""
    return fixtures_dir / 'invalid'


@pytest.fixture
def edge_case_fixtures_dir(fixtures_dir):
    """Returns path to edge-case fixtures directory."""
    return fixtures_dir / 'edge-cases'


@pytest.fixture
def person_entity_file(valid_fixtures_dir):
    """Returns path to PersonEntity.cs fixture."""
    return valid_fixtures_dir / 'PersonEntity.cs'


@pytest.fixture
def person_entity_content(person_entity_file):
    """Returns content of PersonEntity.cs fixture."""
    return person_entity_file.read_text(encoding='utf-8')


@pytest.fixture
def person_dto_file(valid_fixtures_dir):
    """Returns path to PersonDto.cs fixture."""
    return valid_fixtures_dir / 'PersonDto.cs'


@pytest.fixture
def person_dto_content(person_dto_file):
    """Returns content of PersonDto.cs fixture."""
    return person_dto_file.read_text(encoding='utf-8')


@pytest.fixture
def person_service_file(valid_fixtures_dir):
    """Returns path to PersonService.cs fixture."""
    return valid_fixtures_dir / 'PersonService.cs'


@pytest.fixture
def person_service_content(person_service_file):
    """Returns content of PersonService.cs fixture."""
    return person_service_file.read_text(encoding='utf-8')


@pytest.fixture
def people_controller_file(valid_fixtures_dir):
    """Returns path to PeopleController.cs fixture."""
    return valid_fixtures_dir / 'PeopleController.cs'


@pytest.fixture
def people_controller_content(people_controller_file):
    """Returns content of PeopleController.cs fixture."""
    return people_controller_file.read_text(encoding='utf-8')


@pytest.fixture
def unusual_formatting_file(edge_case_fixtures_dir):
    """Returns path to UnusualFormatting.cs fixture."""
    return edge_case_fixtures_dir / 'UnusualFormatting.cs'


@pytest.fixture
def unusual_formatting_content(unusual_formatting_file):
    """Returns content of UnusualFormatting.cs fixture."""
    return unusual_formatting_file.read_text(encoding='utf-8')


@pytest.fixture
def exposed_id_dto_file(invalid_fixtures_dir):
    """Returns path to ExposedIdDto.cs fixture."""
    return invalid_fixtures_dir / 'ExposedIdDto.cs'


@pytest.fixture
def exposed_id_dto_content(exposed_id_dto_file):
    """Returns content of ExposedIdDto.cs fixture."""
    return exposed_id_dto_file.read_text(encoding='utf-8')


@pytest.fixture
def empty_file(edge_case_fixtures_dir):
    """Returns path to EmptyFile.cs fixture."""
    return edge_case_fixtures_dir / 'EmptyFile.cs'


@pytest.fixture
def syntax_error_file(edge_case_fixtures_dir):
    """Returns path to SyntaxError.cs fixture."""
    return edge_case_fixtures_dir / 'SyntaxError.cs'


# Graph merger fixtures

@pytest.fixture
def sample_backend_graph():
    """Returns sample backend graph data."""
    return {
        "version": "1.0",
        "generated_at": "2025-12-28T00:00:00+00:00",
        "entities": {
            "Person": {
                "name": "Person",
                "namespace": "Koinon.Domain.Entities",
                "table": "person",
                "properties": {
                    "FirstName": "string",
                    "LastName": "string",
                    "Email": "string?"
                },
                "navigations": []
            }
        },
        "dtos": {
            "PersonDto": {
                "name": "PersonDto",
                "namespace": "Koinon.Application.DTOs",
                "properties": {
                    "FirstName": "string",
                    "LastName": "string",
                    "Email": "string?"
                },
                "linked_entity": "Person"
            },
            "OrphanDto": {
                "name": "OrphanDto",
                "namespace": "Koinon.Application.DTOs",
                "properties": {
                    "Value": "string"
                },
                "linked_entity": "Orphan"
            }
        },
        "services": {
            "IPersonService": {
                "name": "IPersonService",
                "namespace": "Koinon.Application.Interfaces"
            }
        },
        "controllers": {
            "PeopleController": {
                "name": "PeopleController",
                "namespace": "Koinon.Api.Controllers",
                "route": "api/v1/people",
                "endpoints": [
                    {
                        "name": "GetPerson",
                        "method": "GET",
                        "route": "{idKey}",
                        "request_type": None,
                        "response_type": "PersonDto",
                        "requires_auth": True,
                        "required_roles": []
                    },
                    {
                        "name": "ListPeople",
                        "method": "GET",
                        "route": "",
                        "request_type": None,
                        "response_type": "List<PersonDto>",
                        "requires_auth": True,
                        "required_roles": []
                    },
                    {
                        "name": "OrphanEndpoint",
                        "method": "POST",
                        "route": "orphan",
                        "request_type": None,
                        "response_type": None,
                        "requires_auth": True,
                        "required_roles": []
                    }
                ],
                "patterns": {
                    "response_envelope": True,
                    "idkey_routes": True,
                    "problem_details": True
                },
                "dependencies": ["IPersonService"]
            }
        },
        "edges": [
            {"from": "PersonDto", "to": "Person", "type": "maps_to"},
            {"from": "PeopleController", "to": "PersonDto", "type": "uses"}
        ]
    }


@pytest.fixture
def sample_frontend_graph():
    """Returns sample frontend graph data."""
    return {
        "version": "1.0.0",
        "generated_at": "2025-12-28T00:00:00.000Z",
        "types": {
            "Person": {
                "name": "Person",
                "kind": "interface",
                "properties": {
                    "firstName": "string",
                    "lastName": "string",
                    "email": "string | null"
                },
                "path": "services/api/types.ts"
            },
            "OrphanType": {
                "name": "OrphanType",
                "kind": "interface",
                "properties": {
                    "data": "string"
                },
                "path": "services/api/types.ts"
            }
        },
        "api_functions": {
            "getPerson": {
                "name": "getPerson",
                "path": "services/api/people.ts",
                "endpoint": "/api/v1/people/{idKey}",
                "method": "GET",
                "responseType": "Person"
            },
            "listPeople": {
                "name": "listPeople",
                "path": "services/api/people.ts",
                "endpoint": "/api/v1/people",
                "method": "GET",
                "responseType": "Person[]"
            }
        },
        "hooks": {
            "usePerson": {
                "name": "usePerson",
                "path": "hooks/usePerson.ts",
                "dependencies": ["getPerson"]
            }
        },
        "components": {
            "PersonCard": {
                "name": "PersonCard",
                "path": "components/PersonCard.tsx",
                "dependencies": ["usePerson"]
            }
        },
        "edges": [
            {"from": "PersonCard", "to": "usePerson", "type": "uses"},
            {"from": "usePerson", "to": "getPerson", "type": "calls"}
        ]
    }


@pytest.fixture
def sample_backend_graph_with_mismatch():
    """Returns backend graph with property mismatches."""
    return {
        "version": "1.0",
        "generated_at": "2025-12-28T00:00:00+00:00",
        "entities": {},
        "dtos": {
            "PersonDto": {
                "name": "PersonDto",
                "namespace": "Koinon.Application.DTOs",
                "properties": {
                    "FirstName": "string",
                    "LastName": "string",
                    "Email": "string?",
                    "PhoneNumber": "string?"
                },
                "linked_entity": "Person"
            }
        },
        "services": {},
        "controllers": {},
        "edges": []
    }


@pytest.fixture
def sample_frontend_graph_with_mismatch():
    """Returns frontend graph with property mismatches."""
    return {
        "version": "1.0.0",
        "generated_at": "2025-12-28T00:00:00.000Z",
        "types": {
            "Person": {
                "name": "Person",
                "kind": "interface",
                "properties": {
                    "firstName": "string",
                    "lastName": "string",
                    "email": "string | null"
                },
                "path": "services/api/types.ts"
            }
        },
        "api_functions": {},
        "hooks": {},
        "components": {},
        "edges": []
    }
