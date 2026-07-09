export type AuctionStatus = 'Scheduled' | 'Live' | 'Ended' | 'Cancelled';

export interface AuctionSummary {
  id: string;
  title: string;
  description: string;
  imageUrl: string | null;
  currentPrice: number;
  minIncrement: number;
  startsAt: string;
  endsAt: string;
  status: AuctionStatus;
  seller: string;
  bidCount: number;
}

export interface BidSummary {
  id: string;
  amount: number;
  placedAt: string;
  bidder: string;
}

export interface AuctionDetail extends Omit<AuctionSummary, 'bidCount'> {
  startingPrice: number;
  bids: BidSummary[];
}

// Mirrors the server's BidPlacedEvent record
export interface BidPlacedEvent {
  bidId: string;
  auctionId: string;
  amount: number;
  bidder: string;
  placedAt: string;
  newCurrentPrice: number;
  bidCount: number;
}

export interface PlaceBidRequest {
  amount: number;
  bidderId: string;
}
