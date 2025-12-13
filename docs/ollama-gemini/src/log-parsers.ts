// Define specific types for the keys of our lookup objects.
// This tells TypeScript that 'format' and 'task' can only be one of these specific strings.
const formatContext = {
    json: 'JSON-structured logs with timestamp, level, and message fields',
    apache: 'Apache web server access/error logs with IP, timestamp, request, status',
    nginx: 'Nginx web server logs with similar structure to Apache',
    syslog: 'System logs with facility, severity, hostname, and message',
    application: 'Application logs with timestamps and free-form messages',
    stacktrace: 'Exception stack traces with file names, line numbers, and call stacks'
  };
  
const taskPrompts = {
    extract_errors: `Extract ALL errors, exceptions, and failures from these logs. Return a structured list with:
- Timestamp (if available)
- Error type/severity
- Error message
- Context (file, line, method if available)
- Count (if error repeats)`,

    summarize: `Provide a concise summary of these logs covering:
- Overall status (healthy/degraded/failing)
- Key events and milestones
- Error count and types
- Performance indicators
- Notable patterns`,

    find_patterns: `Analyze these logs for recurring patterns:
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

    extract_metrics: `Extract performance metrics from these logs:
- Response times / latency
- Throughput (requests per second)
- Error rates
- Resource usage (if logged)
- 95th/99th percentile times`,

    trace_requests: `Trace request flows through these logs:
- Request IDs or correlation IDs
- Full request lifecycle (start → processing → completion)
- Cross-service calls
- Failed requests and their causes`
  };

type LogFormat = keyof typeof formatContext;
type LogTask = keyof typeof taskPrompts;


// src/log-parsers.ts
export function detectLogFormat(logs: string): LogFormat {
  // JSON logs
  if (logs.trim().startsWith('{') || logs.includes('"timestamp":')) {
    return 'json';
  }

  // Apache/Nginx access logs
  if (/\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}.*\[.*\].*"(GET|POST|PUT|DELETE)/.test(logs)) {
    return logs.includes('nginx') ? 'nginx' : 'apache';
  }

  // Syslog
  if (/^[A-Z][a-z]{2}\s+\d{1,2}\s+\d{2}:\d{2}:\d{2}/.test(logs)) {
    return 'syslog';
  }

  // Stack traces
  if (logs.includes('Traceback') || logs.includes('at ') && logs.includes('.cs:line')) {
    return 'stacktrace';
  }

  // Default to generic application logs
  return 'application';
}

export function buildLogAnalysisPrompt(
  logs: string,
  task: LogTask,
  format: LogFormat
): { prompt: string; system: string } {

  return {
    prompt: logs,
    system: `You are a log analysis expert specialized in ${format} format logs.

${formatContext[format] || formatContext.application}

Task: ${taskPrompts[task] || taskPrompts.extract_errors}

Be precise, factual, and structured in your response. Extract data, don't speculate.`
  };
}