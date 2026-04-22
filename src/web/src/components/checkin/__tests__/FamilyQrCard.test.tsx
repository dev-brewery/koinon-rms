/**
 * FamilyQrCard tests
 *
 * Catches:
 *  - Renders family name and family ID.
 *  - Encodes koinon://family/{idKey} into the QR value (regression on URL scheme).
 *  - Print button visibility respects the showPrintButton prop.
 *  - Clicking Print triggers window.print().
 */
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';

// Stub qrcode.react before importing the component under test.
vi.mock('qrcode.react', () => ({
  QRCodeSVG: ({ value, size }: { value: string; size: number }) => (
    <svg data-testid="qr" data-value={value} data-size={size} />
  ),
}));

import { FamilyQrCard } from '../FamilyQrCard';

describe('FamilyQrCard', () => {
  it('renders the family name and family id key', () => {
    render(<FamilyQrCard familyIdKey="FAM123" familyName="The Smiths" />);
    expect(screen.getByRole('heading', { name: 'The Smiths' })).toBeInTheDocument();
    expect(screen.getByText(/FAM123/)).toBeInTheDocument();
  });

  it('encodes the family id with the koinon URL scheme in the QR value', () => {
    render(<FamilyQrCard familyIdKey="FAM123" familyName="The Smiths" />);
    const qr = screen.getByTestId('qr');
    expect(qr.getAttribute('data-value')).toBe('koinon://family/FAM123');
  });

  it('passes the qrSize prop through to the QR renderer', () => {
    render(
      <FamilyQrCard familyIdKey="A" familyName="B" qrSize={400} />,
    );
    const qr = screen.getByTestId('qr');
    expect(qr.getAttribute('data-size')).toBe('400');
  });

  it('shows the print button by default and calls window.print when clicked', async () => {
    const printSpy = vi.fn();
    const original = window.print;
    window.print = printSpy;

    const user = userEvent.setup();
    render(<FamilyQrCard familyIdKey="A" familyName="B" />);
    await user.click(screen.getByRole('button', { name: /print qr code/i }));
    expect(printSpy).toHaveBeenCalled();

    window.print = original;
  });

  it('hides the print button when showPrintButton=false', () => {
    render(<FamilyQrCard familyIdKey="A" familyName="B" showPrintButton={false} />);
    expect(screen.queryByRole('button', { name: /print qr code/i })).toBeNull();
  });
});
