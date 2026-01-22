#!/usr/bin/env node

/**
 * Koinon RMS Development MCP Server
 *
 * A custom Model Context Protocol server that provides development validation
 * and architectural guidance for the Koinon RMS project.
 *
 * Features:
 * - Validates naming conventions (snake_case DB, PascalCase C#)
 * - Checks IdKey usage in routes (never integer IDs)
 * - Validates clean architecture dependency rules
 * - Detects legacy anti-patterns
 * - Provides work unit validation
 * - Offers architectural guidance
 * - Queries API graph baseline for architectural patterns
 * - Generates implementation templates for entities, DTOs, services, controllers
 */

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
  ListResourcesRequestSchema,
  ReadResourceRequestSchema,
  ListPromptsRequestSchema,
  GetPromptRequestSchema
} from '@modelcontextprotocol/sdk/types.js';
import { z } from 'zod';
import * as fs from 'fs';
import * as path from 'path';
import { searchRag, getRagStatus, getRagImpactAnalysis } from './rag-client.js';

// Configuration
const PROJECT_ROOT = process.env.KOINON_PROJECT_ROOT || '/home/mbrewer/projects/koinon-rms';

// Type definitions for graph baseline
interface GraphBaseline {
  version: string;
  generated_at: string;
  entities: Record<string, any>;
  controllers: Record<string, any>;
  dtos: Record<string, any>;
  services: Record<string, any>;
  [key: string]: any;
}

// Cached graph baseline
let cachedGraphBaseline: GraphBaseline | undefined;

// Load and cache graph baseline
function loadGraphBaseline(): GraphBaseline {
  if (cachedGraphBaseline) {
    return cachedGraphBaseline;
  }

  const graphPath = path.join(PROJECT_ROOT, 'tools/graph/graph-baseline.json');
  const graphContent = fs.readFileSync(graphPath, 'utf-8');
  cachedGraphBaseline = JSON.parse(graphContent);
  return cachedGraphBaseline as GraphBaseline;
}
// Validation schemas
const NamingConventionSchema = z.object({
  type: z.enum(['database', 'csharp', 'typescript', 'route']),
  names: z.array(z.string())
});

const RouteValidationSchema = z.object({
  routes: z.array(z.string())
});

const DependencyValidationSchema = z.object({
  project: z.enum(['Domain', 'Application', 'Infrastructure', 'Api']),
  dependencies: z.array(z.string())
});

const CodePatternSchema = z.object({
  code: z.string(),
  language: z.enum(['csharp', 'typescript'])
});

const WorkUnitSchema = z.object({
  workUnitId: z.string(),
  completedItems: z.array(z.string())
});

const QueryApiGraphSchema = z.object({
  query: z.enum(['get_controller_pattern', 'get_entity_chain', 'list_inconsistencies', 'validate_new_controller']),
  entityName: z.string().optional()
});

const ImplementationTemplateSchema = z.object({
  type: z.enum(['entity', 'dto', 'service', 'controller']),
  entityName: z.string()
});

const ImpactAnalysisSchema = z.object({
  file_path: z.string()
});

// RAG tool schemas
const RagSearchSchema = z.object({
  query: z.string(),
  filter_layer: z.enum(['Domain', 'Application', 'Infrastructure', 'API', 'Frontend', 'all']).optional(),
  filter_type: z.enum(['Entity', 'DTO', 'Service', 'Controller', 'Component', 'Hook', 'Other', 'all']).optional(),
  limit: z.number().min(1).max(50).optional().default(10)
});

const RagImpactSchema = z.object({
  file_path: z.string(),
  change_description: z.string().optional(),
  include_tests: z.boolean().optional().default(true)
});

// Type definitions for impact analysis
interface ImpactAnalysisResult {
  affected_files: {
    path: string;
    layer: string;
    relationship: string;
  }[];
  affected_work_units: {
    id: string;
    name: string;
    reason: string;
  }[];
  impact_summary: {
    total_files: number;
    high_impact: boolean;
    layers_affected: string[];
  };
}

interface FileAnalysis {
  path: string;
  layer: string;
  entityName?: string;
  dtoName?: string;
  serviceName?: string;
  controllerName?: string;
  componentName?: string;
  hookName?: string;
  apiFunctionName?: string;
}


// Validation functions
function validateDatabaseNaming(names: string[]): { valid: boolean; issues: string[] } {
  const issues: string[] = [];
  const snakeCaseRegex = /^[a-z][a-z0-9]*(_[a-z0-9]+)*$/;

  for (const name of names) {
    if (!snakeCaseRegex.test(name)) {
      issues.push(`"${name}" - Database names must be snake_case (lowercase with underscores)`);
    }
  }

  return { valid: issues.length === 0, issues };
}

function validateCSharpNaming(names: string[]): { valid: boolean; issues: string[] } {
  const issues: string[] = [];
  const pascalCaseRegex = /^[A-Z][a-zA-Z0-9]*$/;

  for (const name of names) {
    if (!pascalCaseRegex.test(name)) {
      issues.push(`"${name}" - C# class/property names must be PascalCase`);
    }
  }

  return { valid: issues.length === 0, issues };
}

function validateTypeScriptNaming(names: string[]): { valid: boolean; issues: string[] } {
  const issues: string[] = [];
  const camelCaseRegex = /^[a-z][a-zA-Z0-9]*$/;
  const pascalCaseRegex = /^[A-Z][a-zA-Z0-9]*$/;

  for (const name of names) {
    // TypeScript can be camelCase for variables or PascalCase for types/interfaces
    if (!camelCaseRegex.test(name) && !pascalCaseRegex.test(name)) {
      issues.push(`"${name}" - TypeScript names must be camelCase (variables) or PascalCase (types/interfaces)`);
    }
  }

  return { valid: issues.length === 0, issues };
}

