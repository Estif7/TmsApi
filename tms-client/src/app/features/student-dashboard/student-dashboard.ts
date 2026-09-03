import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { rxResource } from '@angular/core/rxjs-interop';

import { CourseCard } from '../../ui/course-card/course-card';
import { Course, PagedResponse } from '../../models/course.model';
import { CourseService } from '../../services/course';
import { EnrollmentService } from '../../services/enrollment';
import { EnrollmentStore } from '../../store/enrollment.store';
import { AuthService } from '../../services/auth';
import { effect } from '@angular/core';

@Component({
  selector: 'app-student-dashboard',
  standalone: true,
  imports: [CommonModule, CourseCard],
  templateUrl: './student-dashboard.html',
  styleUrl: './student-dashboard.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentDashboard {
  private readonly courseService = inject(CourseService);
  private readonly enrollmentService = inject(EnrollmentService);
  private readonly store = inject(EnrollmentStore);
  readonly authService = inject(AuthService);

  isEnrolling = signal<boolean>(false);
  statusMessage = signal<{ text: string; type: 'success' | 'error' } | null>(null);

  studentName = computed(() => this.authService.currentUser()?.displayName ?? 'Student');

coursesResource = rxResource({
  stream: () => this.courseService.getAll(),
  defaultValue: [] as Course[],
});

// 2. Extract course list safely with explicit typing
coursesList = computed<Course[]>(() => {
  const res = this.coursesResource.value() as PagedResponse<Course> | Course[];
  if (Array.isArray(res)) {
    return res;
  }
  return res?.items ?? [];
});

  // Access store entities
  // 1. Access store entities
  enrolledCourses = computed(() => this.store.entities());

  // 2. Active Enrollments: Filter by valid status values ('Pending' or 'Approved')
  // Active enrollments count ('Pending' or 'Approved')
  activeEnrollmentsCount = computed(() => {
    return this.enrolledCourses().filter(
      (e) => e.status === 'Pending' || e.status === 'Approved'
    ).length;
  });

  // Earned credits dynamically matched from coursesResource
  earnedCredits = computed(() => {
  const courses = this.coursesResource.value() ?? [];
  const enrollments = this.enrolledCourses();

  return enrollments.reduce((total, enrollment) => {
    // 1. Loose matching between string/number course IDs
    const matchingCourse = courses.find(
      (c) => String(c.id) === String(enrollment.courseId)
    );

    // 2. Normalize status check (handles strings like "Approved", "Pending", or enum ints)
    const statusStr = String(enrollment.status).toUpperCase();
    const isValidStatus =
      statusStr === 'APPROVED' ||
      statusStr === 'PENDING' ||
      statusStr === '1' ||
      statusStr === '0';

    if (isValidStatus) {
      // 3. Fallback to 3 credits if matchingCourse exists but credits is 0 or undefined
      const credits = matchingCourse?.credits || 3;
      return total + credits;
    }

    return total;
  }, 0);
});
  graduationStatus = computed(() =>
    this.earnedCredits() >= 120 ? 'Eligible for Graduation' : 'In Progress'
  );

  constructor() {
    this.store.loadEnrollments();

    effect(() => {
    console.log('--- DASHBOARD DEBUG ---');
    console.log('Raw Store Entities:', this.store.entities());
    console.log('Raw Courses Resource:', this.coursesResource.value());
    console.log('Extracted Courses List:', this.coursesList());
  });
  }

  async enroll(course: Course): Promise<void> {
    if (this.isEnrolling()) return;

    const user = this.authService.currentUser();
    const rawId = user?.studentId ?? user?.id;
    const currentUserId = typeof rawId === 'number' ? rawId : parseInt(String(rawId), 10);
    const finalStudentId = !isNaN(currentUserId) && currentUserId > 0 ? currentUserId : 1;

    this.isEnrolling.set(true);
    this.statusMessage.set(null);

    this.enrollmentService
      .createEnrollment(course.id, { studentId: finalStudentId })
      .subscribe({
        next: () => {
          // Re-load store data from backend API
          this.store.loadEnrollments();
          this.coursesResource.reload();

          // REMOVED: this.earnedCredits.update((c) => c + 3); 
          // Store reload will automatically recalculate the computed signals above.

          this.statusMessage.set({
            text: `Successfully requested enrollment for ${course.title}!`,
            type: 'success',
          });
          this.isEnrolling.set(false);
        },
        error: (err) => {
          console.error('Enrollment failed:', err);
          this.statusMessage.set({
            text: err?.error?.detail || err?.error?.title || 'Failed to complete enrollment.',
            type: 'error',
          });
          this.isEnrolling.set(false);
        },
      });
  }
}