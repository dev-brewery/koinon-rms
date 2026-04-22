/**
 * fileUtils tests
 */

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { triggerFileDownload } from '../fileUtils';

describe('triggerFileDownload', () => {
  let createObjectURLSpy: ReturnType<typeof vi.fn>;
  let revokeObjectURLSpy: ReturnType<typeof vi.fn>;
  let appendSpy: ReturnType<typeof vi.spyOn>;
  let removeSpy: ReturnType<typeof vi.spyOn>;
  let clickSpy: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    createObjectURLSpy = vi.fn(() => 'blob:mock-url');
    revokeObjectURLSpy = vi.fn();
    (globalThis.URL as unknown as { createObjectURL: typeof createObjectURLSpy }).createObjectURL = createObjectURLSpy;
    (globalThis.URL as unknown as { revokeObjectURL: typeof revokeObjectURLSpy }).revokeObjectURL = revokeObjectURLSpy;

    clickSpy = vi.fn();
    appendSpy = vi.spyOn(document.body, 'appendChild');
    removeSpy = vi.spyOn(document.body, 'removeChild');

    const originalCreate = document.createElement.bind(document);
    vi.spyOn(document, 'createElement').mockImplementation(((tag: string) => {
      const el = originalCreate(tag);
      if (tag === 'a') {
        (el as HTMLAnchorElement).click = clickSpy as unknown as () => void;
      }
      return el;
    }) as typeof document.createElement);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('creates an anchor, clicks it, and revokes the blob URL', () => {
    const blob = new Blob(['hello'], { type: 'text/plain' });
    triggerFileDownload(blob, 'hello.txt');

    expect(createObjectURLSpy).toHaveBeenCalledWith(blob);
    expect(appendSpy).toHaveBeenCalled();
    expect(clickSpy).toHaveBeenCalledTimes(1);
    expect(removeSpy).toHaveBeenCalled();
    expect(revokeObjectURLSpy).toHaveBeenCalledWith('blob:mock-url');
  });
});