function validateRoutes(routes: string[]): { valid: boolean; issues: string[] } {
  const issues: string[] = [];

  // Pattern to detect integer IDs in routes (e.g., /api/person/123)
  const integerIdPattern = /\/\d+(?:\/|$)/;

  for (const route of routes) {
    if (integerIdPattern.test(route)) {
      issues.push(`"${route}" - Routes must use IdKey, never integer IDs`);
    }

    // Check for proper versioning
    if (!route.startsWith('/api/v')) {
      issues.push(`"${route}" - API routes should start with /api/v{version}/`);
    }
  }

  return { valid: issues.length === 0, issues };
}

function validateDependencies(project: string, dependencies: string[]): { valid: boolean; issues: string[] } {
  const issues: string[] = [];

  const allowedDependencies: Record<string, string[]> = {
    'Domain': [], // Domain has no dependencies on other layers
    'Application': ['Domain'],
    'Infrastructure': ['Domain', 'Application'],
    'Api': ['Domain', 'Application', 'Infrastructure']
  };

  const allowed = allowedDependencies[project] || [];

  for (const dep of dependencies) {
    if (!allowed.includes(dep)) {
      issues.push(`"${project}" cannot depend on "${dep}". Allowed dependencies: ${allowed.join(', ') || 'none'}`);
    }
  }

  return { valid: issues.length === 0, issues };
}

