import { inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { computed } from '@angular/core';
import {
  patchState,
  signalStore,
  withComputed,
  withHooks,
  withMethods,
  withState,
} from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';
import { AuthResponse, LoginRequest } from '../core/models/auth.models';

const STORAGE_KEY = 'livebid.auth';

export interface AuthState {
  token: string | null;
  userId: string | null;
  username: string | null;
  loading: boolean;
  error: string | null;
}

const initialState: AuthState = {
  token: null,
  userId: null,
  username: null,
  loading: false,
  error: null,
};

export const AuthStore = signalStore(
  { providedIn: 'root' },

  withState(initialState),

  withComputed(({ token }) => ({
    isAuthenticated: computed(() => token() !== null),
  })),

  withMethods((store, http = inject(HttpClient)) => ({
    async login(request: LoginRequest): Promise<boolean> {
      patchState(store, { loading: true, error: null });
      try {
        const res = await firstValueFrom(
          http.post<AuthResponse>('http://localhost:5150/api/auth/login', request),
        );
        patchState(store, {
          token: res.token,
          userId: res.userId,
          username: res.username,
          loading: false,
        });
        localStorage.setItem(STORAGE_KEY, JSON.stringify(res));
        return true;
      } catch (err) {
        const message =
          err instanceof HttpErrorResponse && err.error?.error
            ? err.error.error
            : 'Login failed. Please try again.';
        patchState(store, { loading: false, error: message });
        return false;
      }
    },

    logout(): void {
      localStorage.removeItem(STORAGE_KEY);
      patchState(store, initialState);
    },
  })),

  withHooks({
    onInit(store) {
      // Restore session on app startup
      const saved = localStorage.getItem(STORAGE_KEY);
      if (!saved) return;
      try {
        const res: AuthResponse = JSON.parse(saved);
        patchState(store, {
          token: res.token,
          userId: res.userId,
          username: res.username,
        });
      } catch {
        localStorage.removeItem(STORAGE_KEY);
      }
    },
  }),
);
