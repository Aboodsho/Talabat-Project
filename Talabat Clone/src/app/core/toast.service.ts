import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly message = signal('');
  private timer?: ReturnType<typeof setTimeout>;
  show(message: string): void { this.message.set(message); clearTimeout(this.timer); this.timer = setTimeout(() => this.message.set(''), 2400); }
}