function detectAntiPatterns(code: string, language: 'csharp' | 'typescript'): { patterns: string[] } {
  const patterns: string[] = [];

  if (language === 'csharp') {
    // C# anti-patterns
    if (/\brunat\s*=\s*["']server["']/i.test(code)) {
      patterns.push('LEGACY: Server controls (runat="server") detected - use React components instead');
    }

    if (/ViewState/i.test(code)) {
      patterns.push('LEGACY: ViewState detected - use client-side state management');
    }

    if (/Page_Load|Page_Init/i.test(code)) {
      patterns.push('LEGACY: Page lifecycle methods detected - use MediatR handlers');
    }

    if (/\bDbContext\b/.test(code) && !/Infrastructure/i.test(code)) {
      patterns.push('ARCHITECTURE: DbContext should only be used in Infrastructure layer');
    }

    if (/\.Result\b|\.Wait\(\)/.test(code)) {
      patterns.push('PERFORMANCE: Synchronous database calls detected - use async/await');
    }

    if (/\/api\/[^/]+\/\d+/.test(code)) {
      patterns.push('API: Integer IDs in routes detected - use IdKey instead');
    }
  }

  if (language === 'typescript') {
    // TypeScript anti-patterns
    if (/:\s*any\b/.test(code)) {
      patterns.push('TYPE SAFETY: "any" type detected - use strict typing');
    }

    if (/class\s+\w+\s+extends\s+(React\.)?Component/i.test(code)) {
      patterns.push('LEGACY: Class components detected - use functional components only');
    }

    if (/componentDidMount|componentWillUnmount|shouldComponentUpdate/i.test(code)) {
      patterns.push('LEGACY: Lifecycle methods detected - use hooks instead');
    }
  }

  return { patterns };
}

function getArchitectureGuidance(topic: string): string {
  const guidance: Record<string, string> = {
    'entity': `
Entity Design Guidelines:
- All entities inherit from Entity base class
- Use Guid for external references (exposed as IdKey)
- Never expose integer IDs in APIs
- Implement audit fields (CreatedDateTime, ModifiedDateTime, etc.)
- Use required modifier for non-nullable properties
- Navigation properties should be virtual for lazy loading
`,
    'api': `
API Design Guidelines:
- Base path: /api/v1/
- Use IdKey in URLs, never integer IDs
- Return standard response envelopes
- Use async/await for all operations
- Implement proper error handling
- Use CancellationToken for long-running operations
`,
    'database': `
Database Conventions:
- Table names: snake_case (e.g., group_member)
- Column names: snake_case (e.g., first_name)
- C# properties: PascalCase (e.g., FirstName)
- Primary keys: id (int, identity)
- Foreign keys: {entity}_id (e.g., person_id)
- Use migrations for all schema changes
`,
    'frontend': `
Frontend Guidelines:
- Functional components only (no class components)
- Use TanStack Query for server state
- Strict TypeScript (no 'any')
- Custom hooks for shared logic
- TailwindCSS for styling
- Touch targets minimum 48px for kiosk
`,
    'clean-architecture': `
Clean Architecture Rules:
- Domain: No dependencies on other layers
- Application: Depends only on Domain
- Infrastructure: Depends on Domain and Application
- Api: Can depend on all layers
- Never reference Infrastructure from Application
- Use interfaces in Domain, implement in Infrastructure
`
  };

  return guidance[topic] || 'Topic not found. Available topics: entity, api, database, frontend, clean-architecture';
}

// API Graph query functions
function getControllerPattern(entityName: string): any {
  const baseline = loadGraphBaseline();
  
  // Find controller that handles this entity
  const controllerName = entityName + 'Controller';
  const controller = baseline.controllers[controllerName];
  
  if (!controller) {
    return {
      found: false,
      entity: entityName,
      message: `Controller not found for entity ${entityName}`
    };
  }

  return {
    found: true,
    entity: entityName,
    controller_name: controller.name,
    route: controller.route,
    patterns: controller.patterns,
    endpoints: controller.endpoints,
    dependencies: controller.dependencies
  };
}

function getEntityChain(entityName: string): any {
  const baseline = loadGraphBaseline();
  
  // Find entity
  const entity = baseline.entities[entityName];
  if (!entity) {
    return {
      found: false,
      entity: entityName,
      message: `Entity ${entityName} not found`
    };
  }

  // Find DTOs linked to this entity
  const linkedDtos: any[] = [];
  for (const [dtoName, dto] of Object.entries(baseline.dtos)) {
    if (dto.linked_entity === entityName) {
      linkedDtos.push({
        name: dtoName,
        namespace: dto.namespace,
        properties: Object.keys(dto.properties || {})
      });
    }
  }

  // Find service that handles this entity (heuristic: service name matches entity)
  const serviceName = entityName + 'Service';
  const service = baseline.services[serviceName];

  // Find controller
  const controllerName = entityName + 'Controller';
  const controller = baseline.controllers[controllerName];

  return {
    found: true,
    entity: {
      name: entity.name,
      namespace: entity.namespace,
      table: entity.table
    },
    dtos: linkedDtos,
    service: service ? {
      name: service.name,
      namespace: service.namespace,
      methods: service.methods.map((m: any) => ({ name: m.name, return_type: m.return_type }))
    } : null,
    controller: controller ? {
      name: controller.name,
      route: controller.route,
      endpoints: controller.endpoints.length
    } : null
  };
}

function listInconsistencies(): any {
  const baseline = loadGraphBaseline();
  const inconsistencies: any[] = [];

  // Check controllers with idkey_routes=false when they should use idKey
  for (const [controllerName, controller] of Object.entries(baseline.controllers)) {
    if (!controller.patterns?.idkey_routes) {
      inconsistencies.push({
        type: 'IDKEY_NOT_ENFORCED',
        controller: controllerName,
        route: controller.route,
        issue: 'Controller should enforce IdKey routes for all endpoints'
      });
    }
  }

  // Check DTOs with integer Id properties
  for (const [dtoName, dto] of Object.entries(baseline.dtos)) {
    if (dto.properties && typeof dto.properties === 'object') {
      for (const [propName, propType] of Object.entries(dto.properties)) {
        if (propName === 'Id' && propType === 'int') {
          inconsistencies.push({
            type: 'EXPOSED_INTEGER_ID',
            dto: dtoName,
            property: propName,
            issue: 'DTOs should never expose integer IDs - use IdKey instead'
          });
        }
      }
    }
  }

  // Check for orphaned DTOs without linked_entity
  for (const [dtoName, dto] of Object.entries(baseline.dtos)) {
    if (!dto.linked_entity) {
      inconsistencies.push({
        type: 'ORPHANED_DTO',
        dto: dtoName,
        issue: 'DTO has no linked_entity specified'
      });
    }
  }

  return {
    total_inconsistencies: inconsistencies.length,
    inconsistencies
  };
}

function validateNewController(name: string): any {
  const issues: string[] = [];
  const warnings: string[] = [];

  // Check PascalCase ending in Controller
  const pascalCaseRegex = /^[A-Z][a-zA-Z0-9]*Controller$/;
  if (!pascalCaseRegex.test(name)) {
    issues.push(`Controller name "${name}" must be PascalCase ending with "Controller" (e.g., PersonController)`);
  }

  // Extract resource name from controller
  const resourceMatch = name.match(/^([A-Z][a-zA-Z0-9]*)Controller$/);
  if (resourceMatch) {
    const resourceName = resourceMatch[1];
    
    // Suggest route pattern
    const suggestedRoute = `api/v1/${resourceName.toLowerCase()}s`;
    
    return {
      valid: issues.length === 0,
      name,
      issues,
      warnings,
      resource_name: resourceName,
      suggested_route: suggestedRoute,
      expected_patterns: {
        response_envelope: true,
        idkey_routes: true,
        problem_details: true,
        result_pattern: false
      }
    };
  }

  return {
    valid: false,
    name,
    issues,
    message: 'Could not parse controller name'
  };
}

function getImplementationTemplate(type: string, entityName: string): any {
  const pascalCase = entityName.charAt(0).toUpperCase() + entityName.slice(1);
  const conventions = [
    'Never expose integer IDs in DTOs or routes',
    'Use IdKey for URL routes',
    'All async operations must use async/await',
    'Use CancellationToken for long-running operations',
    'Database tables and columns use snake_case',
    'C# classes and properties use PascalCase',
    'Private fields must be _camelCase',
    'Implement proper error handling',
    'Use required modifier for non-nullable properties'
  ];

  switch (type) {
    case 'entity': {
      const template = `namespace Koinon.Domain.Entities;

public class ${pascalCase} : Entity
{
    // TODO: Add properties here
    // Remember: Use 'required' modifier for non-nullable properties
    // Example: public required string FirstName { get; set; }
}`;
      
      return {
        type: 'entity',
        entityName: pascalCase,
        template,
        filePath: `src/Koinon.Domain/Entities/${pascalCase}.cs`,
        conventions: [
          ...conventions,
          'Inherit from Entity base class',
          'Implement all properties with appropriate types',
          'Use navigation properties as virtual for lazy loading',
          'Include audit fields (CreatedDateTime, ModifiedDateTime)'
        ]
      };
    }

    case 'dto': {
      const template = `namespace Koinon.Application.DTOs;

public record ${pascalCase}Dto
{
    public required string IdKey { get; init; }
    
    // TODO: Add properties here
    // Remember: Use 'init' for record properties (immutability)
    // Example: public required string FirstName { get; init; }
}`;
      
      return {
        type: 'dto',
        entityName: pascalCase,
        dtoName: `${pascalCase}Dto`,
        template,
        filePath: `src/Koinon.Application/DTOs/${pascalCase}Dto.cs`,
        conventions: [
          ...conventions,
          'Use record type for DTOs (immutability)',
          'Always include IdKey property',
          'Never expose integer Id property',
          'Use init-only setters for immutability',
          'Mark required properties with required modifier'
        ]
      };
    }

    case 'service': {
      const templateInterface = `namespace Koinon.Application.Services;

public interface I${pascalCase}Service
{
    Task<${pascalCase}Dto?> GetByIdKeyAsync(string idKey, CancellationToken ct = default);
    Task<IEnumerable<${pascalCase}Dto>> GetAllAsync(CancellationToken ct = default);
    Task<${pascalCase}Dto> CreateAsync(${pascalCase}Dto dto, CancellationToken ct = default);
    Task<${pascalCase}Dto?> UpdateAsync(string idKey, ${pascalCase}Dto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(string idKey, CancellationToken ct = default);
}`;

      const templateImplementation = `namespace Koinon.Application.Services;

public class ${pascalCase}Service : I${pascalCase}Service
{
    private readonly I${pascalCase}Repository _repository;
    
    public ${pascalCase}Service(I${pascalCase}Repository repository)
    {
        _repository = repository;
    }
    
    public async Task<${pascalCase}Dto?> GetByIdKeyAsync(string idKey, CancellationToken ct = default)
    {
        // TODO: Implement
        throw new NotImplementedException();
    }
    
    public async Task<IEnumerable<${pascalCase}Dto>> GetAllAsync(CancellationToken ct = default)
    {
        // TODO: Implement
        throw new NotImplementedException();
    }
    
    public async Task<${pascalCase}Dto> CreateAsync(${pascalCase}Dto dto, CancellationToken ct = default)
    {
        // TODO: Implement
        throw new NotImplementedException();
    }
    
    public async Task<${pascalCase}Dto?> UpdateAsync(string idKey, ${pascalCase}Dto dto, CancellationToken ct = default)
    {
        // TODO: Implement
        throw new NotImplementedException();
    }
    
    public async Task<bool> DeleteAsync(string idKey, CancellationToken ct = default)
    {
        // TODO: Implement
        throw new NotImplementedException();
    }
}`;
      
      return {
        type: 'service',
        entityName: pascalCase,
        serviceName: `I${pascalCase}Service`,
        templates: {
          interface: templateInterface,
          implementation: templateImplementation
        },
        filePaths: {
          interface: `src/Koinon.Application/Services/I${pascalCase}Service.cs`,
          implementation: `src/Koinon.Application/Services/${pascalCase}Service.cs`
        },
        conventions: [
          ...conventions,
          'Service should depend on repository interface',
          'All methods must be async',
          'Use CancellationToken for all async operations',
          'Return DTOs, not entities',
          'Map entities to DTOs in service',
          'Implement validation in service layer',
          'Never expose repository directly to controllers'
        ]
      };
    }

    case 'controller': {
      const resourcePlural = pascalCase.toLowerCase() + 's';
      const template = `namespace Koinon.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Koinon.Application.DTOs;
using Koinon.Application.Services;

[ApiController]
[Route("api/v1/${resourcePlural}")]
public class ${pascalCase}Controller : ControllerBase
{
    private readonly I${pascalCase}Service _service;
    
    public ${pascalCase}Controller(I${pascalCase}Service service)
    {
        _service = service;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<${pascalCase}Dto>>> GetAll(CancellationToken ct)
    {
        var result = await _service.GetAllAsync(ct);
        return Ok(result);
    }
    
    [HttpGet("{idKey}")]
    public async Task<ActionResult<${pascalCase}Dto>> GetByIdKey(string idKey, CancellationToken ct)
    {
        var result = await _service.GetByIdKeyAsync(idKey, ct);
        if (result is null) 
            return NotFound();
        return Ok(result);
    }
    
    [HttpPost]
    public async Task<ActionResult<${pascalCase}Dto>> Create([FromBody] ${pascalCase}Dto dto, CancellationToken ct)
    {
        var result = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetByIdKey), new { idKey = result.IdKey }, result);
    }
    
    [HttpPut("{idKey}")]
    public async Task<ActionResult<${pascalCase}Dto>> Update(string idKey, [FromBody] ${pascalCase}Dto dto, CancellationToken ct)
    {
        var result = await _service.UpdateAsync(idKey, dto, ct);
        if (result is null)
            return NotFound();
        return Ok(result);
    }
    
    [HttpDelete("{idKey}")]
    public async Task<IActionResult> Delete(string idKey, CancellationToken ct)
    {
        var success = await _service.DeleteAsync(idKey, ct);
        if (!success)
            return NotFound();
        return NoContent();
    }
}`;
      
      return {
        type: 'controller',
        entityName: pascalCase,
        controllerName: `${pascalCase}Controller`,
        template,
        filePath: `src/Koinon.Api/Controllers/${pascalCase}Controller.cs`,
        resourcePlural,
        conventions: [
          ...conventions,
          'Controller depends on service interface, not implementation',
          'Use attribute routing with [Route(...)]',
          'Follow REST conventions (GET, POST, PUT, DELETE)',
          'Use IdKey in URL parameters, never integer IDs',
          'Return appropriate HTTP status codes',
          'Include CancellationToken in all async methods',
          'Use [FromBody] for request body parameters',
          'No business logic in controller - delegate to service',
          'Use CreatedAtAction for POST responses'
        ]
      };
    }

    default:
      throw new Error(`Unknown template type: ${type}`);
  }
}


// Impact Analysis Tool Implementation
function analyzeFileImpact(filePath: string): ImpactAnalysisResult {
  const baseline = loadGraphBaseline();
  const affected: FileAnalysis[] = [];
  const workUnits: Map<string, { id: string; name: string; reason: string }> = new Map();

  const fileAnalysis = parseFilePath(filePath);
  
  if (!fileAnalysis) {
    return {
      affected_files: [],
      affected_work_units: [],
      impact_summary: { total_files: 0, high_impact: false, layers_affected: [] }
    };
  }

  affected.push(fileAnalysis);

  if (fileAnalysis.layer === 'Domain' && fileAnalysis.entityName) {
    const entityName = fileAnalysis.entityName;
    
    const linkedDtos = Object.entries(baseline.dtos).filter(
      ([_, dto]: [string, any]) => dto.linked_entity === entityName
    );
    
    for (const [dtoName] of linkedDtos) {
      affected.push({
        path: `src/Koinon.Application/DTOs/${dtoName}.cs`,
        layer: 'Application',
        dtoName: dtoName,
        entityName: entityName
      });
    }

    const linkedServices = Object.entries(baseline.services).filter(
      ([_, service]: [string, any]) => {
        const methods = service.methods || [];
        return methods.some((m: any) => 
          m.return_type?.includes(entityName) || 
          m.parameters?.some((p: any) => p.type?.includes(entityName))
        );
      }
    );

    for (const [serviceName] of linkedServices) {
      affected.push({
        path: `src/Koinon.Application/Services/${serviceName}.cs`,
        layer: 'Application',
        serviceName: serviceName,
        entityName: entityName
      });

      workUnits.set(`WU-2.${serviceName}`, {
        id: `WU-2.${serviceName}`,
        name: `Service: ${serviceName}`,
        reason: `Service references ${entityName} entity`
      });
    }

    const linkedControllers = Object.entries(baseline.controllers).filter(
      ([_, controller]: [string, any]) => {
        const endpoints = controller.endpoints || [];
        return endpoints.some((e: any) => 
          linkedDtos.some(([dtoName]) => e.response_type?.includes(dtoName))
        );
      }
    );

    for (const [controllerName] of linkedControllers) {
      affected.push({
        path: `src/Koinon.Api/Controllers/${controllerName}.cs`,
        layer: 'Api',
        controllerName: controllerName,
        entityName: entityName
      });

      workUnits.set(`WU-3.${controllerName}`, {
        id: `WU-3.${controllerName}`,
        name: `Controller: ${controllerName}`,
        reason: `Controller exposes ${entityName} through API endpoints`
      });
    }

    const frontendConnections = findFrontendConnections(baseline, entityName, linkedDtos);
    for (const component of frontendConnections) {
      affected.push(component);
      const componentName = component.componentName || component.hookName || component.apiFunctionName || 'unknown';
      workUnits.set(`WU-4.${componentName}`, {
        id: `WU-4.${componentName}`,
        name: `Frontend: ${componentName}`,
        reason: `Component/hook uses API connected to ${entityName}`
      });
    }
  } else if (fileAnalysis.layer === 'Application' && fileAnalysis.dtoName) {
    const dtoName = fileAnalysis.dtoName;
    
    const linkedControllers = Object.entries(baseline.controllers).filter(
      ([_, controller]: [string, any]) => {
        const endpoints = controller.endpoints || [];
        return endpoints.some((e: any) => 
          e.response_type?.includes(dtoName) || e.request_type?.includes(dtoName)
        );
      }
    );

    for (const [controllerName] of linkedControllers) {
      if (!affected.some(f => f.path.includes(controllerName))) {
        affected.push({
          path: `src/Koinon.Api/Controllers/${controllerName}.cs`,
          layer: 'Api',
          controllerName: controllerName,
          dtoName: dtoName
        });

        workUnits.set(`WU-3.${controllerName}`, {
          id: `WU-3.${controllerName}`,
          name: `Controller: ${controllerName}`,
          reason: `Controller uses ${dtoName} in endpoints`
        });
      }
    }

    const frontendConnections = findFrontendConnectionsForDto(baseline, dtoName);
    for (const component of frontendConnections) {
      affected.push(component);
      const componentName = component.componentName || component.hookName || component.apiFunctionName || 'unknown';
      workUnits.set(`WU-4.${componentName}`, {
        id: `WU-4.${componentName}`,
        name: `Frontend: ${componentName}`,
        reason: `Component uses API function that returns ${dtoName}`
      });
    }
  } else if (fileAnalysis.layer === 'Api' && fileAnalysis.controllerName) {
    const controllerName = fileAnalysis.controllerName;
    const frontendConnections = findFrontendConnectionsForController(baseline, controllerName);
    for (const component of frontendConnections) {
      affected.push(component);
      const componentName = component.componentName || component.hookName || component.apiFunctionName || 'unknown';
      workUnits.set(`WU-4.${componentName}`, {
        id: `WU-4.${componentName}`,
        name: `Frontend: ${componentName}`,
        reason: `Component calls API endpoint from ${controllerName}`
      });
    }
  }

  const layersAffected = [...new Set(affected.map(f => f.layer))];
  const highImpact = affected.length > 5 || layersAffected.length > 2;

  if (fileAnalysis.entityName) {
    workUnits.set(`WU-1.2.${fileAnalysis.entityName}`, {
      id: `WU-1.2.${fileAnalysis.entityName}`,
      name: `Entity: ${fileAnalysis.entityName}`,
      reason: 'Domain entity definition'
    });
  }

  return {
    affected_files: affected.map(f => ({
      path: f.path,
      layer: f.layer,
      relationship: f.entityName ? `dependent_on_${f.entityName}` : 
                   f.dtoName ? `uses_${f.dtoName}` :
                   f.serviceName ? `implements_${f.serviceName}` :
                   f.controllerName ? `serves_${f.controllerName}` : 'related'
    })),
    affected_work_units: Array.from(workUnits.values()),
    impact_summary: { total_files: affected.length, high_impact: highImpact, layers_affected: layersAffected }
  };
}

function parseFilePath(filePath: string): FileAnalysis | null {
  const domainEntityMatch = filePath.match(/src\/Koinon\.Domain\/Entities\/(.+?)\.cs$/);
  if (domainEntityMatch) {
    return { path: filePath, layer: 'Domain', entityName: domainEntityMatch[1] };
  }

  const dtoMatch = filePath.match(/src\/Koinon\.Application\/DTOs\/(.+?)\.cs$/);
  if (dtoMatch) {
    return { path: filePath, layer: 'Application', dtoName: dtoMatch[1] };
  }

  const serviceMatch = filePath.match(/src\/Koinon\.Application\/Services\/(.+?)(?:Service)?\.cs$/);
  if (serviceMatch) {
    return { path: filePath, layer: 'Application', serviceName: serviceMatch[1] + 'Service' };
  }

  const controllerMatch = filePath.match(/src\/Koinon\.Api\/Controllers\/(.+?)\.cs$/);
  if (controllerMatch) {
    return { path: filePath, layer: 'Api', controllerName: controllerMatch[1] };
  }

  const apiMatch = filePath.match(/src\/web\/src\/services\/api\/(.+?)\.ts$/);
  if (apiMatch) {
    return { path: filePath, layer: 'Frontend', apiFunctionName: apiMatch[1] };
  }

  const hookMatch = filePath.match(/src\/web\/src\/hooks\/(use.+?)\.ts$/);
  if (hookMatch) {
    return { path: filePath, layer: 'Frontend', hookName: hookMatch[1] };
  }

  const componentMatch = filePath.match(/src\/web\/src\/components\/(.+?)\.tsx$/);
  if (componentMatch) {
    return { path: filePath, layer: 'Frontend', componentName: componentMatch[1] };
  }

  return null;
}

function findFrontendConnections(baseline: GraphBaseline, entityName: string, linkedDtos: [string, any][]): FileAnalysis[] {
  const connections: FileAnalysis[] = [];
  const dtoNames = linkedDtos.map(([name]) => name);

  const relevantApiFunctions = (baseline.api_functions || []).filter(
    (fn: any) => dtoNames.some(dto => fn.return_type?.includes(dto))
  );

  for (const fn of relevantApiFunctions) {
    connections.push({
      path: `src/web/src/services/api/${fn.name}.ts`,
      layer: 'Frontend',
      apiFunctionName: fn.name
    });

    const hookEdges = (baseline.edges || []).filter(
      (e: any) => e.target === fn.name && baseline.hooks?.[e.source]
    );

    for (const edge of hookEdges) {
      connections.push({
        path: `src/web/src/hooks/${edge.source}.ts`,
        layer: 'Frontend',
        hookName: edge.source
      });

      const componentEdges = (baseline.edges || []).filter(
        (e: any) => e.target === edge.source && baseline.components?.[e.source]
      );

      for (const cEdge of componentEdges) {
        connections.push({
          path: `src/web/src/components/${cEdge.source}.tsx`,
          layer: 'Frontend',
          componentName: cEdge.source
        });
      }
    }
  }

  return connections;
}

function findFrontendConnectionsForDto(baseline: GraphBaseline, dtoName: string): FileAnalysis[] {
  const connections: FileAnalysis[] = [];

  const relevantApiFunctions = (baseline.api_functions || []).filter(
    (fn: any) => fn.return_type?.includes(dtoName)
  );

  for (const fn of relevantApiFunctions) {
    connections.push({
      path: `src/web/src/services/api/${fn.name}.ts`,
      layer: 'Frontend',
      apiFunctionName: fn.name
    });

    const hookEdges = (baseline.edges || []).filter(
      (e: any) => e.target === fn.name && baseline.hooks?.[e.source]
    );

    for (const edge of hookEdges) {
      connections.push({
        path: `src/web/src/hooks/${edge.source}.ts`,
        layer: 'Frontend',
        hookName: edge.source
      });

      const componentEdges = (baseline.edges || []).filter(
        (e: any) => e.target === edge.source
      );

      for (const cEdge of componentEdges) {
        connections.push({
          path: `src/web/src/components/${cEdge.source}.tsx`,
          layer: 'Frontend',
          componentName: cEdge.source
        });
      }
    }
  }

  return connections;
}

function findFrontendConnectionsForController(baseline: GraphBaseline, controllerName: string): FileAnalysis[] {
  const connections: FileAnalysis[] = [];
  const resourceName = controllerName.replace('Controller', '');

  const relevantApiFunctions = (baseline.api_functions || []).filter(
    (fn: any) => fn.endpoint?.includes(resourceName.toLowerCase())
  );

  for (const fn of relevantApiFunctions) {
    connections.push({
      path: `src/web/src/services/api/${fn.name}.ts`,
      layer: 'Frontend',
      apiFunctionName: fn.name
    });

    const hookEdges = (baseline.edges || []).filter(
      (e: any) => e.target === fn.name && baseline.hooks?.[e.source]
    );

    for (const edge of hookEdges) {
      connections.push({
        path: `src/web/src/hooks/${edge.source}.ts`,
        layer: 'Frontend',
        hookName: edge.source
      });

      const componentEdges = (baseline.edges || []).filter(
        (e: any) => e.target === edge.source
      );

      for (const cEdge of componentEdges) {
        connections.push({
          path: `src/web/src/components/${cEdge.source}.tsx`,
          layer: 'Frontend',
          componentName: cEdge.source
        });
      }
    }
  }

  return connections;
}

// Create the MCP server
const server = new Server(
  {
    name: 'koinon-dev-server',
    version: '1.0.0',
  },
  {
    capabilities: {
      tools: {},
      resources: {},
      prompts: {}
    },
  }
);

// Register tools
server.setRequestHandler(ListToolsRequestSchema, async () => {
  return {
    tools: [
      {
        name: 'validate_naming',
        description: 'Validates naming conventions for database, C#, TypeScript, or routes',
        inputSchema: {
          type: 'object',
          properties: {
            type: {
              type: 'string',
              enum: ['database', 'csharp', 'typescript', 'route'],
              description: 'Type of naming to validate'
            },
            names: {
              type: 'array',
              items: { type: 'string' },
              description: 'Array of names to validate'
            }
          },
          required: ['type', 'names']
        }
      },
      {
        name: 'validate_routes',
        description: 'Validates API routes to ensure they use IdKey and not integer IDs',
        inputSchema: {
          type: 'object',
          properties: {
            routes: {
              type: 'array',
              items: { type: 'string' },
              description: 'Array of route patterns to validate'
            }
          },
          required: ['routes']
        }
      },
      {
        name: 'validate_dependencies',
        description: 'Validates clean architecture dependency rules',
        inputSchema: {
          type: 'object',
          properties: {
            project: {
              type: 'string',
              enum: ['Domain', 'Application', 'Infrastructure', 'Api'],
              description: 'Project layer to validate'
            },
            dependencies: {
              type: 'array',
              items: { type: 'string' },
              description: 'Array of dependencies to validate'
            }
          },
          required: ['project', 'dependencies']
        }
      },
      {
        name: 'detect_antipatterns',
        description: 'Detects legacy patterns and anti-patterns in code',
        inputSchema: {
          type: 'object',
          properties: {
            code: {
              type: 'string',
              description: 'Code snippet to analyze'
            },
            language: {
              type: 'string',
              enum: ['csharp', 'typescript'],
              description: 'Programming language of the code'
            }
          },
          required: ['code', 'language']
        }
      },
      {
        name: 'get_architecture_guidance',
        description: 'Retrieves architectural guidance for specific topics',
        inputSchema: {
          type: 'object',
          properties: {
            topic: {
              type: 'string',
              enum: ['entity', 'api', 'database', 'frontend', 'clean-architecture'],
              description: 'Topic to get guidance on'
            }
          },
          required: ['topic']
        }
      },
      {
        name: 'query_api_graph',
        description: 'Queries the API graph baseline for architectural patterns, entity chains, and consistency checks',
        inputSchema: {
          type: 'object',
          properties: {
            query: {
              type: 'string',
              enum: ['get_controller_pattern', 'get_entity_chain', 'list_inconsistencies', 'validate_new_controller'],
              description: 'Type of query to execute'
            },
            entityName: {
              type: 'string',
              description: 'Entity name (required for get_controller_pattern, get_entity_chain, validate_new_controller)'
            }
          },
          required: ['query']
        }
      },
      {
        name: 'get_implementation_template',
        description: 'Generates implementation templates for entities, DTOs, services, and controllers following Koinon RMS conventions',
        inputSchema: {
          type: 'object',
          properties: {
            type: {
              type: 'string',
              enum: ['entity', 'dto', 'service', 'controller'],
              description: 'Type of template to generate'
            },
            entityName: {
              type: 'string',
              description: 'Entity name (will be converted to PascalCase)'
            }
          },
          required: ['type', 'entityName']
        }
      },
      {
        name: 'get_impact_analysis',
        description: 'Analyzes the impact of changes to a file across layers and work units, showing dependent files and affected functionality',
        inputSchema: {
          type: 'object',
          properties: {
            file_path: {
              type: 'string',
              description: 'The file path to analyze (e.g., src/Koinon.Domain/Entities/Person.cs)'
            }
          },
          required: ['file_path']
        }
      },
      // RAG-powered semantic search tools
      {
        name: 'rag_search',
        description: 'Semantic code search using RAG (Retrieval-Augmented Generation). Find similar patterns, related code, or answer architecture questions using natural language. Returns relevant code snippets with file paths, layers, and similarity scores. Gracefully degrades if RAG infrastructure is unavailable.',
        inputSchema: {
          type: 'object',
          properties: {
            query: {
              type: 'string',
              description: 'Natural language query (e.g., "person validation with email rules", "authentication service pattern")'
            },
            filter_layer: {
              type: 'string',
              enum: ['Domain', 'Application', 'Infrastructure', 'API', 'Frontend', 'all'],
              description: 'Filter results by architectural layer (optional, default: all)'
            },
            filter_type: {
              type: 'string',
              enum: ['Entity', 'DTO', 'Service', 'Controller', 'Component', 'Hook', 'Other', 'all'],
              description: 'Filter results by code type (optional, default: all)'
            },
            limit: {
              type: 'number',
              description: 'Maximum results to return (default: 10, max: 50)'
            }
          },
          required: ['query']
        }
      },
      {
        name: 'rag_impact_analysis',
        description: 'RAG-enhanced impact analysis. Finds semantically related code AND related tests for a file using vector similarity. Combines with structural graph analysis for comprehensive change impact assessment. Use before making changes to understand full-stack implications.',
        inputSchema: {
          type: 'object',
          properties: {
            file_path: {
              type: 'string',
              description: 'File path to analyze (e.g., src/Koinon.Domain/Entities/Person.cs)'
            },
            change_description: {
              type: 'string',
              description: 'Optional description of planned changes for better semantic matching (e.g., "adding email validation")'
            },
            include_tests: {
              type: 'boolean',
              description: 'Search for related tests (default: true)'
            }
          },
          required: ['file_path']
        }
      },
      {
        name: 'rag_index_status',
        description: 'Check RAG index health and statistics. Returns availability of Qdrant and Ollama, collection info, and chunk count. Use to diagnose RAG issues.',
        inputSchema: {
          type: 'object',
          properties: {},
          required: []
        }
      }
    ]
  };
});

// Handle tool calls
server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;

  try {
    switch (name) {
      case 'validate_naming': {
        const { type, names } = NamingConventionSchema.parse(args);
        let result;

        switch (type) {
          case 'database':
            result = validateDatabaseNaming(names);
            break;
          case 'csharp':
            result = validateCSharpNaming(names);
            break;
          case 'typescript':
            result = validateTypeScriptNaming(names);
            break;
          case 'route':
            result = validateRoutes(names);
            break;
        }

        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify(result, null, 2)
            }
          ]
        };
      }

      case 'validate_routes': {
        const { routes } = RouteValidationSchema.parse(args);
        const result = validateRoutes(routes);

        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify(result, null, 2)
            }
          ]
        };
      }

      case 'validate_dependencies': {
        const { project, dependencies } = DependencyValidationSchema.parse(args);
        const result = validateDependencies(project, dependencies);

        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify(result, null, 2)
            }
          ]
        };
      }

      case 'detect_antipatterns': {
        const { code, language } = CodePatternSchema.parse(args);
        const result = detectAntiPatterns(code, language);

        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify(result, null, 2)
            }
          ]
        };
      }

      case 'get_architecture_guidance': {
        const { topic } = z.object({ topic: z.string() }).parse(args);
        const guidance = getArchitectureGuidance(topic);

        return {
          content: [
            {
              type: 'text',
              text: guidance
            }
          ]
        };
      }

      case 'query_api_graph': {
        const { query, entityName } = QueryApiGraphSchema.parse(args);
        let result;

        switch (query) {
          case 'get_controller_pattern':
            if (!entityName) {
              throw new Error('entityName is required for get_controller_pattern query');
            }
            result = getControllerPattern(entityName);
            break;

          case 'get_entity_chain':
            if (!entityName) {
              throw new Error('entityName is required for get_entity_chain query');
            }
            result = getEntityChain(entityName);
            break;

          case 'list_inconsistencies':
            result = listInconsistencies();
            break;

          case 'validate_new_controller':
            if (!entityName) {
              throw new Error('entityName is required for validate_new_controller query');
            }
            result = validateNewController(entityName);
            break;

          default:
            throw new Error(`Unknown graph query: ${query}`);
        }

        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify(result, null, 2)
            }
          ]
        };
      }

      case 'get_implementation_template': {
        const { type, entityName } = ImplementationTemplateSchema.parse(args);
        const result = getImplementationTemplate(type, entityName);

        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify(result, null, 2)
            }
          ]
        };
      }
      case 'get_impact_analysis': {
        const { file_path } = ImpactAnalysisSchema.parse(args);
        const result = analyzeFileImpact(file_path);

        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify(result, null, 2)
            }
          ]
        };
      }

      // RAG tool handlers
      case 'rag_search': {
        const { query, filter_layer, filter_type, limit } = RagSearchSchema.parse(args);
        const result = await searchRag(query, filter_layer, filter_type, limit);

        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify(result, null, 2)
            }
          ]
        };
      }

      case 'rag_impact_analysis': {
        const { file_path, change_description, include_tests } = RagImpactSchema.parse(args);
        const result = await getRagImpactAnalysis(file_path, change_description, include_tests);

        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify(result, null, 2)
            }
          ]
        };
      }

      case 'rag_index_status': {
        const result = await getRagStatus();

        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify(result, null, 2)
            }
          ]
        };
      }

      default:
        throw new Error(`Unknown tool: ${name}`);
    }
  } catch (error) {
    const errorMessage = error instanceof Error ? error.message : String(error);
    return {
      content: [
        {
          type: 'text',
          text: `Error: ${errorMessage}`
        }
      ],
      isError: true
    };
  }
});

