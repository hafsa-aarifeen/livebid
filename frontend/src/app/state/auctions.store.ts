import { computed, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { firstValueFrom, pipe, switchMap, tap } from 'rxjs';
import { tapResponse } from '@ngrx/operators';
import { AuctionApiService } from '../core/services/auction-api.service';
import { AuctionDetail, AuctionSummary, BidPlacedEvent } from '../core/models/auction.models';

export interface AuctionsState {
  auctions: AuctionSummary[];
  selectedAuction: AuctionDetail | null;
  loading: boolean;
  error: string | null;
  placingBid: boolean;
  bidError: string | null;
}

const initialState: AuctionsState = {
  auctions: [],
  selectedAuction: null,
  loading: false,
  error: null,
  placingBid: false,
  bidError: null,
};

export const AuctionsStore = signalStore(
  { providedIn: 'root' },

  withState(initialState),

  withComputed(({ auctions }) => ({
    liveAuctions: computed(() => auctions().filter((a) => a.status === 'Live')),
    scheduledAuctions: computed(() => auctions().filter((a) => a.status === 'Scheduled')),
    endedAuctions: computed(() => auctions().filter((a) => a.status === 'Ended')),
  })),

  withMethods((store, api = inject(AuctionApiService)) => {
    // Shared by the SignalR pipeline AND the HTTP success path.
    // Idempotent by bidId — safe to call twice with the same event.
    function applyBidPlaced(evt: BidPlacedEvent): void {
      patchState(store, {
        auctions: store
          .auctions()
          .map((a) =>
            a.id === evt.auctionId
              ? { ...a, currentPrice: evt.newCurrentPrice, bidCount: evt.bidCount }
              : a,
          ),
      });

      const selected = store.selectedAuction();
      if (!selected || selected.id !== evt.auctionId) return;

      const alreadyHave = selected.bids.some((b) => b.id === evt.bidId);

      patchState(store, {
        selectedAuction: {
          ...selected,
          currentPrice: evt.newCurrentPrice,
          bids: alreadyHave
            ? selected.bids
            : [
                {
                  id: evt.bidId,
                  amount: evt.amount,
                  placedAt: evt.placedAt,
                  bidder: evt.bidder,
                },
                ...selected.bids,
              ],
        },
      });
    }

    return {
      applyBidPlaced,

      loadAuctions: rxMethod<void>(
        pipe(
          tap(() => patchState(store, { loading: true, error: null })),
          switchMap(() =>
            api.getAuctions().pipe(
              tapResponse({
                next: (auctions) => patchState(store, { auctions, loading: false }),
                error: (err: Error) => patchState(store, { error: err.message, loading: false }),
              }),
            ),
          ),
        ),
      ),

      loadAuction: rxMethod<string>(
        pipe(
          tap(() => patchState(store, { loading: true, error: null })),
          switchMap((id) =>
            api.getAuction(id).pipe(
              tapResponse({
                next: (auction) => patchState(store, { selectedAuction: auction, loading: false }),
                error: (err: Error) => patchState(store, { error: err.message, loading: false }),
              }),
            ),
          ),
        ),
      ),

      // --- Optimistic bid placement ---
      async placeBid(amount: number, bidderId: string): Promise<void> {
        const selected = store.selectedAuction();
        if (!selected || store.placingBid()) return;

        // Snapshots for rollback
        const auctionsSnapshot = store.auctions();
        const selectedSnapshot = selected;

        // 1. Optimistic patch — bid shows immediately as pending
        const tempId = `pending-${Date.now()}`;
        patchState(store, {
          placingBid: true,
          bidError: null,
          selectedAuction: {
            ...selected,
            currentPrice: amount,
            bids: [
              {
                id: tempId,
                amount,
                placedAt: new Date().toISOString(),
                bidder: 'you',
              },
              ...selected.bids,
            ],
          },
        });

        try {
          // 2. The real request
          const evt = await firstValueFrom(api.placeBid(selected.id, { amount, bidderId }));

          // 3a. Confirmed — remove the pending row, apply the real event.
          //     (SignalR may have already delivered it; applyBidPlaced dedupes.)
          const current = store.selectedAuction();
          if (current) {
            patchState(store, {
              selectedAuction: {
                ...current,
                bids: current.bids.filter((b) => b.id !== tempId),
              },
            });
          }
          applyBidPlaced(evt);
          patchState(store, { placingBid: false });
        } catch (err) {
          // 3b. Rejected — roll back and surface the server's reason
          const message =
            err instanceof HttpErrorResponse && err.error?.error
              ? err.error.error
              : 'Bid failed. Please try again.';

          patchState(store, {
            auctions: auctionsSnapshot,
            selectedAuction: selectedSnapshot,
            placingBid: false,
            bidError: message,
          });
        }
      },

      clearBidError(): void {
        patchState(store, { bidError: null });
      },

      clearSelected(): void {
        patchState(store, { selectedAuction: null, bidError: null });
      },
    };
  }),
);
