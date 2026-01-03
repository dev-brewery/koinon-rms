/**
 * MergeFieldPicker Component Tests
 */

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MergeFieldPicker } from '../MergeFieldPicker';
import type { MergeField } from '../MergeFieldPicker';
import * as useCommunicationsModule from '@/hooks/useCommunications';

// Mock merge fields data
const mockMergeFields: MergeField[] = [
  { name: 'FirstName', token: '{{FirstName}}', description: "Recipient's first name" },
  { name: 'LastName', token: '{{LastName}}', description: "Recipient's last name" },
  { name: 'NickName', token: '{{NickName}}', description: "Recipient's nickname (falls back to first name)" },
  { name: 'FullName', token: '{{FullName}}', description: "Recipient's full name" },
  { name: 'Email', token: '{{Email}}', description: "Recipient's email address" },
];

// Mock the useMergeFields hook
vi.mock('@/hooks/useCommunications', async () => {
  const actual = await vi.importActual('@/hooks/useCommunications');
  return {
    ...actual,
    useMergeFields: vi.fn(),
  };
});

describe('MergeFieldPicker', () => {
  beforeEach(() => {
    // Setup the mock to return merge fields data
    vi.mocked(useCommunicationsModule.useMergeFields).mockReturnValue({
      data: mockMergeFields,
      isLoading: false,
      isError: false,
      error: null,
    } as ReturnType<typeof useCommunicationsModule.useMergeFields>);
  });

  it('renders the button with correct label', () => {
    const mockOnInsert = vi.fn();
    render(<MergeFieldPicker onInsert={mockOnInsert} />);

    const button = screen.getByRole('button', { name: /insert merge field/i });
    expect(button).toBeTruthy();
    expect(button.textContent).toContain('Insert Field');
  });

  it('opens dropdown menu when button is clicked', () => {
    const mockOnInsert = vi.fn();
    render(<MergeFieldPicker onInsert={mockOnInsert} />);

    const button = screen.getByRole('button', { name: /insert merge field/i });
    fireEvent.click(button);

    // Check for menu items
    expect(screen.getByText('FirstName')).toBeTruthy();
    expect(screen.getByText('LastName')).toBeTruthy();
    expect(screen.getByText('Email')).toBeTruthy();
  });

  it('calls onInsert with correct token when field is clicked', () => {
    const mockOnInsert = vi.fn();
    render(<MergeFieldPicker onInsert={mockOnInsert} />);

    // Open menu
    const button = screen.getByRole('button', { name: /insert merge field/i });
    fireEvent.click(button);

    // Click on FirstName field
    const firstNameButton = screen.getByText('FirstName').closest('button');
    if (firstNameButton) {
      fireEvent.click(firstNameButton);
    }

    expect(mockOnInsert).toHaveBeenCalledWith('{{FirstName}}');
  });

  it('closes menu after field is selected', async () => {
    const mockOnInsert = vi.fn();
    render(<MergeFieldPicker onInsert={mockOnInsert} />);

    // Open menu
    const button = screen.getByRole('button', { name: /insert merge field/i });
    fireEvent.click(button);

    // Click on a field
    const firstNameButton = screen.getByText('FirstName').closest('button');
    if (firstNameButton) {
      fireEvent.click(firstNameButton);
    }

    // Menu should close - FirstName should no longer be in the document
    await waitFor(() => {
      expect(screen.queryByText('FirstName')).toBeNull();
    });
  });

  it('closes menu when Escape key is pressed', async () => {
    const mockOnInsert = vi.fn();
    render(<MergeFieldPicker onInsert={mockOnInsert} />);

    // Open menu
    const button = screen.getByRole('button', { name: /insert merge field/i });
    fireEvent.click(button);

    // Verify menu is open
    expect(screen.getByText('FirstName')).toBeTruthy();

    // Press Escape
    fireEvent.keyDown(document, { key: 'Escape' });

    // Menu should close
    await waitFor(() => {
      expect(screen.queryByText('FirstName')).toBeNull();
    });
  });

  it('is disabled when disabled prop is true', () => {
    const mockOnInsert = vi.fn();
    render(<MergeFieldPicker onInsert={mockOnInsert} disabled />);

    const button = screen.getByRole('button', { name: /insert merge field/i });
    expect(button.hasAttribute('disabled')).toBe(true);
  });

  it('displays field descriptions and tokens', () => {
    const mockOnInsert = vi.fn();
    render(<MergeFieldPicker onInsert={mockOnInsert} />);

    // Open menu
    const button = screen.getByRole('button', { name: /insert merge field/i });
    fireEvent.click(button);

    // Check that descriptions are shown
    expect(screen.getByText("Recipient's first name")).toBeTruthy();
    expect(screen.getByText("Recipient's email address")).toBeTruthy();

    // Check that tokens are shown
    expect(screen.getByText('{{FirstName}}')).toBeTruthy();
    expect(screen.getByText('{{Email}}')).toBeTruthy();
  });

  it('has proper ARIA attributes for accessibility', () => {
    const mockOnInsert = vi.fn();
    render(<MergeFieldPicker onInsert={mockOnInsert} />);

    const button = screen.getByRole('button', { name: /insert merge field/i });
    
    // Check ARIA attributes when closed
    expect(button.getAttribute('aria-haspopup')).toBe('true');
    expect(button.getAttribute('aria-expanded')).toBe('false');

    // Open menu
    fireEvent.click(button);

    // Check ARIA attributes when open
    expect(button.getAttribute('aria-expanded')).toBe('true');
    
    // Check menu has role
    const menu = screen.getByRole('menu');
    expect(menu).toBeTruthy();
  });

  it('supports keyboard navigation with Enter key', () => {
    const mockOnInsert = vi.fn();
    render(<MergeFieldPicker onInsert={mockOnInsert} />);

    // Open menu
    const button = screen.getByRole('button', { name: /insert merge field/i });
    fireEvent.click(button);

    // Find a menu item and press Enter
    const firstNameButton = screen.getByText('FirstName').closest('button');
    if (firstNameButton) {
      fireEvent.keyDown(firstNameButton, { key: 'Enter' });
    }

    expect(mockOnInsert).toHaveBeenCalledWith('{{FirstName}}');
  });

  it('supports keyboard navigation with Space key', () => {
    const mockOnInsert = vi.fn();
    render(<MergeFieldPicker onInsert={mockOnInsert} />);

    // Open menu
    const button = screen.getByRole('button', { name: /insert merge field/i });
    fireEvent.click(button);

    // Find a menu item and press Space
    const lastNameButton = screen.getByText('LastName').closest('button');
    if (lastNameButton) {
      fireEvent.keyDown(lastNameButton, { key: ' ' });
    }

    expect(mockOnInsert).toHaveBeenCalledWith('{{LastName}}');
  });
});
