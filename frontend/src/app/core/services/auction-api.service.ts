import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AuctionDetail,
  AuctionSummary,
  BidPlacedEvent,
  PlaceBidRequest,
} from '../models/auction.models';

@Injectable({ providedIn: 'root' })
export class AuctionApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5150/api';

  getAuctions(status?: string): Observable<AuctionSummary[]> {
    const params = status ? { params: { status } } : {};
    return this.http.get<AuctionSummary[]>(`${this.baseUrl}/auctions`, params);
  }

  getAuction(id: string): Observable<AuctionDetail> {
    return this.http.get<AuctionDetail>(`${this.baseUrl}/auctions/${id}`);
  }

  placeBid(auctionId: string, request: PlaceBidRequest): Observable<BidPlacedEvent> {
    return this.http.post<BidPlacedEvent>(`${this.baseUrl}/auctions/${auctionId}/bids`, request);
  }
}
