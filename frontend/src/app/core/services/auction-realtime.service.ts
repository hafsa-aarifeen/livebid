import { Injectable, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { AuctionsStore } from '../../state/auctions.store';
import { AuctionEndedEvent, AuctionStartedEvent, BidPlacedEvent } from '../models/auction.models';

export type ConnectionStatus = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

@Injectable({ providedIn: 'root' })
export class AuctionRealtimeService {
  private readonly store = inject(AuctionsStore);
  private connection: HubConnection | null = null;

  // Exposed as a readonly signal so components can show connection state
  private readonly _status = signal<ConnectionStatus>('disconnected');
  readonly status = this._status.asReadonly();

  async connect(): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) return;

    this._status.set('connecting');

    this.connection = new HubConnectionBuilder()
      .withUrl('http://localhost:5150/hubs/auction')
      .withAutomaticReconnect()
      .build();

    // Route incoming events into the store — the ONLY place SignalR touches state
    this.connection.on('BidPlaced', (evt: BidPlacedEvent) => {
      this.store.applyBidPlaced(evt);
    });

    this.connection.on('AuctionStarted', (evt: AuctionStartedEvent) => {
      this.store.applyAuctionStarted(evt);
    });

    this.connection.on('AuctionEnded', (evt: AuctionEndedEvent) => {
      this.store.applyAuctionEnded(evt);
    });

    this.connection.onreconnecting(() => this._status.set('reconnecting'));
    this.connection.onreconnected(() => this._status.set('connected'));
    this.connection.onclose(() => this._status.set('disconnected'));

    try {
      await this.connection.start();
      this._status.set('connected');
    } catch {
      this._status.set('disconnected');
    }
  }

  async joinAuction(auctionId: string): Promise<void> {
    if (this.connection?.state !== HubConnectionState.Connected) {
      await this.connect();
    }
    await this.connection!.invoke('JoinAuction', auctionId);
  }

  async leaveAuction(auctionId: string): Promise<void> {
    if (this.connection?.state !== HubConnectionState.Connected) return;
    await this.connection.invoke('LeaveAuction', auctionId);
  }

  async disconnect(): Promise<void> {
    await this.connection?.stop();
    this._status.set('disconnected');
  }
}
