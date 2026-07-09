import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'auctions',
    loadComponent: () =>
      import('./features/auction-list/auction-list.component').then((m) => m.AuctionListComponent),
  },
  {
    path: 'auctions/:id',
    loadComponent: () =>
      import('./features/auction-detail/auction-detail.component').then(
        (m) => m.AuctionDetailComponent,
      ),
  },
  { path: '', pathMatch: 'full', redirectTo: 'auctions' },
];
