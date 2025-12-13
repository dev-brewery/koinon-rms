# Ollama MCP Server Implementation Plan
## GPU-Accelerated Local LLM for Claude Code

**Project:** Windows-hosted Ollama MCP Server with NVIDIA GPU acceleration
**Location:** `G:\repos\wsl-mcp-ollama`
**Model:** QWEN2.5-coder:7b (code-specialized, 32K context)
**Primary Use Case:** Token-rich tasks requiring fast, accurate responses without advanced reasoning

---

## Executive Summary

This plan details the implementation of an MCP server that exposes a Windows-hosted Ollama instance to Claude Code running in WSL2. The server will leverage an NVIDIA 1080 Ti GPU for accelerated inference using the QWEN2.5-coder:7b model, optimized for high-volume token processing with minimal latency.

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│ WSL2 Environment                                            │
│                                                             │
│  ┌──────────────┐         ┌────────────────────┐          │
│  │ Claude Code  │ ◄─────► │ Windows MCP Server │          │
│  │   (Client)   │  stdio  │   (Node.js)        │          │
│  └──────────────┘         └─────────┬──────────┘          │
│                                      │                      │
└──────────────────────────────────────┼──────────────────────┘
                                       │ HTTP (localhost:11434)
┌──────────────────────────────────────▼──────────────────────┐
│ Windows Host                                                │
│                                                             │
│  ┌──────────────────────────────────────────────────┐     │
│  │ Ollama Server (localhost:11434)                  │     │
│  │  - Model: qwen2.5-coder:7b                       │     │
│  │  - Token optimization for fast inference         │     │
│  └──────────────────────────────────────────────────┘     │
│                              │                              │
│                              ▼                              │
│                    ┌─────────────────┐                     │
│                    │ NVIDIA 1080 Ti  │                     │
│                    │  11GB VRAM      │                     │
│                    └─────────────────┘                     │
└─────────────────────────────────────────────────────────────┘
```

---

## Confirmed Requirements

✓ **Environment:** WSL2 on Windows with NVIDIA 1080 Ti (11GB VRAM)
✓ **Install Location:** `G:\repos\wsl-mcp-ollama`
✓ **Primary Use Case:** Token-rich tasks with fast turnaround (log parsing, bulk analysis)
✓ **Log Format Support:** Universal design for any log format
✓ **Performance Priority:** Speed and accuracy over advanced reasoning

---

## Phase 1: Windows Environment Setup (15 minutes)

### 1.1 Install Ollama on Windows

```powershell
# Option A: Direct download
# Visit: https://ollama.ai/download/windows

# Option B: Package manager
winget install Ollama.Ollama

# Verify installation
ollama --version

# Start Ollama service (runs in background)
ollama serve
```

**Expected Output:**
```
Ollama server running on http://localhost:11434
```

### 1.2 Pull QWEN2.5-Coder Model

```powershell
# Pull the model (~4GB download)
ollama pull qwen2.5-coder:7b

# Verify model is available
ollama list

# Test model inference
ollama run qwen2.5-coder:7b "Write a Python function to parse CSV logs"
```

**Performance Notes:**
- Initial download: ~4GB (one-time)
- Model load time: 2-5 seconds (first use)
- Cached in VRAM: subsequent calls <100ms

### 1.3 Verify GPU Acceleration

```powershell
# Monitor GPU usage
nvidia-smi

# Run inference and watch VRAM usage
ollama run qwen2.5-coder:7b "Test prompt"
# In another terminal: nvidia-smi -l 1  # Update every 1 second
```

**Expected GPU Usage:**
- Idle: ~1-2GB VRAM (model cached)
- Inference: 80-100% GPU utilization
- VRAM: 6-8GB total for qwen2.5-coder:7b

---

## Phase 2: Token Threshold Research & Optimization

### 2.1 QWEN2.5-Coder Model Specifications

**Model Details:**
- **Parameters:** 7 billion
- **Context Window:** 32,768 tokens (~130KB text)
- **Architecture:** Transformer-based decoder
- **Specialization:** Code analysis, generation, debugging

### 2.2 Token Performance Characteristics

Based on QWEN2.5-coder documentation and 1080 Ti benchmarks:

| Input Tokens | Expected Latency | Throughput | Recommended Use Case |
|--------------|------------------|------------|----------------------|
| 1-512 | 50-100ms | 50 tok/s | Single function analysis, short logs |
| 513-2048 | 100-500ms | 40-50 tok/s | File-level code review, medium logs |
| 2049-8192 | 500ms-2s | 30-40 tok/s | Multi-file analysis, large log files |
| 8193-16384 | 2-5s | 20-30 tok/s | Repository analysis, aggregated logs |
| 16385-32768 | 5-10s | 15-25 tok/s | Repository analysis, massive logs |

**Optimal Token Range for Fast Responses:** 512-4096 tokens

### 2.3 Token Budget Strategy

**Tool Configuration:**

1. **Fast Operations (50-100ms):**
   - Max input: 512 tokens
   - Max output: 256 tokens
   - Use case: Error extraction, pattern matching

2. **Standard Operations (100-500ms):**
   - Max input: 2048 tokens
   - Max output: 512 tokens
   - Use case: Code review, log summarization

3. **Heavy Operations (500ms-2s):**
   - Max input: 8192 tokens
   - Max output: 1024 tokens
   - Use case: Deep analysis, comprehensive reviews

**Automatic Chunking:**
- Input >4096 tokens: Auto-split into chunks
- Process chunks in parallel (GPU batch processing)
- Merge results with context preservation

### 2.4 Token Estimation Utilities

```typescript
// Token estimation (rough approximation)
function estimateTokens(text: string): number {
  // QWEN uses ~4 chars per token on average for code
  return Math.ceil(text.length / 4);
}

