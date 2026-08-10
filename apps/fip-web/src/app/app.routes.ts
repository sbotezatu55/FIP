import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/index/pages/index-page/index-page.component').then(
        ({ IndexPageComponent }) => IndexPageComponent
      )
  },
  {
    path: 'flights',
    loadComponent: () =>
      import('./features/flights/pages/flight-list-page/flight-list-page.component').then(
        ({ FlightListPageComponent }) => FlightListPageComponent
      )
  },
  {
    path: 'flights/import',
    loadComponent: () =>
      import('./features/flights/pages/flight-import-page/flight-import-page.component').then(
        ({ FlightImportPageComponent }) => FlightImportPageComponent
      )
  },
  {
    path: 'flights/:id',
    loadComponent: () =>
      import('./features/flights/pages/flight-detail-page/flight-detail-page.component').then(
        ({ FlightDetailPageComponent }) => FlightDetailPageComponent
      )
  }
];
