/**
 * PhotoUpload tests
 *
 * Exercises validation, drag-drop, file-input, and preview branches:
 *   - Default render shows dropzone + "Browse Files".
 *   - Drag enter / leave toggles the blue border state.
 *   - validateFile: rejects unknown types, rejects files > maxSize.
 *   - handleFile on valid file: creates object URL, shows preview, fires onFileSelect.
 *   - Clear preview resets state and revokes the URL.
 *   - File input onChange path picks the first file.
 *   - Error banner shows last validation error.
 */
import { act, fireEvent, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { PhotoUpload } from '../PhotoUpload';

const makeFile = (
  name: string,
  size: number,
  type: string = 'image/png'
): File => {
  const file = new File(['x'.repeat(size)], name, { type });
  // In happy-dom File `size` is inferred from content length; force if needed.
  Object.defineProperty(file, 'size', { value: size });
  return file;
};

describe('PhotoUpload', () => {
  beforeEach(() => {
    // Provide object URL stubs; happy-dom has them but restore cleanly between tests.
    const urls: string[] = [];
    vi.spyOn(URL, 'createObjectURL').mockImplementation((_obj) => {
      const url = `blob:mock-${urls.length}`;
      urls.push(url);
      return url;
    });
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders dropzone with Browse Files CTA by default', () => {
    render(<PhotoUpload onFileSelect={vi.fn()} />);
    expect(screen.getByText(/drag and drop/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /browse files/i })).toBeInTheDocument();
  });

  it('drag enter/leave toggle the dragging class on the dropzone', () => {
    const { container } = render(<PhotoUpload onFileSelect={vi.fn()} />);
    const zone = container.querySelector('[class*="border-dashed"]') as HTMLElement;
    expect(zone).toBeTruthy();
    act(() => {
      zone.dispatchEvent(new DragEvent('dragenter', { bubbles: true }));
    });
    expect(zone.className).toMatch(/border-blue-500/);
    act(() => {
      zone.dispatchEvent(new DragEvent('dragleave', { bubbles: true }));
    });
    expect(zone.className).not.toMatch(/border-blue-500/);
  });

  it('rejects invalid file types with an error banner', async () => {
    const onFileSelect = vi.fn();
    render(<PhotoUpload onFileSelect={onFileSelect} />);
    const input = screen.getByLabelText(/file input/i) as HTMLInputElement;
    const bad = makeFile('evil.txt', 100, 'text/plain');
    // fireEvent.change bypasses the browser's accept-attribute gate that
    // userEvent.upload enforces; the component still receives the onChange
    // event and runs its own validateFile().
    act(() => {
      fireEvent.change(input, { target: { files: [bad] } });
    });
    expect(screen.getByText(/please select a valid image file/i)).toBeInTheDocument();
    expect(onFileSelect).not.toHaveBeenCalled();
  });

  it('rejects files larger than maxSizeMB', async () => {
    const onFileSelect = vi.fn();
    render(<PhotoUpload onFileSelect={onFileSelect} maxSizeMB={1} />);
    const input = screen.getByLabelText(/file input/i) as HTMLInputElement;
    // 2 MB file while maxSize=1 MB.
    const big = makeFile('big.png', 2 * 1024 * 1024, 'image/png');
    await act(async () => {
      await userEvent.upload(input, big);
    });
    expect(screen.getByText(/must be less than 1mb/i)).toBeInTheDocument();
    expect(onFileSelect).not.toHaveBeenCalled();
  });

  it('happy path: valid file creates preview and calls onFileSelect', async () => {
    const onFileSelect = vi.fn();
    render(<PhotoUpload onFileSelect={onFileSelect} />);
    const input = screen.getByLabelText(/file input/i) as HTMLInputElement;
    const good = makeFile('pic.png', 500, 'image/png');
    await act(async () => {
      await userEvent.upload(input, good);
    });
    expect(onFileSelect).toHaveBeenCalledWith(good);
    // Preview image renders
    const preview = screen.getByRole('img', { name: /preview/i });
    expect(preview).toHaveAttribute('src', expect.stringContaining('blob:mock'));
  });

  it('Clear preview button revokes URL and returns to dropzone', async () => {
    const user = userEvent.setup();
    render(<PhotoUpload onFileSelect={vi.fn()} />);
    const input = screen.getByLabelText(/file input/i) as HTMLInputElement;
    await act(async () => {
      await userEvent.upload(input, makeFile('p.png', 100, 'image/png'));
    });
    expect(screen.getByRole('img', { name: /preview/i })).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /clear preview/i }));
    expect(screen.queryByRole('img', { name: /preview/i })).toBeNull();
    expect(URL.revokeObjectURL).toHaveBeenCalled();
  });

  it('Browse Files click triggers the hidden file input', async () => {
    const user = userEvent.setup();
    render(<PhotoUpload onFileSelect={vi.fn()} />);
    const input = screen.getByLabelText(/file input/i) as HTMLInputElement;
    const spy = vi.spyOn(input, 'click');
    await user.click(screen.getByRole('button', { name: /browse files/i }));
    expect(spy).toHaveBeenCalled();
  });

  it('drop event with a file triggers validation + onFileSelect', () => {
    const onFileSelect = vi.fn();
    const { container } = render(<PhotoUpload onFileSelect={onFileSelect} />);
    const zone = container.querySelector('[class*="border-dashed"]') as HTMLElement;
    const file = makeFile('dragged.png', 100, 'image/png');
    const dropEvent = new DragEvent('drop', { bubbles: true });
    // Attach files to the event
    Object.defineProperty(dropEvent, 'dataTransfer', {
      value: { files: [file] },
    });
    act(() => {
      zone.dispatchEvent(dropEvent);
    });
    expect(onFileSelect).toHaveBeenCalledWith(file);
  });

  it('drop with no files is a no-op', () => {
    const onFileSelect = vi.fn();
    const { container } = render(<PhotoUpload onFileSelect={onFileSelect} />);
    const zone = container.querySelector('[class*="border-dashed"]') as HTMLElement;
    const dropEvent = new DragEvent('drop', { bubbles: true });
    Object.defineProperty(dropEvent, 'dataTransfer', {
      value: { files: [] },
    });
    act(() => {
      zone.dispatchEvent(dropEvent);
    });
    expect(onFileSelect).not.toHaveBeenCalled();
  });
});