// Smart chunking for large inputs
function chunkText(text: string, maxTokens: number = 4096): string[] {
  const estimatedTokens = estimateTokens(text);

  if (estimatedTokens <= maxTokens) {
    return [text];
  }

  // Split by logical boundaries (newlines, then sentences)
  const lines = text.split('\n');
  const chunks: string[] = [];
  let currentChunk = '';
  let currentTokens = 0;

  for (const line of lines) {
    const lineTokens = estimateTokens(line);

    if (currentTokens + lineTokens > maxTokens) {
      if (currentChunk) chunks.push(currentChunk);
      currentChunk = line;
      currentTokens = lineTokens;
    } else {
      currentChunk += (currentChunk ? '\n' : '') + line;
      currentTokens += lineTokens;
    }
  }

  if (currentChunk) chunks.push(currentChunk);
  return chunks;
}
```

---

## Phase 3: MCP Server Implementation (2-3 hours)

### 3.1 Project Structure

```
G:\repos\wsl-mcp-ollama\
├── package.json           # Dependencies and scripts
├── tsconfig.json          # TypeScript configuration
├── README.md              # Usage documentation
├── .env.example           # Environment template
├── src\
│   ├── index.ts           # Main MCP server (~500 lines)
│   ├── ollama-client.ts   # Ollama API wrapper (~150 lines)
│   ├── token-utils.ts     # Token estimation/chunking (~100 lines)
│   ├── log-parsers.ts     # Universal log format detection (~200 lines)
│   └── schemas.ts         # Zod validation schemas (~100 lines)
├── dist\                  # Compiled JavaScript
└── tests\                 # Unit tests (optional)
```

### 3.2 Dependencies

**`package.json`:**
```json
{
  "name": "@wsl-mcp/ollama-server",
  "version": "1.0.0",
  "type": "module",
  "main": "dist/index.js",
  "bin": {
    "wsl-mcp-ollama": "dist/index.js"
  },
  "scripts": {
    "build": "tsc",
    "watch": "tsc --watch",
    "start": "node dist/index.js",
    "test": "node --test"
  },
  "dependencies": {
    "@modelcontextprotocol/sdk": "^1.24.3",
    "zod": "^3.23.8",
    "node-fetch": "^3.3.2"
  },
  "devDependencies": {
    "@types/node": "^22.0.0",
    "typescript": "^5.6.0"
  }
}
```

**`tsconfig.json`:**
```json
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "Node16",
    "moduleResolution": "Node16",
    "lib": ["ES2022"],
    "outDir": "./dist",
    "rootDir": "./src",
    "strict": true,
    "esModuleInterop": true,
    "skipLibCheck": true,
    "forceConsistentCasingInFileNames": true,
    "declaration": true,
    "declarationMap": true,
    "sourceMap": true
  },
  "include": ["src/**/*"],
  "exclude": ["node_modules", "dist"]
}
```

### 3.3 Tool Implementations

#### Tool 1: `ollama_generate` - General-Purpose Generation

**Purpose:** Flexible LLM queries with token-aware optimization

**Input Schema:**
```typescript
const GenerateSchema = z.object({
  prompt: z.string(),
  system: z.string().optional(),
  temperature: z.number().min(0).max(2).default(0.7),
  max_tokens: z.number().min(1).max(4096).default(1024),
  auto_chunk: z.boolean().default(true)
});
```

**Behavior:**
- Estimates input tokens
- Auto-chunks if >4096 tokens (when `auto_chunk: true`)
- Returns aggregated results
- Logs token usage for optimization

#### Tool 2: `ollama_analyze_logs` - Universal Log Analysis

**Purpose:** Fast, accurate log parsing for any format

**Input Schema:**
```typescript
const AnalyzeLogsSchema = z.object({
  logs: z.string(),
  task: z.enum([
    'extract_errors',      // Find errors, exceptions, failures
    'summarize',           // High-level summary
    'find_patterns',       // Recurring patterns, anomalies
    'detect_format',       // Identify log format
    'extract_metrics',     // Performance metrics, timings
    'trace_requests'       // Request/response correlation
  ]).default('extract_errors'),
  format_hint: z.enum([
    'auto',              // Auto-detect
    'json',              // JSON logs
    'apache',            // Apache access/error logs
    'nginx',             // Nginx logs
    'syslog',            // Syslog format
    'application',       // Generic app logs
    'stacktrace'         // Stack traces
  ]).optional(),
  chunk_size: z.number().min(512).max(8192).default(4096)
});
```

**Log Format Detection Strategy:**

```typescript
// src/log-parsers.ts
export function detectLogFormat(logs: string): string {
  // JSON logs
  if (logs.trim().startsWith('{') || logs.includes('"timestamp":')) {
    return 'json';
  }

  // Apache/Nginx access logs
  if (/\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}.*\[.*\]Ꭵ.*"(GET|POST|PUT|DELETE)/.test(logs)) {
    return logs.includes('nginx') ? 'nginx' : 'apache';
  }

  // Syslog
  if (/^[A-Z][a-z]{2}\s+\d{1,2}\s+\d{2}:\d{2}:\d{2}/.test(logs)) {
    return 'syslog';
  }

  // Stack traces
  if (logs.includes('Traceback') || (logs.includes('at ') && logs.includes('.cs:line'))) {
    return 'stacktrace';
  }

  // Default to generic application logs
  return 'application';
}

