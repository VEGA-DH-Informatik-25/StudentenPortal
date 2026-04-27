import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth-guard';
import { adminGuard } from './core/guards/admin-guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login-page/login-page').then(m => m.LoginPage),
  },
  {
    path: '',
    loadComponent: () => import('./layout/shell/shell').then(m => m.Shell),
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'feed', pathMatch: 'full' },
      {
        path: 'feed',
        loadComponent: () => import('./features/feed/feed-page/feed-page').then(m => m.FeedPage),
      },
      {
        path: 'mensa',
        loadComponent: () => import('./features/mensa/mensa-page/mensa-page').then(m => m.MensaPage),
      },
      {
        path: 'calendar',
        loadComponent: () => import('./features/calendar/calendar-page/calendar-page').then(m => m.CalendarPage),
      },
      {
        path: 'timetable',
        loadComponent: () => import('./features/timetable/timetable-page/timetable-page').then(m => m.TimetablePage),
      },
      {
        path: 'grades',
        loadComponent: () => import('./features/grades/grades-page/grades-page').then(m => m.GradesPage),
      },
      {
        path: 'groups',
        loadComponent: () => import('./features/groups/groups-page/groups-page').then(m => m.GroupsPage),
      },
      {
        path: 'profile',
        loadComponent: () => import('./features/profile/profile-page/profile-page').then(m => m.ProfilePage),
      },
      {
        path: 'admin',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/admin/admin-page/admin-page').then(m => m.AdminPage),
      },
    ],
  },
];

