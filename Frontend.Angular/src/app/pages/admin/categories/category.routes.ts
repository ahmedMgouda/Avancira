import { Routes } from '@angular/router';

export const CATEGORY_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./category-list/category-list.component').then(
        (m) => m.CategoryListComponent
      ),
    data: { title: 'Categories' },
  },
  {
    path: 'create',
    loadComponent: () =>
      import('./category-form/category-form.component').then(
        (m) => m.CategoryFormComponent
      ),
    data: { title: 'Create Category' },
  },
  {
    path: 'edit/:id',
    loadComponent: () =>
      import('./category-form/category-form.component').then(
        (m) => m.CategoryFormComponent
      ),
    data: { title: 'Edit Category' },
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./category-detail/category-detail.component').then(
        (m) => m.CategoryDetailComponent
      ),
    data: { title: 'Category Details' },
  },
];