export function buildLogAnalysisPrompt(
  logs: string,
  task: string,
  format: string
): { prompt: string; system: string } {
  const formatContext = {
    json: 'JSON-structured logs with timestamp, level, and message fields',
    apache: 'Apache web server access/error logs with IP, timestamp, request, status',
    nginx: 'Nginx web server logs with similar structure to Apache',
    syslog: 'System logs with facility, severity, hostname, and message',
    application: 'Application logs with timestamps and free-form messages',
    stacktrace: 'Exception stack traces with file names, line numbers, and call stacks'
  };

  const taskPrompts = {
    extract_errors: `Extract ALL errors, exceptions, and failures from these ${format} logs. Return a structured list with:
- Timestamp (if available)
- Error type/severity
- Error message
- Context (file, line, method if available)
- Count (if error repeats)`,

    summarize: `Provide a concise summary of these ${format} logs covering:
- Overall status (healthy/degraded/failing)
- Key events and milestones
- Error count and types
- Performance indicators
- Notable patterns`,

    find_patterns: `Analyze these ${format} logs for recurring patterns:
- Repeating errors or warnings
- Request/response patterns
- Performance bottlenecks
- Anomalies or outliers
- Temporal patterns (time-based)`,

    detect_format: `Identify the exact log format and structure. Provide:
- Format type (JSON, Apache, syslog, etc.)
- Field structure
- Timestamp format
- Special patterns or conventions used`,

    extract_metrics: `Extract performance metrics from these ${format} logs:
- Response times / latency
- Throughput (requests per second)
- Error rates
- Resource usage (if logged)
- 95th/99th percentile times`,

    trace_requests: `Trace request flows through these ${format} logs:
- Request IDs or correlation IDs
- Full request lifecycle (start → processing → completion)
- Cross-service calls
- Failed requests and their causes`
  };

  return {
    prompt: logs,
    system: `You are a log analysis expert specialized in ${format} format logs.

${formatContext[format] || formatContext.application}

Task: ${taskPrompts[task] || taskPrompts.extract_errors}

Be precise, factual, and structured in your response. Extract data, don't speculate.`
  };
}
```

**Optimization:**
- Temperature: 0.3 (factual extraction)
- Max output: 2048 tokens (detailed findings)
- Auto-chunking for logs >4096 tokens
- Parallel processing of chunks

#### Tool 3: `ollama_review_code` - Code Review

**Purpose:** Fast code analysis with focus areas

**Input Schema:**
```typescript
const ReviewCodeSchema = z.object({
  code: z.string(),
  language: z.string(),
  focus: z.enum([
    'bugs',           // Logic errors, null refs, edge cases
    'style',          // Code style, naming, formatting
    'performance',    // Performance issues, N+1, blocking calls
    'security',       // Security vulnerabilities
    'all'             // Comprehensive review
  ]).default('all')
});
```

**Temperature by Focus:**
```typescript
const focusTemperature = {
  bugs: 0.4,          // Precise bug detection
  style: 0.6,         // Balanced style suggestions
  performance: 0.5,   // Factual performance analysis
  security: 0.3,      // Critical security checks
  all: 0.5            // Balanced comprehensive review
};
```

#### Tool 4: `ollama_chat` - Multi-Turn Conversation

**Purpose:** Interactive problem-solving with context

**Input Schema:**
```typescript
const ChatSchema = z.object({
  messages: z.array(z.object({
    role: z.enum(['user', 'assistant']),
    content: z.string()
  })),
  temperature: z.number().min(0).max(2).default(0.7),
  max_tokens: z.number().min(128).max(2048).default(512)
});
```

**Context Management:**
- Maintains conversation history
- Auto-prunes old messages if total tokens >8192
- Preserves last 3 exchanges minimum

### 3.4 Ollama API Client

**`src/ollama-client.ts`:**
```typescript
import fetch from 'node-fetch';

