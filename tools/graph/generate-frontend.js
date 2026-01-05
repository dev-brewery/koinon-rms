#!/usr/bin/env node
/**
 * TypeScript Frontend Graph Generator
 * 
 * Parses TypeScript/React source files to extract:
 * - Type definitions (interfaces, types) from types.ts
 * - API functions from services/api
 * - React hooks from hooks
 * - React components from components directory (optional)
 */

const fs = require('fs');
const path = require('path');

// ============================================================================
// Argument Parsing
// ============================================================================

/**
 * Parse command-line arguments
 * Returns { srcDir, outputDir } with defaults
 */
function parseArgs() {
  const args = process.argv.slice(2);
  let srcDir = path.join(process.cwd(), 'src/web/src');
  let outputDir = path.join(process.cwd(), 'tools/graph');

  // Parse arguments
  for (let i = 0; i < args.length; i++) {
    if (args[i] === '--src-dir' && i + 1 < args.length) {
      srcDir = path.resolve(args[i + 1]);
      i++; // Skip next arg since we processed it
    } else if (args[i] === '--output-dir' && i + 1 < args.length) {
      outputDir = path.resolve(args[i + 1]);
      i++; // Skip next arg since we processed it
    }
  }

  return { srcDir, outputDir };
}
// ============================================================================
// Configuration
// ============================================================================

const { srcDir: WEB_SRC_DIR, outputDir: OUTPUT_DIR_BASE } = parseArgs();
const TYPES_FILE = path.join(WEB_SRC_DIR, 'services/api/types.ts');
const TYPES_DIR = path.join(WEB_SRC_DIR, 'types');
const SERVICES_API_DIR = path.join(WEB_SRC_DIR, 'services/api');
const HOOKS_DIR = path.join(WEB_SRC_DIR, 'hooks');
const COMPONENTS_DIR = path.join(WEB_SRC_DIR, 'components');
const OUTPUT_DIR = OUTPUT_DIR_BASE;
const OUTPUT_FILE = path.join(OUTPUT_DIR, 'frontend-graph.json');

// ============================================================================
// Type Parsing
// ============================================================================

/**
 * Extract the body of an interface/type with balanced braces
 * @param {string} content - File content starting after opening brace
 * @returns {string} The body content between braces
 */
function extractBalancedBody(content) {
  let braceCount = 1;
  let pos = 0;

  while (pos < content.length && braceCount > 0) {
    if (content[pos] === '{') {
      braceCount++;
    } else if (content[pos] === '}') {
      braceCount--;
    }
    pos++;
  }

  return content.substring(0, pos - 1);
}

/**
 * Extract interface/type definitions from a TypeScript file
 * @param {string} content - File content
 * @param {string} filePath - Relative path for tracking
 */
function parseTypes(content, filePath = 'services/api/types.ts') {
  const types = {};

  // Match: export interface Name { ... } with balanced braces
  const interfaceStartRegex = /export\s+interface\s+(\w+)\s*(?:extends\s+[\w<>,\s]+)?\s*\{/g;
  let match;

  while ((match = interfaceStartRegex.exec(content)) !== null) {
    const name = match[1];
    const startPos = match.index + match[0].length;
    const body = extractBalancedBody(content.substring(startPos));
    const properties = parseProperties(body);

    types[name] = {
      name,
      kind: 'interface',
      properties,
      path: filePath,
    };
  }

  // Match: export type Name = ...
  const typeRegex = /export\s+type\s+(\w+)\s*=\s*([^;]+);/g;
  while ((match = typeRegex.exec(content)) !== null) {
    const [, name, typeStr] = match;
    types[name] = {
      name,
      kind: 'type',
      properties: {}, // Type aliases don't have properties
      path: filePath,
    };
  }

  // Match: export enum Name { values }
  const enumRegex = /export\s+enum\s+(\w+)\s*\{([^}]+)\}/g;
  while ((match = enumRegex.exec(content)) !== null) {
    const [, name] = match;
    types[name] = {
      name,
      kind: 'enum',
      properties: {},
      path: filePath,
    };
  }

  return types;
}

/**
 * Scan types directory and parse all .ts files
 * @param {string} dir - Directory to scan
 * @returns {Object} Combined types from all files
 */
