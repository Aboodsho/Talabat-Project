import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LucideDynamicIcon, LucideArrowLeft, LucideArrowUpRight, LucideCheck } from '@lucide/angular';
import { AuthService } from '../../core/auth.service';
import { PasswordRequirementsComponent } from './password-requirements.component';

@Component({ selector: 'app-password-reset', standalone: true, imports: [CommonModule, FormsModule, RouterLink, LucideDynamicIcon, PasswordRequirementsComponent], template: `
<main class="reset-page"><a class="back" routerLink="/"><svg [lucideIcon]="icons.ArrowLeft" [size]="15"></svg> BACK TO MARKET</a><section class="card"><div class="eyebrow">ACCOUNT SECURITY</div><h1>{{resetMode()?'Choose a new':'Reset your'}} <span>password.</span></h1><p>{{resetMode()?'Set a new password for your market account.':'Enter your account email and we’ll send a secure reset link.'}}</p>
<form *ngIf="!sent()" (ngSubmit)="submit()"><label *ngIf="!resetMode()">EMAIL ADDRESS<input name="email" type="email" [(ngModel)]="email" required maxlength="256" autocomplete="email"></label><ng-container *ngIf="resetMode()"><label>NEW PASSWORD<input name="password" type="password" [(ngModel)]="password" required minlength="8" maxlength="20" [pattern]="passwordPattern" autocomplete="new-password"></label><app-password-requirements [password]="password"></app-password-requirements><label>CONFIRM PASSWORD<input name="confirmPassword" type="password" [(ngModel)]="confirmPassword" required autocomplete="new-password"></label><small class="mismatch" *ngIf="confirmPassword && password!==confirmPassword">Passwords don’t match yet.</small></ng-container><p class="error" *ngIf="error()">{{error()}}</p><button class="submit" type="submit" [disabled]="busy() || (resetMode() && (!isPasswordValid() || password!==confirmPassword))">{{busy()?'PLEASE WAIT…':resetMode()?'UPDATE PASSWORD':'SEND RESET LINK'}}<svg [lucideIcon]="icons.ArrowUpRight" [size]="16"></svg></button></form>
<div class="success" *ngIf="sent()"><svg [lucideIcon]="icons.Check" [size]="19"></svg><span>{{resetMode()?'Your password has been updated. You can sign in now.':'If an account uses that email, a password reset link has been sent.'}}</span></div><a class="sign-in" routerLink="/" [queryParams]="{account:'login'}">BACK TO SIGN IN</a></section></main>`, styles: [`
:host{display:block}.reset-page{max-width:1140px;min-height:66vh;margin:auto;padding:35px 24px 70px;color:#17212b}.back{display:flex;align-items:center;gap:7px;text-decoration:none;color:#616c75;font-size:9px;letter-spacing:.12em}.card{width:min(480px,100%);margin:40px auto;background:white;border:1px solid #e2e6e9;padding:30px}.eyebrow{font-size:8px;letter-spacing:.15em;color:#818991;font-weight:700}.card h1{font-size:clamp(30px,5vw,42px);letter-spacing:-.065em;margin:12px 0 8px}.card h1 span{color:#ed7900}.card>p{font-size:11px;line-height:1.6;color:#727d86}.card form{display:grid;gap:12px;margin-top:22px}.card label{display:grid;gap:6px;font-size:8px;letter-spacing:.12em}.card input{height:42px;border:1px solid #d6dce1;padding:0 11px;font:inherit;font-size:12px;letter-spacing:0;outline-color:#f39828}.submit{height:44px;border:0;background:#ff941c;color:#17212b;display:flex;align-items:center;justify-content:space-between;padding:0 13px;font-size:9px;letter-spacing:.08em;font-weight:750;cursor:pointer}.submit:disabled{background:#e4e5e4;color:#8a8e8e;cursor:not-allowed}.mismatch,.error{font-size:10px;color:#ac432c}.error{margin:0}.success{margin:24px 0;padding:15px;background:#f1f7ee;border:1px solid #d5e5cc;color:#356b39;display:flex;align-items:center;gap:10px;font-size:11px;line-height:1.5}.sign-in{display:block;margin-top:17px;text-align:center;color:#66717a;font-size:8px;letter-spacing:.12em;text-decoration:none}.sign-in:hover{color:#d66a00}@media(max-width:600px){.reset-page{padding:22px 12px 45px}.card{margin:25px auto;padding:23px 18px}}` ] })
export class PasswordResetComponent {
  private readonly auth = inject(AuthService); private readonly route = inject(ActivatedRoute);
  readonly resetMode = signal(false); readonly busy = signal(false); readonly sent = signal(false); readonly error = signal('');
  readonly icons = { ArrowLeft: LucideArrowLeft, ArrowUpRight: LucideArrowUpRight, Check: LucideCheck };
  readonly passwordPattern = '(?=.*[A-Z])(?=.*[a-z])(?=.*[0-9]).{8,20}';
  userId = ''; token = ''; email = ''; password = ''; confirmPassword = '';
  constructor() { this.route.queryParamMap.subscribe(params => { this.userId=params.get('userId')||''; this.token=params.get('token')||''; this.resetMode.set(!!this.userId&&!!this.token); }); }
  isPasswordValid(): boolean { return this.password.length>=8&&this.password.length<=20&&/[A-Z]/.test(this.password)&&/[a-z]/.test(this.password)&&/[0-9]/.test(this.password); }
  submit(): void {
    this.error.set(''); this.busy.set(true);
    const request = this.resetMode() ? this.auth.resetPassword(this.userId,this.token,this.password) : this.auth.requestPasswordReset(this.email);
    request.subscribe({ next:()=>{this.busy.set(false);this.sent.set(true)}, error:err=>{this.busy.set(false);this.error.set(err.error?.error||'We could not complete that request. Check the details or request a new reset link.')} });
  }
}
