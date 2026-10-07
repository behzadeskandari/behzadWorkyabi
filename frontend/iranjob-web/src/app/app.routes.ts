import { Routes } from '@angular/router';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';
import { HomeComponent } from './pages/home/home.component';
import { authGuard } from './auth/guards/auth.guard';
import { roleGuard } from './auth/guards/role.guard';
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
      },
      {
        path: 'candidate/applications',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Candidate'] },
        loadComponent: () =>
          import('./candidate/applications/application-list.component').then(m => m.ApplicationListComponent)
      },
      {
        path: 'candidate/applications/:id',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Candidate'] },
        loadComponent: () =>
          import('./candidate/applications/application-detail.component').then(m => m.ApplicationDetailComponent)
      },
      {
        path: 'employer/profile',
        canActivate: [authGuard],
        loadComponent: () =>
          import('./employer/pages/profile/profile.component').then(m => m.EmployerProfileComponent)
      },
      {
        path: 'jobs',
        loadComponent: () =>
          import('./jobs/pages/job-search.component').then(m => m.JobSearchComponent)
      },
      {
        path: 'jobs/:id',
        loadComponent: () =>
          import('./jobs/pages/job-details.component').then(m => m.JobDetailsComponent)
      },
      {
        path: 'employer/jobs',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Employer'] },
        loadComponent: () =>
          import('./employer/pages/jobs/job-list.component').then(m => m.EmployerJobListComponent)
      },
      {
        path: 'employer/jobs/new',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Employer'] },
        loadComponent: () =>
          import('./employer/pages/jobs/job-form.component').then(m => m.EmployerJobFormComponent)
      },
      {
        path: 'employer/jobs/:id/edit',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Employer'] },
        loadComponent: () =>
          import('./employer/pages/jobs/job-form.component').then(m => m.EmployerJobFormComponent)
      },
      {
        path: 'employer/applications',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Employer'] },
        loadComponent: () =>
          import('./employer/applications/application-list.component').then(m => m.EmployerApplicationListComponent)
      },
      {
        path: 'employer/applications/:id',
        canActivate: [authGuard, roleGuard],
        data: { roles: ['Employer'] },
        loadComponent: () =>
          import('./employer/applications/application-detail.component').then(m => m.EmployerApplicationDetailComponent)
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
