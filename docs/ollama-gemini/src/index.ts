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
import { spawn, exec } from 'child_process';
import { promisify } from 'util';
import fetch from 'node-fetch';
import { zodToJsonSchema } from 'zod-to-json-schema';

const execPromise = promisify(exec);

// --- Robust Ollama Launcher ---

async function stopAllOllamaProcesses(): Promise<void> {
    console.error('[OllamaManager] Forcefully stopping any existing Ollama processes to ensure a clean start...');
    try {
        await execPromise('taskkill /im ollama.exe /f');
        console.error('[OllamaManager] Cleanup command executed. Any running Ollama processes should now be stopped.');
        await new Promise(resolve => setTimeout(resolve, 2000));
    } catch (error: unknown) {
        if (error instanceof Error && error.message.includes('not found')) {
            console.error('[OllamaManager] No pre-existing Ollama processes found to stop, which is good.');
        } else if (error instanceof Error) {
            console.error(`[OllamaManager] An unexpected error occurred while trying to stop Ollama processes: ${error.message}`);
        } else {
            console.error(`[OllamaManager] An unknown error occurred while trying to stop Ollama processes: ${String(error)}`);
        }
    }
}

async function startOllamaProcess(): Promise<void> {
    console.error('[OllamaManager] Spawning a new "ollama serve" process...');
    const ollamaProcess = spawn('ollama', ['serve'], { detached: true, stdio: 'ignore' });
    ollamaProcess.unref();
    console.error(`[OllamaManager] New "ollama serve" process spawned.`);
    let attempts = 0;
    const maxAttempts = 15;
    while (attempts < maxAttempts) {
        console.error(`[OllamaManager] Waiting for new Ollama process to respond... (Attempt ${attempts + 1})`);
        try {
            const response = await fetch('http://localhost:11434/api/tags', { signal: AbortSignal.timeout(1000) });
            if (response.ok) {
                console.error('[OllamaManager] New Ollama process is responsive.');
                return;
            }
        } catch (e) { /* Errors are expected while the server starts up. */ }
        await new Promise(resolve => setTimeout(resolve, 1000));
        attempts++;
    }
    throw new Error('The new "ollama serve" process did not become responsive within 15 seconds.');
}

async function ensureOllamaIsRunning(): Promise<void> {
    await stopAllOllamaProcesses();
    await startOllamaProcess();
}


// --- MCP Server Configuration ---

// Define the tools with their schemas converted to JSON Schema.
const ollamaToolDefinitions = {
    'ollama_generate': {
        description: 'General-purpose text generation with QWEN2.5-coder. Auto-chunks large inputs.',
        inputSchema: zodToJsonSchema(GenerateSchema)
    },
    'ollama_analyze_logs': {
        description: 'Universal log analysis: extract errors, summarize, find patterns. Supports any log format.',
        inputSchema: zodToJsonSchema(AnalyzeLogsSchema)
    },
    'ollama_review_code': {
        description: 'Fast code review focusing on bugs, style, performance, or security.',
        inputSchema: zodToJsonSchema(ReviewCodeSchema)
    },
    'ollama_chat': {
        description: 'Multi-turn conversation with context management.',
        inputSchema: zodToJsonSchema(ChatSchema)
    }
};

const server = new Server(
  {
    name: 'wsl-mcp-ollama-server',
    version: '1.0.0',
  },
  {
    capabilities: {
      // FIX: Declare the tools here during initialization.
      tools: ollamaToolDefinitions
    },
  }
);

const ollama = new OllamaClient(
  process.env.OLLAMA_HOST || 'http://localhost:11434',
  'qwen2.5-coder:7b'
);

// This handler is used if the client asks for the tool list after initialization.
server.setRequestHandler(ListToolsRequestSchema, async () => {
  const toolList = Object.entries(ollamaToolDefinitions).map(([name, def]) => ({
      name,
      description: def.description,
      inputSchema: def.inputSchema
  }));
  return { tools: toolList };
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
        
        const formatToUse = (format_hint && format_hint !== 'auto') ? format_hint : detectLogFormat(logs);
        const { prompt, system } = buildLogAnalysisPrompt(logs, task, formatToUse);

        console.error(`[ollama_analyze_logs] Format: ${formatToUse}, Task: ${task}`);

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
  await ensureOllamaIsRunning();
  
  const transport = new StdioServerTransport();
  await server.connect(transport);

  console.error('WSL MCP Ollama Server running on stdio');
  console.error('Model: qwen2.5-coder:7b');
  console.error('Optimized for: Fast token processing (512-4096 token range)');
}

main().catch((error) => {
  console.error('SERVER STARTUP FAILED:', error.message);
  process.exit(1);
});