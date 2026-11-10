import english from '../../../src/LosTresMosqueteros.Domain.Shared/Localization/LosTresMosqueteros/en.json';
import spanish from '../../../src/LosTresMosqueteros.Domain.Shared/Localization/LosTresMosqueteros/es.json';
import { HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { HttpErrorReporterService } from '@abp/ng.core';

@Injectable()
export class StartupErrorReporter extends HttpErrorReporterService {
  private starting = true;

  applicationStarted(): void {
    this.starting = false;
  }

  override reportError(error: HttpErrorResponse): void {
    if (this.starting && error.status === 0) {
      showStartupError(document, () => window.location.reload());
      return;
    }
    super.reportError(error);
  }
}

export function showStartupError(document: Document, retry: () => void): void {
  const culture = document.cookie.split('; ').find(cookie => cookie.startsWith('.AspNetCore.Culture='));
  const language = culture ? decodeURIComponent(culture.slice(culture.indexOf('=') + 1)) : '';
  const texts = /(?:^|\|)uic=es(?:-|\||$)/i.test(language) ? spanish.texts : english.Texts;

  document.querySelectorAll('abp-http-error-wrapper').forEach(element => element.remove());
  const root = document.querySelector('app-root');
  if (!root) return;

  const container = document.createElement('main');
  container.className = 'startup-error';
  container.setAttribute('role', 'alert');
  const message = document.createElement('p');
  message.textContent = texts['Pantry:StartupError'];
  const button = document.createElement('button');
  button.type = 'button';
  button.className = 'btn btn-primary';
  button.textContent = texts['Pantry:Retry'];
  button.addEventListener('click', retry);
  container.append(message, button);
  root.replaceChildren(container);
}
