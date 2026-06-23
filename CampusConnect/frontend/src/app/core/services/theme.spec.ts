import { TestBed } from '@angular/core/testing';
import { Theme } from './theme';

describe('Theme', () => {
  let mediaListener: ((event: MediaQueryListEvent) => void) | null;

  beforeEach(() => {
    TestBed.resetTestingModule();
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
    document.documentElement.style.colorScheme = '';
    mockMatchMedia(false);
  });

  it('defaults to system preference and applies the current system theme', () => {
    const service = TestBed.inject(Theme);

    expect(service.preference()).toBe('system');
    expect(service.effectiveTheme()).toBe('light');
    expect(document.documentElement.dataset['theme']).toBe('light');
    expect(document.documentElement.style.colorScheme).toBe('light');
  });

  it('follows system changes while preference is system', () => {
    const service = TestBed.inject(Theme);

    emitSystemTheme(true);

    expect(service.preference()).toBe('system');
    expect(service.effectiveTheme()).toBe('dark');
    expect(document.documentElement.dataset['theme']).toBe('dark');
  });

  it('loads a stored theme preference', () => {
    localStorage.setItem('campusconnect.theme', 'dark');

    const service = TestBed.inject(Theme);

    expect(service.preference()).toBe('dark');
    expect(service.effectiveTheme()).toBe('dark');
    expect(document.documentElement.dataset['theme']).toBe('dark');
  });

  it('stores and applies an explicit theme preference', () => {
    const service = TestBed.inject(Theme);

    service.setPreference('dark');

    expect(service.preference()).toBe('dark');
    expect(localStorage.getItem('campusconnect.theme')).toBe('dark');
    expect(document.documentElement.dataset['theme']).toBe('dark');
  });

  it('ignores invalid theme preferences', () => {
    const service = TestBed.inject(Theme);

    service.setPreference('sepia');

    expect(service.preference()).toBe('system');
    expect(localStorage.getItem('campusconnect.theme')).toBeNull();
  });

  function mockMatchMedia(matches: boolean): void {
    mediaListener = null;
    const mediaQuery = {
      matches,
      media: '(prefers-color-scheme: dark)',
      onchange: null,
      addEventListener: vi.fn((_event: string, listener: (event: MediaQueryListEvent) => void) => {
        mediaListener = listener;
      }),
      removeEventListener: vi.fn(),
      addListener: vi.fn(),
      removeListener: vi.fn(),
      dispatchEvent: vi.fn(),
    } as unknown as MediaQueryList;

    Object.defineProperty(globalThis, 'matchMedia', {
      configurable: true,
      value: vi.fn(() => mediaQuery),
    });
  }

  function emitSystemTheme(matches: boolean): void {
    mediaListener?.({ matches } as MediaQueryListEvent);
  }
});
