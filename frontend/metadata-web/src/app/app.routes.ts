import { Routes } from '@angular/router';
import { MainLayoutComponent } from './layout/main-layout.component';

export const routes: Routes = [
  {
    path: '',
    component: MainLayoutComponent,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'connections' },
      {
        path: 'connections',
        loadComponent: () =>
          import('./pages/connections/connections.component').then((m) => m.ConnectionsComponent)
      },
      {
        path: 'metadata',
        loadComponent: () =>
          import('./pages/metadata/metadata.component').then((m) => m.MetadataComponent)
      },
      {
        path: 'data-query',
        loadComponent: () =>
          import('./pages/data-query/data-query.component').then((m) => m.DataQueryComponent)
      }
    ]
  },
  { path: '**', redirectTo: 'connections' }
];