function parseTypesFromDirectory(dir) {
  const allTypes = {};

  if (!fs.existsSync(dir)) {
    return allTypes;
  }

  // Skip index.ts (barrel export) and template.ts (local storage utilities)
  const skipFiles = new Set(['index.ts', 'template.ts']);

  const files = fs.readdirSync(dir)
    .filter(f => f.endsWith('.ts') && !skipFiles.has(f));

  for (const file of files) {
    const filePath = path.join(dir, file);
    try {
      const content = fs.readFileSync(filePath, 'utf-8');
      const relativePath = `types/${file}`;
      const fileTypes = parseTypes(content, relativePath);

      // Merge types, avoiding duplicates
      for (const [name, type] of Object.entries(fileTypes)) {
        if (!allTypes[name]) {
          allTypes[name] = type;
        }
      }
    } catch (err) {
      console.log(`  Warning: Could not read ${file}: ${err.message}`);
    }
  }

  return allTypes;
}

/**
 * Strip comments from TypeScript code
 */
function stripComments(code) {
  // Remove block comments (/** ... */ and /* ... */)
  let result = code.replace(/\/\*[\s\S]*?\*\//g, '');
  // Remove single-line comments (// ...)
  result = result.replace(/\/\/[^\n]*/g, '');
  return result;
}

/**
 * Parse property declarations from object literal
 * Handles: propertyName: type; and propertyName?: type;
 */
function parseProperties(body) {
  const properties = {};

  // Strip comments before parsing to avoid matching text inside comments
  const cleanBody = stripComments(body);

  // Match: propertyName?: type;
  const propRegex = /(\w+)\s*\??\s*:\s*([^;]+);/g;
  let match;

  while ((match = propRegex.exec(cleanBody)) !== null) {
    const [, propName, typeStr] = match;
    const type = typeStr.trim();
    if (propName && type) {
      properties[propName] = type;
    }
  }

  return properties;
}

// ============================================================================
// API Function Parsing
// ============================================================================

/**
 * Extract API functions from services/api .ts files
 */
function parseApiFunctions(typesMap) {
  const apiFunctions = {};

  // Skip types.ts, client.ts, validators.ts, index.ts
  const skipFiles = new Set(['types.ts', 'client.ts', 'validators.ts', 'index.ts']);

  if (!fs.existsSync(SERVICES_API_DIR)) {
    console.log(`Warning: ${SERVICES_API_DIR} not found`);
    return apiFunctions;
  }

  const files = fs.readdirSync(SERVICES_API_DIR)
    .filter(f => f.endsWith('.ts') && !skipFiles.has(f));

  for (const file of files) {
    const filePath = path.join(SERVICES_API_DIR, file);
    const content = fs.readFileSync(filePath, 'utf-8');

    // Extract exports: export async function name(...): Promise<Type>
    const funcRegex = /export\s+async\s+function\s+(\w+)\s*\([^)]*\)\s*:\s*Promise<([^>]+)>\s*\{/g;
    let match;

    while ((match = funcRegex.exec(content)) !== null) {
      const [, funcName, returnType] = match;
      const functionContent = extractFunctionBody(content, match.index);
      const { method, endpoint } = extractHttpDetails(functionContent);

      apiFunctions[funcName] = {
        name: funcName,
        path: `services/api/${file}`,
        endpoint: endpoint || '/unknown',
        method: method || 'GET',
        responseType: returnType.trim(),
      };
    }
  }

  return apiFunctions;
}

/**
 * Extract HTTP method and endpoint from function body
 */
function extractHttpDetails(funcBody) {
  const methodMap = {
    get: 'GET',
    post: 'POST',
    put: 'PUT',
    patch: 'PATCH',
    del: 'DELETE',
  };

  // Pattern 1: Direct call with string literal: get<T>('/endpoint')
  const directCallRegex = /\b(get|post|put|patch|del)\s*<[^>]*>\s*\(\s*['"`]([^'">`]+)/;
  const directMatch = funcBody.match(directCallRegex);

  if (directMatch) {
    const [, methodName, endpoint] = directMatch;
    return {
      method: methodMap[methodName] || 'GET',
      endpoint: endpoint || null,
    };
  }

  // Pattern 2: Call with template literal: get<T>(`/endpoint/${id}`)
  const templateCallRegex = /\b(get|post|put|patch|del)\s*<[^>]*>\s*\(\s*`([^`]+)`/;
  const templateMatch = funcBody.match(templateCallRegex);

  if (templateMatch) {
    const [, methodName, endpoint] = templateMatch;
    return {
      method: methodMap[methodName] || 'GET',
      endpoint: endpoint || null,
    };
  }

  // Pattern 3: URL variable with template literal: const url = `/endpoint...`; get<T>(url)
  const urlVarRegex = /const\s+url\s*=\s*`([^`]+)`/;
  const urlVarMatch = funcBody.match(urlVarRegex);
  const methodCallRegex = /\b(get|post|put|patch|del)\s*<[^>]*>\s*\(\s*url/;
  const methodCallMatch = funcBody.match(methodCallRegex);

  if (urlVarMatch && methodCallMatch) {
    // Extract base path from template, removing query string parts
    let endpoint = urlVarMatch[1];
    // Remove ${...} interpolations that are query params
    endpoint = endpoint.replace(/\$\{[^}]*queryString[^}]*\}/, '');
    endpoint = endpoint.replace(/\?.*$/, ''); // Remove anything after ?
    return {
      method: methodMap[methodCallMatch[1]] || 'GET',
      endpoint: endpoint || null,
    };
  }

  // Pattern 4: Just find the HTTP method being called
  const anyMethodRegex = /\b(get|post|put|patch|del)\s*<[^>]*>\s*\(/;
  const anyMethodMatch = funcBody.match(anyMethodRegex);

  if (anyMethodMatch) {
    // Try to find any URL-like string in the function
    const urlPatternRegex = /['"`](\/?[\w-]+(?:\/[\w-${}\[\]]+)*)/;
    const urlPatternMatch = funcBody.match(urlPatternRegex);

    return {
      method: methodMap[anyMethodMatch[1]] || 'GET',
      endpoint: urlPatternMatch ? urlPatternMatch[1] : null,
    };
  }

  return { method: null, endpoint: null };
}

/**
 * Extract function body from source, limited to reasonable size
 */
function extractFunctionBody(source, startIndex) {
  const remaining = source.substring(startIndex);
  const braceMatch = remaining.match(/\{/);
  if (!braceMatch) return remaining;

  let braceCount = 1;
  let pos = braceMatch.index + 1;
  const maxLength = Math.min(500, remaining.length); // Limit to 500 chars

  while (pos < maxLength && braceCount > 0) {
    if (remaining[pos] === '{') braceCount++;
    else if (remaining[pos] === '}') braceCount--;
    pos++;
  }

  return remaining.substring(0, pos);
}

// ============================================================================
// Hook Parsing
// ============================================================================

/**
 * Extract hooks from hooks .ts files
 */
function parseHooks(apiFunctions) {
  const hooks = {};

  if (!fs.existsSync(HOOKS_DIR)) {
    console.log(`Warning: ${HOOKS_DIR} not found`);
    return hooks;
  }

  const files = fs.readdirSync(HOOKS_DIR)
    .filter(f => f.startsWith('use') && f.endsWith('.ts'));

  for (const file of files) {
    const filePath = path.join(HOOKS_DIR, file);
    const content = fs.readFileSync(filePath, 'utf-8');

    // Extract: export function useHookName(...)
    const hookRegex = /export\s+function\s+(use\w+)\s*\([^)]*\)\s*\{/g;
    let match;

    while ((match = hookRegex.exec(content)) !== null) {
      const [, hookName] = match;
      const hookContent = extractFunctionBody(content, match.index);
      const { queryKey, apiFunctionName, usesQuery, usesMutation } = extractHookDetails(hookContent);
      const dependencies = extractDependencies(hookContent, Object.keys(apiFunctions));

      hooks[hookName] = {
        name: hookName,
        path: `hooks/${file}`,
        apiBinding: apiFunctionName || '',
        queryKey: queryKey || [hookName.replace('use', '').toLowerCase()],
        usesQuery,
        usesMutation,
        dependencies,
      };
    }
  }

  return hooks;
}

/**
 * Extract hook details (useQuery vs useMutation, query key, API binding)
 */
function extractHookDetails(hookBody) {
  const usesQuery = /useQuery\s*\(/.test(hookBody);
  const usesMutation = /useMutation\s*\(/.test(hookBody);

  // Extract query key: queryKey: [...]
  const queryKeyRegex = /queryKey\s*:\s*\[\s*(['"`])([\w-]+)\1/;
  const queryKeyMatch = hookBody.match(queryKeyRegex);
  const queryKey = queryKeyMatch ? [queryKeyMatch[2]] : null;

  // Extract API function: .queryFn: () => functionName( or mutationFn: (...) => functionName(
  const apiFuncRegex = /(?:queryFn|mutationFn)\s*:\s*(?:\(\)\s*=>\s*)?(\w+)\s*\(/;
  const apiFuncMatch = hookBody.match(apiFuncRegex);
  const apiFunctionName = apiFuncMatch ? apiFuncMatch[1] : null;

  return { queryKey, apiFunctionName, usesQuery, usesMutation };
}

/**
 * Extract dependencies (other functions called)
 */
function extractDependencies(content, apiFunctionNames) {
  const dependencies = new Set();

  for (const funcName of apiFunctionNames) {
    const regex = new RegExp(`\\b${funcName}\\s*\\(`);
    if (regex.test(content)) {
      dependencies.add(funcName);
    }
  }

  return Array.from(dependencies);
}

// ============================================================================
// Component Parsing (Optional MVP)
// ============================================================================

/**
 * Extract components from components directory (optional)
 */
function parseComponents(hooks) {
  const components = {};

  if (!fs.existsSync(COMPONENTS_DIR)) {
    return components; // Skip if no components directory
  }

  // For MVP, just collect component file names without deep analysis
  const componentFiles = findComponentFiles(COMPONENTS_DIR);

  for (const filePath of componentFiles) {
    try {
      const content = fs.readFileSync(filePath, 'utf-8');
      const componentName = extractComponentName(filePath, content);

      if (componentName) {
        const relPath = path.relative(WEB_SRC_DIR, filePath).replace(/\\/g, '/');
        const hooksUsed = extractHooksUsedInComponent(content, Object.keys(hooks));
        const apiCallsDirectly = /\bfetch\s*\(|apiClient\s*\(|\bget\s*</.test(content);

        components[componentName] = {
          name: componentName,
          path: relPath,
          hooksUsed,
          apiCallsDirectly,
        };
      }
    } catch (err) {
      // Skip files that can't be read
    }
  }

  return components;
}

/**
 * Recursively find all .tsx files in directory
 */
function findComponentFiles(dir) {
  const files = [];

  try {
    const entries = fs.readdirSync(dir, { withFileTypes: true });
    for (const entry of entries) {
      const fullPath = path.join(dir, entry.name);
      if (entry.isDirectory()) {
        files.push(...findComponentFiles(fullPath));
      } else if (entry.isFile() && entry.name.endsWith('.tsx')) {
        files.push(fullPath);
      }
    }
  } catch (err) {
    // Skip directories that can't be read
  }

  return files;
}

/**
 * Extract component name from file path or export statement
 */
function extractComponentName(filePath, content) {
  // Try to extract from export default function Name() or export const Name =
  const nameRegex = /export\s+(?:default\s+)?(?:function|const)\s+(\w+)/;
  const match = content.match(nameRegex);

  if (match) {
    return match[1];
  }

  // Fall back to file name
  const fileName = path.basename(filePath, '.tsx');
  if (fileName.match(/^[A-Z]/)) {
    return fileName;
  }

  return null;
}

/**
 * Extract hooks used in component
 */
function extractHooksUsedInComponent(content, hookNames) {
  const used = new Set();

  for (const hookName of hookNames) {
    const regex = new RegExp(`\\b${hookName}\\s*\\(`);
    if (regex.test(content)) {
      used.add(hookName);
    }
  }

  return Array.from(used);
}

// ============================================================================
// Edge Building
// ============================================================================

/**
 * Build edges connecting nodes in the graph
 */
function buildEdges(apiFunctions, hooks, components) {
  const edges = [];

  // Hook -> API Function (apiBinding)
  for (const [hookName, hook] of Object.entries(hooks)) {
    if (hook.apiBinding && apiFunctions[hook.apiBinding]) {
      edges.push({
        from: hookName,
        to: hook.apiBinding,
        type: 'api_binding',
      });
    }

    // Hook -> Hook (dependencies)
    for (const dep of hook.dependencies) {
      if (hooks[dep]) {
        edges.push({
          from: hookName,
          to: dep,
          type: 'depends_on',
        });
      }
    }
  }

  // Component -> Hook (hooksUsed)
  for (const [compName, comp] of Object.entries(components)) {
    for (const hookName of comp.hooksUsed) {
      if (hooks[hookName]) {
        edges.push({
          from: compName,
          to: hookName,
          type: 'uses_hook',
        });
      }
    }
  }

  return edges;
}

// ============================================================================
// Main
// ============================================================================

async function main() {
  try {
    console.log('Frontend Graph Generator');
    console.log('========================\n');
    console.log(`Source directory: ${WEB_SRC_DIR}\n`);

    // 1. Parse types from services/api/types.ts
    console.log('Reading services/api/types.ts...');
    let types = {};
    if (fs.existsSync(TYPES_FILE)) {
      const typesContent = fs.readFileSync(TYPES_FILE, 'utf-8');
      types = parseTypes(typesContent, 'services/api/types.ts');
      console.log(`  Found ${Object.keys(types).length} types/interfaces/enums\n`);
    } else {
      console.log('  Warning: services/api/types.ts not found\n');
    }

    // 2. Parse types from types/ directory
    console.log('Reading types/ directory...');
    const typesFromDir = parseTypesFromDirectory(TYPES_DIR);
    const typesFromDirCount = Object.keys(typesFromDir).length;
    console.log(`  Found ${typesFromDirCount} types/interfaces/enums\n`);

    // 3. Merge types (types.ts takes precedence for duplicates)
    const mergedTypes = { ...typesFromDir, ...types };
    console.log(`  Total merged: ${Object.keys(mergedTypes).length} types\n`);
    types = mergedTypes;

    // 4. Parse API functions
    console.log('Reading API service files...');
    const apiFunctions = parseApiFunctions(types);
    console.log(`  Found ${Object.keys(apiFunctions).length} API functions\n`);

    // 5. Parse hooks
    console.log('Reading hook files...');
    const hooks = parseHooks(apiFunctions);
    console.log(`  Found ${Object.keys(hooks).length} hooks\n`);

    // 6. Parse components (optional)
    console.log('Reading component files...');
    const components = parseComponents(hooks);
    console.log(`  Found ${Object.keys(components).length} components\n`);

    // 7. Build edges
    console.log('Building relationship graph...');
    const edges = buildEdges(apiFunctions, hooks, components);
    console.log(`  Found ${edges.length} edges\n`);

    // 8. Create graph
    const graph = {
      version: '1.0.0',
      generated_at: new Date().toISOString(),
      types,
      api_functions: apiFunctions,
      hooks,
      components,
      edges,
    };

    // 9. Validate minimal structure
    if (Object.keys(types).length === 0) {
      throw new Error('No types found - check TYPES_FILE and TYPES_DIR paths');
    }

    // 10. Ensure output directory exists
    if (!fs.existsSync(OUTPUT_DIR)) {
      fs.mkdirSync(OUTPUT_DIR, { recursive: true });
    }

    // 11. Write output
    fs.writeFileSync(OUTPUT_FILE, JSON.stringify(graph, null, 2));
    console.log(`Written to: ${OUTPUT_FILE}`);
    console.log(`Total nodes: ${Object.keys(types).length + Object.keys(apiFunctions).length + Object.keys(hooks).length + Object.keys(components).length}`);
    console.log('Done!');

  } catch (error) {
    console.error('Error:', error.message);
    process.exit(1);
  }
}

// Only run main if called directly (not when required for testing)
if (require.main === module) {
  main();
}

// Export functions for testing
module.exports = {
  parseArgs,
  parseTypes,
  parseTypesFromDirectory,
  parseProperties,
  parseApiFunctions,
  extractHttpDetails,
  extractFunctionBody,
  parseHooks,
  extractHookDetails,
  extractDependencies,
  parseComponents,
  findComponentFiles,
  extractComponentName,
  extractHooksUsedInComponent,
  buildEdges,
};