// Register resources (documentation files)
server.setRequestHandler(ListResourcesRequestSchema, async () => {
  return {
    resources: [
      {
        uri: 'koinon://docs/architecture',
        name: 'Architecture Documentation',
        mimeType: 'text/markdown',
        description: 'High-level architecture decisions and patterns'
      },
      {
        uri: 'koinon://docs/conventions',
        name: 'Coding Conventions',
        mimeType: 'text/markdown',
        description: 'Naming conventions and code standards'
      },
      {
        uri: 'koinon://docs/entity-design',
        name: 'Entity Design Guidelines',
        mimeType: 'text/markdown',
        description: 'How to design and implement entities'
      },
      {
        uri: 'koinon://docs/api-design',
        name: 'API Design Guidelines',
        mimeType: 'text/markdown',
        description: 'REST API design patterns and conventions'
      }
    ]
  };
});

server.setRequestHandler(ReadResourceRequestSchema, async (request) => {
  const uri = request.params.uri;

  const content: Record<string, string> = {
    'koinon://docs/architecture': getArchitectureGuidance('clean-architecture'),
    'koinon://docs/conventions': `
# Naming Conventions

## Database
- Table names: snake_case (e.g., group_member)
- Column names: snake_case (e.g., first_name)
- Primary keys: id (int, identity)
- Foreign keys: {entity}_id (e.g., person_id)

## C#
- Classes, Properties, Methods: PascalCase
- Private fields: _camelCase (with underscore prefix)
- Local variables, parameters: camelCase

## TypeScript
- Variables, functions: camelCase
- Interfaces, Types, Classes: PascalCase
- Constants: UPPER_SNAKE_CASE

## Routes
- Use IdKey in URLs, never integer IDs
- Example: /api/v1/people/{idKey} NOT /api/v1/people/123
`,
    'koinon://docs/entity-design': getArchitectureGuidance('entity'),
    'koinon://docs/api-design': getArchitectureGuidance('api')
  };

  const text = content[uri];
  if (!text) {
    throw new Error(`Resource not found: ${uri}`);
  }

  return {
    contents: [
      {
        uri,
        mimeType: 'text/markdown',
        text
      }
    ]
  };
});

