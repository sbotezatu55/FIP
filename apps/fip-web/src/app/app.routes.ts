import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
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
  },
  {
    path: 'technology',
    loadComponent: () =>
      import('./features/technology/pages/technology-page/technology-page.component').then(
        ({ TechnologyPageComponent }) => TechnologyPageComponent
      )
  },
  {
    path: 'features',
    loadComponent: () =>
      import('./features/features-page/features-page.component').then(
        ({ FeaturesPageComponent }) => FeaturesPageComponent
      )
  },
  {
    path: 'privacy',
    loadComponent: () =>
      import('./features/legal/pages/privacy-page/privacy-page.component').then(
        ({ PrivacyPageComponent }) => PrivacyPageComponent
      )
  },
  {
    path: 'terms',
    loadComponent: () =>
      import('./features/legal/pages/terms-page/terms-page.component').then(
        ({ TermsPageComponent }) => TermsPageComponent
      )
  },
  {
    path: 'contact',
    loadComponent: () =>
      import('./features/legal/pages/contact-page/contact-page.component').then(
        ({ ContactPageComponent }) => ContactPageComponent
      )
  }
];
