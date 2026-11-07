import { afterEach, describe, expect, it, vi } from 'vitest';
import { HttpErrorResponse } from '@angular/common/http';
import { showStartupError, StartupErrorReporter } from './startup-error';

describe('showStartupError', () => {
  afterEach(() => {
    document.cookie = '.AspNetCore.Culture=; Max-Age=0; path=/';
    document.body.replaceChildren();
  });

  it('muestra un mensaje en español y permite reintentar', () => {
    document.cookie = '.AspNetCore.Culture=c%3Des%7Cuic%3Des; path=/';
    document.body.innerHTML = '<app-root></app-root><abp-http-error-wrapper>Detalle técnico</abp-http-error-wrapper>';
    const retry = vi.fn();

    showStartupError(document, retry);

    expect(document.querySelector('[role="alert"]')?.textContent).toContain('No pudimos iniciar la aplicación');
    expect(document.body.textContent).not.toContain('Detalle técnico');
    document.querySelector<HTMLButtonElement>('button')!.click();
    expect(retry).toHaveBeenCalledOnce();
  });

  it('muestra el mensaje en inglés cuando la cultura es inglés', () => {
    document.cookie = '.AspNetCore.Culture=c%3Den%7Cuic%3Den; path=/';
    document.body.innerHTML = '<app-root></app-root>';

    showStartupError(document, vi.fn());

    expect(document.querySelector('[role="alert"]')?.textContent).toContain('We could not start the application');
    expect(document.querySelector('button')?.textContent).toBe('Retry');
  });

  it('maneja un error de conexión durante el inicio y conserva el manejo de ABP después', () => {
    document.body.innerHTML = '<app-root></app-root>';
    const reporter = new StartupErrorReporter();
    const reported = vi.fn();
    reporter.reporter$.subscribe(reported);
    const error = new HttpErrorResponse({ status: 0 });

    reporter.reportError(error);
    expect(document.querySelector('[role="alert"]')).not.toBeNull();
    expect(reported).not.toHaveBeenCalled();

    reporter.applicationStarted();
    reporter.reportError(error);
    expect(reported).toHaveBeenCalledWith(error);
  });
});
