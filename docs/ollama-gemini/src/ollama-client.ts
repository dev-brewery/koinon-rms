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