// Register prompts for common development tasks
server.setRequestHandler(ListPromptsRequestSchema, async () => {
  return {
    prompts: [
      {
        name: 'review_entity',
        description: 'Review an entity implementation for compliance',
        arguments: [
          {
            name: 'entity_code',
            description: 'The C# entity class code to review',
            required: true
          }
        ]
      },
      {
        name: 'review_api_endpoint',
        description: 'Review an API endpoint implementation',
        arguments: [
          {
            name: 'endpoint_code',
            description: 'The controller endpoint code to review',
            required: true
          }
        ]
      },
      {
        name: 'check_work_unit',
        description: 'Check work unit completion criteria',
        arguments: [
          {
            name: 'work_unit_id',
            description: 'Work unit ID (e.g., WU-1.1.1)',
            required: true
          }
        ]
      }
    ]
  };
});

server.setRequestHandler(GetPromptRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;

  switch (name) {
    case 'review_entity':
      return {
        messages: [
          {
            role: 'user',
            content: {
              type: 'text',
              text: `Review this entity implementation for Koinon RMS compliance:

${args?.entity_code}

Check for:
1. Inherits from Entity base class
2. Uses PascalCase for properties
3. Required modifier for non-nullable properties
4. Proper navigation properties
5. No business logic in entity
6. Audit fields present`
            }
          }
        ]
      };

    case 'review_api_endpoint':
      return {
        messages: [
          {
            role: 'user',
            content: {
              type: 'text',
              text: `Review this API endpoint for Koinon RMS compliance:

${args?.endpoint_code}

Check for:
1. Uses IdKey in routes, not integer IDs
2. Async/await pattern
3. Returns standard response envelope
4. Proper error handling
5. CancellationToken support
6. No business logic in controller`
            }
          }
        ]
      };

    case 'check_work_unit':
      return {
        messages: [
          {
            role: 'user',
            content: {
              type: 'text',
              text: `Check completion criteria for work unit: ${args?.work_unit_id}

Please verify:
1. All acceptance criteria met
2. Code compiles with zero warnings
3. Unit tests pass
4. Follows naming conventions
5. No TODO comments
6. Documentation updated if needed`
            }
          }
        ]
      };

    default:
      throw new Error(`Unknown prompt: ${name}`);
  }
});

// Start the server
async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
  console.error('Koinon RMS Development MCP Server running on stdio');
}

main().catch((error) => {
  console.error('Server error:', error);
  process.exit(1);
});
