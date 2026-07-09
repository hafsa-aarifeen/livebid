import { Component, OnInit, effect, inject } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuctionsStore } from '../../state/auctions.store';
import { AuctionRealtimeService } from '../../core/services/auction-realtime.service';

@Component({
  selector: 'app-auction-list',
  imports: [CurrencyPipe, DatePipe, RouterLink],
  template: `
    @if (realtime.status() !== 'connected') {
      <div class="banner" [class.reconnecting]="realtime.status() === 'reconnecting'">
        {{
          realtime.status() === 'reconnecting'
            ? '⚠ Reconnecting — prices may be stale…'
            : 'Connecting to live updates…'
        }}
      </div>
    }

    <h1>LiveBid</h1>

    @if (store.loading()) {
      <p class="muted">Loading auctions…</p>
    }
    @if (store.error(); as err) {
      <p class="error">{{ err }}</p>
    }

    <section>
      <h2>🔴 Live now</h2>
      @for (auction of store.liveAuctions(); track auction.id) {
        <a class="card" [routerLink]="['/auctions', auction.id]">
          <div class="card-main">
            <h3>{{ auction.title }}</h3>
            <p class="muted">{{ auction.description }}</p>
            <p class="meta">
              Ends {{ auction.endsAt | date: 'short' }} · {{ auction.bidCount }} bid{{
                auction.bidCount === 1 ? '' : 's'
              }}
              · seller: {{ auction.seller }}
            </p>
          </div>
          <div class="price">
            {{ auction.currentPrice | currency: 'USD' }}
          </div>
        </a>
      } @empty {
        <p class="muted">No live auctions right now.</p>
      }
    </section>

    <section>
      <h2>🗓 Upcoming</h2>
      @for (auction of store.scheduledAuctions(); track auction.id) {
        <div class="card">
          <div class="card-main">
            <h3>{{ auction.title }}</h3>
            <p class="meta">Starts {{ auction.startsAt | date: 'short' }}</p>
          </div>
          <div class="price muted">
            {{ auction.currentPrice | currency: 'USD' }}
          </div>
        </div>
      } @empty {
        <p class="muted">Nothing scheduled.</p>
      }
    </section>
  `,
  styles: `
    :host {
      display: block;
      max-width: 720px;
      margin: 0 auto;
      padding: 1.5rem;
      font-family: system-ui, sans-serif;
    }
    h1 {
      margin-bottom: 1.5rem;
    }
    h2 {
      font-size: 1rem;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: #666;
      margin: 2rem 0 0.75rem;
    }
    .card {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 1rem;
      padding: 1rem;
      margin-bottom: 0.75rem;
      border: 1px solid #ddd;
      border-radius: 8px;
      text-decoration: none;
      color: inherit;
      transition: border-color 0.15s;
    }
    a.card:hover {
      border-color: #888;
    }
    .card h3 {
      margin: 0 0 0.25rem;
    }
    .card p {
      margin: 0.15rem 0;
    }
    .price {
      font-size: 1.4rem;
      font-weight: 700;
      white-space: nowrap;
    }
    .meta {
      font-size: 0.8rem;
      color: #888;
    }
    .muted {
      color: #999;
    }
    .error {
      color: #c0392b;
    }
    .banner {
      padding: 0.5rem 1rem;
      background: #eef;
      border-radius: 6px;
      margin-bottom: 1rem;
    }
    .banner.reconnecting {
      background: #fdecea;
      color: #c0392b;
    }
  `,
})
export class AuctionListComponent implements OnInit {
  readonly store = inject(AuctionsStore);
  readonly realtime = inject(AuctionRealtimeService);

  private readonly joinedGroups = new Set<string>();

  constructor() {
    // Whenever the auction list changes, join the SignalR group
    // for any auction we haven't subscribed to yet.
    effect(() => {
      const auctions = this.store.auctions();
      if (this.realtime.status() !== 'connected') return;

      for (const auction of auctions) {
        if (!this.joinedGroups.has(auction.id)) {
          this.joinedGroups.add(auction.id);
          this.realtime.joinAuction(auction.id);
        }
      }
    });
  }

  ngOnInit(): void {
    this.store.loadAuctions();
    this.realtime.connect();
  }
}
