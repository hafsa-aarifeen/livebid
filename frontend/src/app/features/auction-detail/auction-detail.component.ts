import { Component, OnDestroy, OnInit, computed, inject, input } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuctionsStore } from '../../state/auctions.store';
import { AuctionRealtimeService } from '../../core/services/auction-realtime.service';
import { CountdownComponent } from './countdown.component';

@Component({
  selector: 'app-auction-detail',
  imports: [CurrencyPipe, DatePipe, RouterLink, ReactiveFormsModule, CountdownComponent],
  template: `
    <a routerLink="/auctions" class="back">← All auctions</a>

    @if (store.selectedAuction(); as auction) {
      <header>
        <h1>{{ auction.title }}</h1>
        <p class="muted">{{ auction.description }}</p>
        <p class="meta">
          Seller: {{ auction.seller }} · Started at {{ auction.startingPrice | currency: 'USD' }} ·
          Ends {{ auction.endsAt | date: 'medium' }}
        </p>
      </header>

      <div class="price-panel">
        <span class="label">Current price</span>
        <span class="price">{{ auction.currentPrice | currency: 'USD' }}</span>
        @if (auction.status === 'Live') {
          <span class="min">
            <app-countdown [endsAt]="auction.endsAt" /> · next bid ≥
            {{ minimumBid() | currency: 'USD' }}
          </span>
        } @else {
          <span class="min">{{ auction.status }}</span>
        }
      </div>

      @if (auction.status === 'Live') {
        <div class="bid-form">
          <input
            type="number"
            [formControl]="bidAmount"
            [placeholder]="'e.g. ' + minimumBid()"
            step="0.01"
          />
          <button (click)="submitBid()" [disabled]="bidAmount.invalid || store.placingBid()">
            {{ store.placingBid() ? 'Placing…' : 'Place bid' }}
          </button>
        </div>
        @if (store.bidError(); as bidErr) {
          <p class="error">{{ bidErr }}</p>
        }
      }

      <section>
        <h2>Bid history ({{ auction.bids.length }})</h2>
        @for (bid of auction.bids; track bid.id) {
          <div class="bid-row" [class.pending]="bid.id.startsWith('pending-')">
            <span class="bidder">{{ bid.bidder }}</span>
            <span class="amount">{{ bid.amount | currency: 'USD' }}</span>
            <span class="time muted">
              {{ bid.id.startsWith('pending-') ? 'sending…' : (bid.placedAt | date: 'mediumTime') }}
            </span>
          </div>
        } @empty {
          <p class="muted">No bids yet — be the first!</p>
        }
      </section>
    } @else if (store.loading()) {
      <p class="muted">Loading auction…</p>
    } @else if (store.error(); as err) {
      <p class="error">{{ err }}</p>
    }
  `,
  styles: `
    :host {
      display: block;
      max-width: 720px;
      margin: 0 auto;
      padding: 1.5rem;
      font-family: system-ui, sans-serif;
    }
    .back {
      display: inline-block;
      margin-bottom: 1rem;
      color: #666;
      text-decoration: none;
    }
    .back:hover {
      color: #000;
    }
    header h1 {
      margin: 0 0 0.25rem;
    }
    .meta {
      font-size: 0.85rem;
      color: #888;
    }
    .price-panel {
      display: flex;
      align-items: baseline;
      gap: 1rem;
      padding: 1.25rem;
      background: #f7f7f7;
      border-radius: 10px;
      margin: 1.5rem 0;
    }
    .price-panel .label {
      text-transform: uppercase;
      font-size: 0.75rem;
      letter-spacing: 0.05em;
      color: #666;
    }
    .price-panel .price {
      font-size: 2.2rem;
      font-weight: 800;
    }
    .price-panel .min {
      font-size: 0.85rem;
      color: #888;
      margin-left: auto;
    }
    .bid-form {
      display: flex;
      gap: 0.75rem;
      margin: 1rem 0;
    }
    .bid-form input {
      flex: 1;
      padding: 0.65rem 0.8rem;
      font-size: 1rem;
      border: 1px solid #ccc;
      border-radius: 8px;
    }
    .bid-form button {
      padding: 0.65rem 1.4rem;
      font-size: 1rem;
      font-weight: 600;
      border: none;
      border-radius: 8px;
      background: #1a7f37;
      color: #fff;
      cursor: pointer;
    }
    .bid-form button:disabled {
      background: #9cc7a8;
      cursor: not-allowed;
    }
    h2 {
      font-size: 1rem;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: #666;
      margin: 2rem 0 0.75rem;
    }
    .bid-row {
      display: flex;
      gap: 1rem;
      padding: 0.6rem 0.25rem;
      border-bottom: 1px solid #eee;
    }
    .bid-row.pending {
      opacity: 0.55;
      font-style: italic;
    }
    .bid-row .bidder {
      font-weight: 600;
      min-width: 8rem;
    }
    .bid-row .amount {
      font-variant-numeric: tabular-nums;
    }
    .bid-row .time {
      margin-left: auto;
      font-size: 0.85rem;
    }
    .muted {
      color: #999;
    }
    .error {
      color: #c0392b;
    }
  `,
})
export class AuctionDetailComponent implements OnInit, OnDestroy {
  readonly store = inject(AuctionsStore);
  private readonly realtime = inject(AuctionRealtimeService);

  readonly id = input.required<string>();

  readonly minimumBid = computed(() => {
    const a = this.store.selectedAuction();
    return a ? a.currentPrice + a.minIncrement : 0;
  });

  readonly bidAmount = new FormControl<number | null>(null, {
    validators: [Validators.required, Validators.min(0.01)],
  });

  ngOnInit(): void {
    this.store.loadAuction(this.id());
    this.realtime.joinAuction(this.id());
  }

  ngOnDestroy(): void {
    this.realtime.leaveAuction(this.id());
    this.store.clearSelected();
  }

  submitBid(): void {
    const amount = this.bidAmount.value;
    if (amount == null) return;
    this.store.placeBid(amount);
    this.bidAmount.reset();
  }
}
