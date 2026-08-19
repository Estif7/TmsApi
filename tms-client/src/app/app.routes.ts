import { Routes } from '@angular/router';
import { Login } from './components/login/login';
import { authGuard } from './guards/auth.guard';
import { roleGuard } from './guards/role.guard';
import { Unauthorized } from './components/unauthorized/unauthorized';

export const routes: Routes = [
  {
    path: 'login',
    component: Login
  },
  {
    path: 'student-dashboard',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/student-dashboard/student-dashboard')
        .then(m => m.StudentDashboard)
  },
  {
    path: 'courses/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/course-detail/course-detail')
        .then(m => m.CourseDetail)
  },
  {
    path: 'enroll',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/enrollment-form/enrollment-form')
        .then(m => m.EnrollmentForm)
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/instructor-dashboard/instructor-dashboard')
        .then(m => m.InstructorDashboard)
  },
  {
    path: 'enrollments',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/enrollment-list/enrollment-list')
        .then(m => m.EnrollmentList)
  },
  {
    path: 'admin/courses',
    canActivate: [roleGuard('Admin')],
    loadComponent: () =>
      import('./features/instructor-dashboard/instructor-dashboard')
        .then(m => m.InstructorDashboard)
  },
  {
    path: 'unauthorized',
    component: Unauthorized
  },
  {
    path: '',
    redirectTo: 'student-dashboard',
    pathMatch: 'full'
  }
];