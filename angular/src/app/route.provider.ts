import { RoutesService, eLayoutType } from '@abp/ng.core';
import { inject, provideAppInitializer } from '@angular/core';
export const APP_ROUTE_PROVIDER = [
  provideAppInitializer(() => {
    configureRoutes();
  }),
];
function configureRoutes() {
  const routes = inject(RoutesService);
  routes.add([
      {
        path: '/catalog',
        name: '::Catalog:Title',
        iconClass: 'fas fa-search',
        order: 4,
        layout: eLayoutType.application,
      },
      {
        path: '/preferences',
        name: '::Preferences:Title',
        iconClass: 'fas fa-sliders',
        order: 3,
        layout: eLayoutType.application,
      },
      {
        path: '/pantry',
        name: '::Menu:Pantry',
        iconClass: 'fas fa-box-open',
        order: 2,
        layout: eLayoutType.application,
      },
      {
        path: '/',
        name: '::Menu:Home',
        iconClass: 'fas fa-home',
        order: 1,
        layout: eLayoutType.application,
      },
  ]);
}
