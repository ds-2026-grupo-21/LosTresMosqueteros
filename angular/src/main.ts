import { provideZoneChangeDetection } from "@angular/core";
import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';
import { HttpErrorReporterService } from '@abp/ng.core';
import { showStartupError, StartupErrorReporter } from './app/startup-error';

bootstrapApplication(AppComponent, {...appConfig, providers: [provideZoneChangeDetection(), ...appConfig.providers]})
  .then(app => (app.injector.get(HttpErrorReporterService) as StartupErrorReporter).applicationStarted())
  .catch(() => showStartupError(document, () => window.location.reload()));
