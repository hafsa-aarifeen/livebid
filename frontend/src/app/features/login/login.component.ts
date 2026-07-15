import { Component, inject, input } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthStore } from '../../state/auth.store';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  template: `
    <div class="login-box">
      <h1>Sign in to LiveBid</h1>

      <form [formGroup]="form" (ngSubmit)="submit()">
        <label>
          Username
          <input formControlName="username" autocomplete="username" />
        </label>
        <label>
          Password
          <input type="password" formControlName="password" autocomplete="current-password" />
        </label>

        <button type="submit" [disabled]="form.invalid || auth.loading()">
          {{ auth.loading() ? 'Signing in…' : 'Sign in' }}
        </button>

        @if (auth.error(); as err) {
          <p class="error">{{ err }}</p>
        }
      </form>

      <p class="hint">Seed users: alice / bob · password123</p>
    </div>
  `,
  styles: `
    :host {
      display: block;
      max-width: 380px;
      margin: 4rem auto;
      padding: 0 1.5rem;
      font-family: system-ui, sans-serif;
    }
    .login-box h1 {
      font-size: 1.4rem;
      margin-bottom: 1.5rem;
    }
    form {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }
    label {
      display: flex;
      flex-direction: column;
      gap: 0.35rem;
      font-size: 0.9rem;
      color: #444;
    }
    input {
      padding: 0.6rem 0.75rem;
      font-size: 1rem;
      border: 1px solid #ccc;
      border-radius: 8px;
    }
    button {
      padding: 0.7rem;
      font-size: 1rem;
      font-weight: 600;
      border: none;
      border-radius: 8px;
      background: #1a7f37;
      color: #fff;
      cursor: pointer;
    }
    button:disabled {
      background: #9cc7a8;
      cursor: not-allowed;
    }
    .error {
      color: #c0392b;
      margin: 0;
    }
    .hint {
      margin-top: 1.5rem;
      font-size: 0.8rem;
      color: #999;
    }
  `,
})
export class LoginComponent {
  readonly auth = inject(AuthStore);
  private readonly router = inject(Router);

  // Bound from the ?returnUrl= query param via withComponentInputBinding
  readonly returnUrl = input<string>();

  readonly form = new FormGroup({
    username: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  async submit(): Promise<void> {
    if (this.form.invalid) return;
    const ok = await this.auth.login(this.form.getRawValue());
    if (ok) {
      this.router.navigateByUrl(this.returnUrl() ?? '/auctions');
    }
  }
}