export interface OllamaOptions {
  temperature?: number;
  num_predict?: number;  // Max output tokens
  top_p?: number;
  top_k?: number;
  repeat_penalty?: number;
}

export interface OllamaRequest {
  model: string;
  prompt: string;
  system?: string;
  stream?: boolean;
  options?: OllamaOptions;
}

export interface OllamaResponse {
  model: string;
  response: string;
  created_at: string;
  done: boolean;
  total_duration: number;
  load_duration: number;
  prompt_eval_count: number;    // Input tokens
  eval_count: number;            // Output tokens
  eval_duration: number;
}

export class OllamaClient {
  private baseUrl: string;
  private model: string;

  constructor(baseUrl: string = 'http://localhost:11434', model: string = 'qwen2.5-coder:7b') {
    this.baseUrl = baseUrl;
    this.model = model;
  }

  async generate(
    prompt: string,
    system?: string,
    options?: OllamaOptions
  ): Promise<OllamaResponse> {
    const response = await fetch(`${this.baseUrl}/api/generate`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        model: this.model,
        prompt,
        system,
        stream: false,
        options: {
          temperature: 0.7,
          num_predict: 1024,
          ...options
        }
      } as OllamaRequest)
    });

    if (!response.ok) {
      throw new Error(`Ollama API error: ${response.statusText}`);
    }

    return await response.json() as OllamaResponse;
  }

  async chat(
    messages: Array<{ role: string; content: string }>,
    options?: OllamaOptions
  ): Promise<OllamaResponse> {
    // Convert chat messages to single prompt
    const prompt = messages
      .map(m => `${m.role === 'user' ? 'User' : 'Assistant'}: ${m.content}`)
      .join('\n\n');

    return this.generate(prompt, undefined, options);
  }

  async healthCheck(): Promise<boolean> {
    try {
      const response = await fetch(`${this.baseUrl}/api/tags`);
      return response.ok;
    } catch {
      return false;
    }
  }
}
```

### 3.5 Main Server Implementation

**`src/index.ts`** (simplified structure):
```typescript
import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import {
  CallToolRequestSchema,
  ListToolsRequestSchema
} from '@modelcontextprotocol/sdk/types.js';
import { OllamaClient } from './ollama-client.js';
import {
  GenerateSchema,
  AnalyzeLogsSchema,
  ReviewCodeSchema,
  ChatSchema
} from './schemas.js';
import {
  estimateTokens,
  chunkText
} from './token-utils.js';
import {
  detectLogFormat,
  buildLogAnalysisPrompt
} from './log-parsers.js';

const server = new Server(
  {
    name: 'wsl-mcp-ollama-server',
    version: '1.0.0',
  },
  {
    capabilities: {
      tools: {}
    },
  }
);

const ollama = new OllamaClient(
  process.env.OLLAMA_HOST || 'http://localhost:11434',
  'qwen2.5-coder:7b'
);

// Tool registration
server.setRequestHandler(ListToolsRequestSchema, async () => {
  return {
    tools: [
      {
        name: 'ollama_generate',
        description: 'General-purpose text generation with QWEN2.5-coder. Auto-chunks large inputs.',
        inputSchema: { /* GenerateSchema as JSON Schema */ }
      },
      {
        name: 'ollama_analyze_logs',
        description: 'Universal log analysis: extract errors, summarize, find patterns. Supports any log format.',
        inputSchema: { /* AnalyzeLogsSchema as JSON Schema */ }
      },
      {
        name: 'ollama_review_code',
        description: 'Fast code review focusing on bugs, style, performance, or security.',
        inputSchema: { /* ReviewCodeSchema as JSON Schema */ }
      },
      {
        name: 'ollama_chat',
        description: 'Multi-turn conversation with context management.',
        inputSchema: { /* ChatSchema as JSON Schema */ }
      }
    ]
  };
});

