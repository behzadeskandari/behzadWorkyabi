import { Routes } from '@angular/router';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';
import { HomeComponent } from './pages/home/home.component';
import { authGuard } from './auth/guards/auth.guard';
import { AccountComponent } from './auth/pages/account/account.component';
import { LoginComponent } from './auth/pages/login/login.component';
import { RegisterComponent } from './auth/pages/register/register.component';

export const routes: Routes = [
  {
    path: '',
    component: MainLayoutComponent,
    children: [
      {
        path: '',
        component: HomeComponent
      },
      {
        path: 'account',
        component: AccountComponent,
        canActivate: [authGuard]
      },
      {
        path: 'candidate/profile',
        canActivate: [authGuard],
        loadComponent: () =>
          import('./candidate/pages/profile/profile.component').then(m => m.ProfileComponent)
      }
    ]
  },
  {
    path: 'login',
    component: LoginComponent
  },
  {
    path: 'register',
    component: RegisterComponent
  },
  {
    path: '**',
    redirectTo: ''
  }
];
