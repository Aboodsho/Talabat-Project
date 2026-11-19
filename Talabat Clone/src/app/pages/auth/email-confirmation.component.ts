import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-email-confirmation',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `<main class="confirmation-shell"><a class="wordmark" routerLink="/">near<span>market</span><b>.</b></a><section class="confirmation-card"><div class="confirmation-icon" [class.failed]="!busy() && !confirmed()">{{ busy() ? '…' : confirmed() ? '✓' : '!' }}</div><div class="eyebrow">ACCOUNT SETUP</div><h1>{{ busy() ? 'Confirming your email…' : confirmed() ? 'You’re all set.' : 'We couldn’t confirm that.' }}</h1><p>{{ busy() ? 'This will only take a moment.' : confirmed() ? 'Your email is confirmed. Sign in to build your bag and place an order.' : message() }}</p><a class="continue-link" routerLink="/">BACK TO THE MARKET <span>↗</span></a></section></main>`,
  styles: [`:host{display:block;min-height:100vh;background:#f7f6f2;color:#252620}.confirmation-shell{min-height:100vh;max-width:1200px;margin:auto;padding:30px 6%;display:flex;flex-direction:column}.wordmark{font-size:23px;font-weight:900;letter-spacing:-1.3px;color:#21221e;text-decoration:none}.wordmark span{font-weight:400}.wordmark b{color:#ff6b00}.confirmation-card{width:min(470px,100%);margin:auto;background:#fbfaf7;border:1px solid #dfded7;padding:45px}.confirmation-icon{width:48px;height:48px;background:#fff0e6;color:#ff6b00;display:grid;place-items:center;font-size:24px;margin-bottom:25px}.confirmation-icon.failed{background:#fff1eb;color:#b54324}.eyebrow{font-size:9px;letter-spacing:.15em;color:#74756e}.confirmation-card h1{font-size:clamp(34px,6vw,48px);letter-spacing:-.07em;line-height:1;margin:12px 0}.confirmation-card p{font-size:13px;line-height:1.7;color:#72736c}.continue-link{display:flex;justify-content:space-between;align-items:center;margin-top:28px;padding:14px;background:#ff6b00;color:#fff;text-decoration:none;font-size:9px;letter-spacing:.1em;font-weight:650}.continue-link span{font-size:16px}`]
})
export class EmailConfirmationComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(AuthService);
  readonly busy = signal(true);
  readonly confirmed = signal(false);
  readonly message = signal('');

  constructor() {
    const userId = this.route.snapshot.queryParamMap.get('userId');
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!userId || !token) {
      this.busy.set(false);
      this.message.set('The confirmation link is incomplete or expired. Request a fresh confirmation email and try again.');
      return;
    }
    this.auth.confirmEmail(userId, token).subscribe({
      next: () => { this.confirmed.set(true); this.busy.set(false); },
      error: () => { this.message.set('The confirmation link may have expired. Request a fresh confirmation email and try again.'); this.busy.set(false); }
    });
  }
}
