import { z } from 'zod';

export const GenerateSchema = z.object({
  prompt: z.string(),
  system: z.string().optional(),
  temperature: z.number().min(0).max(2).default(0.7),
  max_tokens: z.number().min(1).max(4096).default(1024),
  auto_chunk: z.boolean().default(true)
});

export const AnalyzeLogsSchema = z.object({
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

export const ReviewCodeSchema = z.object({
  code: z.string(),
  language: z.string(),
  focus: z.enum([
    'bugs',           // Logic errors, null refs, edge cases
    'style',          // Code style, naming, formatting
    'performance',    // Performance issues, N+1, blocking calls
    'security',       // Security vulnerabilities
    'all'             // Comprehensive review
  ]).default('all'),
  max_tokens: z.number().min(512).max(4096).default(2048)
});

export const ChatSchema = z.object({
  messages: z.array(z.object({
    role: z.enum(['user', 'assistant']),
    content: z.string()
  })),
  temperature: z.number().min(0).max(2).default(0.7),
  max_tokens: z.number().min(128).max(2048).default(512)
});