// Tool call handler
server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;

  try {
    switch (name) {
      case 'ollama_generate': {
        const { prompt, system, temperature, max_tokens, auto_chunk } =
          GenerateSchema.parse(args);

        const estimatedTokens = estimateTokens(prompt);
        console.error(`[ollama_generate] Input: ${estimatedTokens} tokens (estimated)`);

        if (auto_chunk && estimatedTokens > 4096) {
          const chunks = chunkText(prompt, 4096);
          console.error(`[ollama_generate] Chunked into ${chunks.length} parts`);

          const results = await Promise.all(
            chunks.map(chunk =>
              ollama.generate(chunk, system, {
                temperature,
                num_predict: max_tokens
              })
            )
          );

          const aggregated = results.map(r => r.response).join('\n\n');
          return {
            content: [{
              type: 'text',
              text: aggregated
            }]
          };
        }

        const result = await ollama.generate(prompt, system, {
          temperature,
          num_predict: max_tokens
        });

        console.error(`[ollama_generate] Output: ${result.eval_count} tokens, ${result.total_duration / 1e6}ms`);

        return {
          content: [{
            type: 'text',
            text: result.response
          }]
        };
      }

      case 'ollama_analyze_logs': {
        const { logs, task, format_hint, chunk_size } =
          AnalyzeLogsSchema.parse(args);

        const detectedFormat = format_hint === 'auto' ? detectLogFormat(logs) : format_hint;
        const { prompt, system } = buildLogAnalysisPrompt(logs, task, detectedFormat);

        console.error(`[ollama_analyze_logs] Format: ${detectedFormat}, Task: ${task}`);

        const estimatedTokens = estimateTokens(logs);
        if (estimatedTokens > chunk_size) {
          const chunks = chunkText(logs, chunk_size);
          console.error(`[ollama_analyze_logs] Chunked into ${chunks.length} parts`);

          const results = await Promise.all(
            chunks.map(chunk =>
              ollama.generate(chunk, system, {
                temperature: 0.3,
                num_predict: 2048
              })
            )
          );

          const aggregated = results.map((r, i) =>
            `## Chunk ${i + 1}/${chunks.length}\n${r.response}`
          ).join('\n\n');

          return {
            content: [{
              type: 'text',
              text: aggregated
            }]
          };
        }

        const result = await ollama.generate(prompt, system, {
          temperature: 0.3,
          num_predict: 2048
        });

        return {
          content: [{
            type: 'text',
            text: result.response
          }]
        };
      }

      case 'ollama_review_code': {
        const { code, language, focus, max_tokens } =
          ReviewCodeSchema.parse(args);

        const temperatures = {
          bugs: 0.4,
          style: 0.6,
          performance: 0.5,
          security: 0.3,
          all: 0.5
        };

        const systemMessage = `You are a senior ${language} developer. Review this code for ${focus === 'all' ? 'bugs, style, performance, and security' : focus}. Be specific and actionable.`;

        const result = await ollama.generate(code, systemMessage, {
          temperature: temperatures[focus],
          num_predict: max_tokens
        });

        return {
          content: [{
            type: 'text',
            text: result.response
          }]
        };
      }

      case 'ollama_chat': {
        const { messages, temperature, max_tokens } =
          ChatSchema.parse(args);

        const result = await ollama.chat(messages, {
          temperature,
          num_predict: max_tokens
        });

        return {
          content: [{
            type: 'text',
            text: result.response
          }]
        };
      }

      default:
        throw new Error(`Unknown tool: ${name}`);
    }
  } catch (error) {
    const errorMessage = error instanceof Error ? error.message : String(error);
    console.error(`[${name}] Error:`, errorMessage);

    return {
      content: [{
        type: 'text',
        text: `Error: ${errorMessage}`
      }],
      isError: true
    };
  }
});

async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);

  // Health check
  const healthy = await ollama.healthCheck();
  if (!healthy) {
    console.error('WARNING: Ollama server not responding at', process.env.OLLAMA_HOST || 'http://localhost:11434');
  }

  console.error('WSL MCP Ollama Server running on stdio');
  console.error('Model: qwen2.5-coder:7b');
  console.error('Optimized for: Fast token processing (512-4096 token range)');
}

