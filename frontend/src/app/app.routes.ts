import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'auctions',
    loadComponent: () =>
      import('./features/auction-list/auction-list.component').then((m) => m.AuctionListComponent),
  },
  {
    path: 'auctions/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/auction-detail/auction-detail.component').then(
        (m) => m.AuctionDetailComponent,
      ),
  },
  { path: '', pathMatch: 'full', redirectTo: 'auctions' },
];
