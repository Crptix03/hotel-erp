import { Routes } from '@angular/router';
import { Login } from './pages/login/login';
import { Shell } from './layout/shell';
import { Dashboard } from './pages/dashboard/dashboard';
import { authGuard } from './core/auth.guard';

export const routes: Routes = [
  { path: 'login', component: Login },
  {
    path: '',
    component: Shell,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: Dashboard },
    ],
  },
  { path: '**', redirectTo: '' },
];