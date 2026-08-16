import { Routes } from '@angular/router';

export const routes: Routes = [
    {
        path: 'student-dashboard',
        loadComponent: () =>
            import('./features/student-dashboard/student-dashboard')
                .then(m => m.StudentDashboard)
    },
    {
        path: '',
        redirectTo: 'student-dashboard',
        pathMatch: 'full'
    },
    {
        path: 'courses/:id',
        loadComponent: () => import('./features/course-detail/course-detail')
            .then(m => m.CourseDetail)
    },
    {
        path: 'enroll',
        loadComponent: () =>
            import('./features/enrollment-form/enrollment-form')
                .then(m => m.EnrollmentForm)
    },
    {
        path: 'dashboard',
        loadComponent: () =>
        import('./features/instructor-dashboard/instructor-dashboard')
            .then(m => m.InstructorDashboard)
    },
    {
        path: 'enrollments',
        loadComponent: () =>
        import('./features/enrollment-list/enrollment-list')
            .then(m => m.EnrollmentList)
    },
    { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
];