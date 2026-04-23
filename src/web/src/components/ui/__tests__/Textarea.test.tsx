import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { Textarea } from '../Textarea';

describe('Textarea', () => {
  it('renders without label', () => {
    render(<Textarea placeholder="Enter text" />);
    expect(screen.getByPlaceholderText('Enter text')).toBeInTheDocument();
  });

  it('renders with label and proper htmlFor association', () => {
    render(<Textarea label="Description" />);
    const label = screen.getByText('Description');
    const textarea = screen.getByRole('textbox');
    expect(label).toHaveAttribute('for', textarea.id);
  });

  it('uses provided id for label association', () => {
    render(<Textarea id="custom-id" label="Notes" />);
    const label = screen.getByText('Notes');
    expect(label).toHaveAttribute('for', 'custom-id');
    expect(screen.getByRole('textbox')).toHaveAttribute('id', 'custom-id');
  });

  it('displays error message with aria-describedby', () => {
    render(<Textarea label="Bio" error="Required field" />);
    const textarea = screen.getByRole('textbox');
    const error = screen.getByText('Required field');

    expect(textarea).toHaveAttribute('aria-invalid', 'true');
    expect(textarea).toHaveAttribute('aria-describedby', error.id);
    expect(error).toHaveAttribute('role', 'alert');
  });

  it('does not set aria-invalid when no error', () => {
    render(<Textarea label="Bio" />);
    const textarea = screen.getByRole('textbox');
    expect(textarea).not.toHaveAttribute('aria-invalid');
    expect(textarea).not.toHaveAttribute('aria-describedby');
  });

  it('applies error styling when error is present', () => {
    render(<Textarea error="Invalid" />);
    const textarea = screen.getByRole('textbox');
    expect(textarea).toHaveClass('border-red-500');
  });

  it('forwards ref to textarea element', () => {
    const ref = { current: null as HTMLTextAreaElement | null };
    render(<Textarea ref={ref} />);
    expect(ref.current).toBeInstanceOf(HTMLTextAreaElement);
  });

  it('passes through additional props', () => {
    render(<Textarea rows={5} maxLength={500} disabled />);
    const textarea = screen.getByRole('textbox');
    expect(textarea).toHaveAttribute('rows', '5');
    expect(textarea).toHaveAttribute('maxlength', '500');
    expect(textarea).toBeDisabled();
  });
});
