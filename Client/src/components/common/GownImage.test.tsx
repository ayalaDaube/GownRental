import { fireEvent, render, screen } from '@testing-library/react';
import GownImage, { fallbackImage } from './GownImage';

describe('the picture of a gown', () => {
  test('shows the gown\'s own picture when it has one', () => {
    render(<GownImage gownId={1} name="Simple Gown" src="https://example.com/gown.jpg" />);

    expect(screen.getByRole('img', { name: 'Simple Gown' })).toHaveAttribute('src', 'https://example.com/gown.jpg');
  });

  test.each([null, undefined, ''])('shows a bundled picture when there is none (%p)', (src) => {
    render(<GownImage gownId={2} name="Simple Gown" src={src} />);

    expect(screen.getByRole('img', { name: 'Simple Gown' })).toHaveAttribute('src', fallbackImage(2));
  });

  test('switches to the bundled picture when the address cannot be loaded', () => {
    render(<GownImage gownId={3} name="Simple Gown" src="https://example.com/missing.jpg" />);

    fireEvent.error(screen.getByRole('img', { name: 'Simple Gown' }));

    expect(screen.getByRole('img', { name: 'Simple Gown' })).toHaveAttribute('src', fallbackImage(3));
  });

  test('a corrected address is tried again after an earlier failure', () => {
    const { rerender } = render(<GownImage gownId={3} name="Simple Gown" src="https://example.com/missing.jpg" />);
    fireEvent.error(screen.getByRole('img', { name: 'Simple Gown' }));

    rerender(<GownImage gownId={3} name="Simple Gown" src="https://example.com/fixed.jpg" />);

    expect(screen.getByRole('img', { name: 'Simple Gown' })).toHaveAttribute('src', 'https://example.com/fixed.jpg');
  });

  test('every gown gets one of the three bundled pictures, whatever its id', () => {
    const urls = [0, 1, 2, 3, 4, 5, 1000, -7].map(fallbackImage);

    urls.forEach(u => expect(u).toMatch(/\/assets\/dress[123]\.svg$/));
    expect(new Set([1, 2, 3].map(fallbackImage)).size).toBe(3);
  });
});
