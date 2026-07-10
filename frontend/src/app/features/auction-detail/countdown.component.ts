import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { interval } from 'rxjs';

@Component({
  selector: 'app-countdown',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="countdown" [class.urgent]="urgent()">
      {{ display() }}
    </span>
  `,
  styles: `
    .countdown {
      font-variant-numeric: tabular-nums;
      font-weight: 600;
    }
    .countdown.urgent {
      color: #c0392b;
      animation: pulse 1s infinite;
    }
    @keyframes pulse {
      50% {
        opacity: 0.5;
      }
    }
  `,
})
export class CountdownComponent {
  // The target time, as an ISO string from the API
  readonly endsAt = input.required<string>();

  // A signal that ticks every second
  private readonly now = signal(Date.now());

  readonly remainingMs = computed(() =>
    Math.max(0, new Date(this.endsAt()).getTime() - this.now()),
  );

  readonly urgent = computed(() => this.remainingMs() > 0 && this.remainingMs() < 60_000);

  readonly display = computed(() => {
    const ms = this.remainingMs();
    if (ms <= 0) return 'Ended';

    const totalSeconds = Math.floor(ms / 1000);
    const days = Math.floor(totalSeconds / 86_400);
    const hours = Math.floor((totalSeconds % 86_400) / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;

    if (days > 0) return `${days}d ${hours}h ${minutes}m`;
    if (hours > 0) return `${hours}h ${minutes}m ${seconds}s`;
    return `${minutes}m ${seconds}s`;
  });

  constructor() {
    interval(1000)
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.now.set(Date.now()));
  }
}