main().catch((error) => {
  console.error('Server error:', error);
  process.exit(1);
});
```

---

## Phase 4: WSL2 Integration (30 minutes)

### 4.1 Create Wrapper Script

**File:** `/home/mbrewer/.claude/scripts/ollama-mcp.sh`

```bash
#!/bin/bash
# Wrapper to invoke Windows Node.js MCP server from WSL2

# Path to Windows Node.js
NODE_EXE="/mnt/g/Program Files/nodejs/node.exe"

# Path to compiled MCP server
MCP_SERVER="/mnt/g/repos/wsl-mcp-ollama/dist/index.js"

# Ollama API endpoint (localhost accessible from both WSL2 and Windows)
export OLLAMA_HOST="http://localhost:11434"

# Execute server
exec "$NODE_EXE" "$MCP_SERVER"
```

**Make executable:**
```bash
chmod +x /home/mbrewer/.claude/scripts/ollama-mcp.sh
```

### 4.2 Update MCP Configuration

**File:** `/home/mbrewer/projects/koinon-rms/.claude/.mcp.json`

Add the ollama server entry:

```json
{
  "mcpServers": {
    "postgres": { /* existing */ },
    "memory": { /* existing */ },
    "github": { /* existing */ },
    "filesystem": { /* existing */ },
    "koinon-dev": { /* existing */ },

    "ollama": {
      "command": "/home/mbrewer/.claude/scripts/ollama-mcp.sh",
      "args": [],
      "env": {
        "OLLAMA_HOST": "http://localhost:11434"
      }
    }
  }
}
```

### 4.3 Path Translation Notes

**Windows → WSL2:**
- `G:\repos\wsl-mcp-ollama` → `/mnt/g/repos/wsl-mcp-ollama`
- `C:\Program Files\nodejs\node.exe` → `/mnt/c/Program Files/nodejs/node.exe`

**Localhost Networking:**
- `localhost:11434` accessible from both WSL2 and Windows
- No special port forwarding required

---

## Phase 5: Testing & Validation (30-60 minutes)

### 5.1 Ollama Health Check

```powershell
# Check if Ollama is running
curl http://localhost:11434/api/tags

# Should return list of models including qwen2.5-coder:7b
```

### 5.2 Manual API Test

```powershell
# Test generation
curl http://localhost:11434/api/generate -d '{
  "model": "qwen2.5-coder:7b",
  "prompt": "Write a Python function to parse Apache logs",
  "stream": false
}' | ConvertFrom-Json | Select-Object -ExpandProperty response
```

### 5.3 MCP Server Startup Test

```powershell
# From Windows PowerShell in G:\repos\wsl-mcp-ollama
npm install
npm run build

# Test server startup
node dist/index.js
# Expected output:
# WSL MCP Ollama Server running on stdio
# Model: qwen2.5-coder:7b
# Optimized for: Fast token processing (512-4096 token range)

# Press Ctrl+C to exit
```

### 5.4 WSL2 Integration Test

```bash
# From WSL2
/home/mbrewer/.claude/scripts/ollama-mcp.sh
# Should start server with same output as above

# Test from Claude Code:
# After restarting Claude Code, check available tools
# Should see: mcp__ollama__generate, mcp__ollama__analyze_logs, etc.
```

### 5.5 Token Performance Benchmarking

Create test script: `G:\repos\wsl-mcp-ollama\benchmark.js`

```javascript
import { OllamaClient } from './dist/ollama-client.js';

const client = new OllamaClient();

const testCases = [
  { name: 'Small (128 tokens)', size: 512 },
  { name: 'Medium (512 tokens)', size: 2048 },
  { name: 'Large (2048 tokens)', size: 8192 },
  { name: 'XLarge (8192 tokens)', size: 32768 }
];

for (const test of testCases) {
  const prompt = 'x'.repeat(test.size);  // Dummy text
  const start = Date.now();

  const result = await client.generate(prompt, 'Summarize this text briefly');

  const duration = Date.now() - start;
  const tokensPerSecond = (result.eval_count / (duration / 1000)).toFixed(2);

  console.log(`${test.name}: ${duration}ms, ${result.eval_count} tokens, ${tokensPerSecond} tok/s`);
}
```

**Expected Results:**
- Small: 50-100ms, 40-50 tok/s
- Medium: 200-500ms, 35-45 tok/s
- Large: 1-3s, 25-35 tok/s
- XLarge: 5-10s, 15-25 tok/s

### 5.6 Log Format Detection Test

Create test file: `test-logs.txt`

```
# Apache access log
192.168.1.1 - - [09/Dec/2024:18:30:15 +0000] "GET /api/users HTTP/1.1" 200 1234

