import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({ selector: 'app-password-requirements', standalone: true, imports: [CommonModule], template: `
<div class="requirements" aria-live="polite"><span [class.met]="password.length >= 8 && password.length <= 20">8–20 characters</span><span [class.met]="/[A-Z]/.test(password)">One uppercase letter</span><span [class.met]="/[a-z]/.test(password)">One lowercase letter</span><span [class.met]="/[0-9]/.test(password)">One number</span><span class="optional">Special character is optional</span></div>`, styles: [`
.requirements{display:grid;grid-template-columns:1fr 1fr;gap:5px 12px;margin-top:7px;color:#858981;font-size:9px;line-height:1.4}.requirements span:before{content:'○';margin-right:5px}.requirements span.met{color:#387548}.requirements span.met:before{content:'✓'}.requirements .optional{grid-column:1/-1;color:#858981}.requirements .optional:before{content:'i'}`] })
export class PasswordRequirementsComponent { @Input() password = ''; }
