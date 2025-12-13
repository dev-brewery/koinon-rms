// Token estimation (rough approximation)
export function estimateTokens(text: string): number {
  // QWEN uses ~4 chars per token on average for code
  return Math.ceil(text.length / 4);
}

// Smart chunking for large inputs
export function chunkText(text: string, maxTokens: number = 4096): string[] {
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
