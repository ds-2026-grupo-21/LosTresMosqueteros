import { LocalizationPipe } from '@abp/ng.core';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { PreferenceService } from '@proxy/preferences';

@Component({
  selector: 'app-preferences',
  imports: [LocalizationPipe, ReactiveFormsModule, RouterLink],
  templateUrl: './preferences.component.html',
  styleUrl: './preferences.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PreferencesComponent {
  private readonly service = inject(PreferenceService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  readonly state = signal<'loading' | 'error' | 'ready'>('loading');
  readonly busy = signal(false);
  readonly message = signal<string | null>(null);
  readonly items = signal<string[]>([]);
  readonly photo = signal<string | null>(null);
  readonly entry = this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(100)]);
  readonly form = this.fb.nonNullable.group({ showAlerts: true });

  constructor() { this.load(); }

  load(): void {
    this.state.set('loading');
    this.service.get({ skipHandleError: true }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: value => {
        this.items.set(value.watchedItems ?? []);
        this.photo.set(value.photoDataUrl ?? null);
        this.form.patchValue({ showAlerts: value.showAlerts ?? true });
        this.state.set('ready');
      },
      error: () => this.state.set('error'),
    });
  }

  add(): void {
    if (this.busy()) return;
    const clean = this.entry.value.trim().replace(/\s+/g, ' ');
    const normalize = (value: string) => value.normalize('NFD').replace(/\p{M}/gu, '').toLowerCase();
    if (!clean || clean.length > 100 || this.items().length >= 50 ||
        this.items().some(x => normalize(x) === normalize(clean))) {
      this.entry.markAsTouched();
      this.message.set('::Preferences:InvalidEntry');
      return;
    }
    this.items.update(items => [...items, clean]);
    this.entry.reset();
    this.message.set(null);
  }

  remove(index: number): void {
    if (!this.busy()) this.items.update(items => items.filter((_, i) => i !== index));
  }

  async choosePhoto(event: Event): Promise<void> {
    if (this.busy()) return;
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    if (!['image/png', 'image/jpeg'].includes(file.type) || file.size > 80000) {
      this.message.set('::Preferences:PhotoError'); input.value = ''; return;
    }
    this.busy.set(true);
    try {
      const data = await new Promise<string>((resolve, reject) => {
        const reader = new FileReader(); reader.onload = () => resolve(reader.result as string);
        reader.onerror = reject; reader.readAsDataURL(file);
      });
      this.photo.set(data); this.message.set(null);
    } catch { this.message.set('::Preferences:PhotoError'); }
    finally { this.busy.set(false); input.value = ''; }
  }

  save(): void {
    if (this.busy() || this.state() !== 'ready') return;
    // Una entrada todavía sin agregar no debe perderse silenciosamente al guardar.
    if (this.entry.value.trim()) {
      this.message.set('::Preferences:PendingEntry');
      return;
    }
    this.busy.set(true);
    this.message.set(null);
    this.service.update({ showAlerts: this.form.controls.showAlerts.value, watchedItems: this.items(), photoDataUrl: this.photo() },
      { skipHandleError: true }).pipe(
        takeUntilDestroyed(this.destroyRef), finalize(() => this.busy.set(false)),
      ).subscribe({
        next: value => {
          this.items.set(value.watchedItems ?? []);
          this.form.patchValue({ showAlerts: value.showAlerts ?? true });
          this.message.set('::Preferences:Saved');
        },
        error: () => this.message.set('::Preferences:SaveError'),
      });
  }
}
