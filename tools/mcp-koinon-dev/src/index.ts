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

// Configuration
const PROJECT_ROOT = process.env.KOINON_PROJECT_ROOT || '/home/mbrewer/projects/koinon-rms';

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