# JSON log
{"timestamp":"2024-12-09T18:30:15Z","level":"ERROR","message":"Connection timeout"}

# Syslog
Dec  9 18:30:15 server app[1234]: Fatal error in module initialization

# Stack trace
Traceback (most recent call last):
  File "app.py", line 42, in main
    process_data(None)
TypeError: 'NoneType' object is not iterable
```

Test from Claude Code:
```typescript
const logs = await readFile('test-logs.txt');
const result = await mcp__ollama__analyze_logs({
  logs,
  task: 'detect_format'
});
// Should identify all 4 formats
```

---

## Performance Optimization Guidelines

### 6.1 Optimal Token Ranges by Use Case

| Use Case | Input Tokens | Output Tokens | Expected Latency | Temperature |
|----------|--------------|---------------|------------------|-------------|
| Error extraction | 512-2048 | 256-512 | 100-300ms | 0.3 |
| Code review (function) | 256-1024 | 512-1024 | 100-500ms | 0.5 |
| Code review (file) | 1024-4096 | 1024-2048 | 500ms-2s | 0.5 |
| Log summarization | 2048-8192 | 256-512 | 500ms-2s | 0.3 |
| Pattern detection | 4096-16384 | 512-1024 | 2-5s | 0.4 |
| Code generation | 128-512 | 256-1024 | 50-300ms | 0.7-0.9 |

### 6.2 GPU Optimization

**Ollama Configuration** (create `~/.ollama/config.json` on Windows):
```json
{
  "num_gpu": 1,
  "gpu_layers": 35,
  "num_thread": 8,
  "num_ctx": 4096,
  "num_batch": 512,
  "low_vram": false
}
```

**Explanation:**
- `num_gpu: 1` - Use single GPU (1080 Ti)
- `gpu_layers: 35` - Offload 35 transformer layers to GPU
- `num_ctx: 4096` - Default context window (sweet spot for speed)
- `num_batch: 512` - Batch size for parallel processing
- `low_vram: false` - Use full VRAM (we have 11GB)

### 6.3 Caching Strategy

Ollama automatically caches:
- Model weights in VRAM (6-8GB persistent)
- Recent prompts/responses (helps with repeated queries)
- Auto-unloads after 5 minutes idle (configurable)

**Keep-Alive Configuration:**
```bash
# Keep model loaded indefinitely
ollama run qwen2.5-coder:7b --keepalive -1
```

---

## Use Case Implementation Examples

### Example 1: Fast CI Log Analysis

**Scenario:** GitHub Actions build fails, parse logs quickly

```typescript
// From Claude Code
const ciLogs = await Bash('gh run view 12345 --log-failed');

const errors = await mcp__ollama__analyze_logs({
  logs: ciLogs,
  task: 'extract_errors',
  format_hint: 'application',
  chunk_size: 4096  // Optimal for speed
});

// Returns in 200-500ms:
// - Compilation errors with line numbers
// - Test failures with stack traces
// - Build step that failed
```

### Example 2: Bulk Code Review (Multiple Files)

**Scenario:** Review 10 files before committing

```typescript
const files = await Glob('src/**/*.ts');
const reviews = await Promise.all(
  files.slice(0, 10).map(async file => {
    const code = await Read(file);
    return await mcp__ollama__review_code({
      code,
      language: 'typescript',
      focus: 'bugs',
      max_tokens: 1024  // Fast review
    });
  })
);

// Parallel execution: ~1-2 seconds total for 10 files
```

### Example 3: Pattern Detection in Large Logs

**Scenario:** Analyze 1MB log file for patterns

```typescript
const logs = await Read('/var/log/app.log');

// Auto-chunks into ~16 parts (4KB each)
const patterns = await mcp__ollama__analyze_logs({
  logs,
  task: 'find_patterns',
  chunk_size: 4096,
  format_hint: 'json'
});

// Returns:
// - "Connection timeout" appears 47 times (timestamps: ...)
// - "Memory warning" spikes at 14:30-15:00
// - Request rate: 100 req/s baseline, 500 req/s during spike
```

### Example 4: Commit Message Generation

**Scenario:** Generate commit message from staged changes

```typescript
const diff = await Bash('git diff --cached');

const message = await mcp__ollama__generate({
  prompt: `Generate a conventional commit message for this diff:\n\n${diff}`,
  system: 'You are a commit message expert. Format: type(scope): description. Focus on WHY, not WHAT.',
  temperature: 0.6,
  max_tokens: 200,
  auto_chunk: false  // Diffs usually <4K tokens
});

// Returns in 50-150ms:
// "fix(api): resolve null reference in user authentication
//
// Adds null check before accessing user.claims to prevent
// NullReferenceException when claims collection is empty."
```

---

## Monitoring & Debugging

### 7.1 Server Logs

All console.error output is visible in Claude Code's MCP panel:

```typescript
console.error('[ollama_generate] Input: 2048 tokens (estimated)');
console.error('[ollama_generate] Output: 512 tokens, 287ms');
```

### 7.2 Ollama Server Logs

**Windows Event Viewer:**
```powershell
Get-EventLog -LogName Application -Source Ollama -Newest 20
```

**Or run Ollama in foreground:**
```powershell
# Stop service
Stop-Service Ollama

# Run manually to see logs
ollama serve
```

### 7.3 GPU Monitoring

**Real-time monitoring:**
```powershell
nvidia-smi -l 1  # Update every 1 second
```

**Watch for:**
- GPU utilization: Should be 80-100% during inference
- VRAM usage: 6-8GB when model loaded
- Temperature: <80°C is normal
- Power draw: 150-250W is typical for 1080 Ti

### 7.4 Performance Metrics

Add to `src/index.ts`:

```typescript
const metrics = {
  total_requests: 0,
  avg_latency: 0,
  avg_tokens_in: 0,
  avg_tokens_out: 0,
  errors: 0
};

// Update in tool handlers
metrics.total_requests++;
metrics.avg_latency = (metrics.avg_latency * (metrics.total_requests - 1) + duration) / metrics.total_requests;

// Export endpoint
server.setRequestHandler('get_metrics', async () => {
  return { content: [{ type: 'text', text: JSON.stringify(metrics, null, 2) }] };
});
```

---

## Troubleshooting

### Common Issues

**1. "Ollama server not responding"**
```powershell
# Check if Ollama is running
Get-Process ollama

# Restart service
Restart-Service Ollama

# Or start manually
ollama serve
```

**2. "Model not found"**
```powershell
ollama list  # Check available models
ollama pull qwen2.5-coder:7b  # Re-download if missing
```

**3. "WSL2 can't connect to Windows"**
```bash
# Test localhost connectivity
curl http://localhost:11434/api/tags

# If fails, check Windows firewall
# Allow inbound on port 11434
```

**4. "Out of VRAM"**
```powershell
# Check VRAM usage
nvidia-smi

# If at 11GB limit, try smaller model:
ollama pull qwen2.5-coder:3b  # Only 3GB VRAM
```

**5. "Slow inference (>5s for small prompts)"**
- Check GPU layers: Should be 35 for qwen2.5-coder:7b
- Verify GPU is being used: `nvidia-smi` should show activity
- Check temperature: Thermal throttling if >85°C
- Try smaller context window: `num_ctx: 2048` instead of 4096

---

## Future Enhancements

### Phase 2 Features (Optional)

1. **Streaming Responses:**
   - Implement SSE streaming for long generations
   - Show progress in real-time

2. **Response Caching:**
   - Cache common queries (e.g., "extract errors from this log format")
   - Redis-backed cache for multi-session persistence

3. **Model Selection:**
   - Support multiple models (qwen, codellama, mistral)
   - Auto-select model based on task type

4. **Fine-Tuning:**
   - Train on project-specific logs for better accuracy
   - Custom adapters for domain-specific code review

5. **Batch Processing API:**
   - Queue multiple requests
   - Process in parallel with GPU batching
   - Priority queue for interactive vs background tasks

---

## Summary

### What This Achieves

✓ **Fast:** 20-50 tok/s with GPU acceleration
✓ **Free:** No API costs, runs locally
✓ **Private:** Code/logs never leave machine
✓ **Flexible:** Universal log format support
✓ **Optimized:** Token-aware chunking and processing
✓ **Scalable:** Parallel processing for bulk operations

### Time Investment

- Initial setup: 3-3.5 hours
- Ongoing maintenance: ~15 min/month (updates)

### ROI Analysis

**Savings vs Claude API (typical usage):**
- 1000 log analyses/month: ~$50-150 saved
- 500 code reviews/month: ~$30-100 saved
- Daily commit messages: ~$10-30 saved

**Total:** $90-280/month savings for typical development workflow

### Next Steps

1. Confirm all requirements and answers provided
2. Install Ollama on Windows
3. Create project structure at `G:\repos\wsl-mcp-ollama`
4. Implement MCP server following this plan
5. Test and validate performance
6. Integrate into daily workflow

---

**Plan Status:** Ready for implementation
**Estimated Completion:** 3.5 hours
**Primary Benefits:** Fast, free, private code/log analysis
