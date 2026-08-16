import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';

import { CourseCard } from '../../ui/course-card/course-card';
import { Course } from '../../models/course.model';
import { CourseService } from '../../services/course';
import { EnrollmentService } from '../../services/enrollment';
import { EnrollmentStore } from '../../store/enrollment.store';

@Component({
  selector: 'app-student-dashboard',
  standalone: true,
  imports: [CourseCard],
  templateUrl: './student-dashboard.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './student-dashboard.scss',
})
export class StudentDashboard {
  private readonly courseService = inject(CourseService);
  private readonly store = inject(EnrollmentStore);
  private readonly enrollmentService = inject(EnrollmentService);

  studentName = signal('Liya Kebede');

  earnedCredits = signal(45);

  selectedCourse = signal<Course | null>(null);

  graduationStatus = computed(() =>
    this.earnedCredits() >= 120 ? 'Eligible for Graduation' : 'In Progress',
  );

  coursesResource = rxResource({
    stream: () => this.courseService.getAll(),
    defaultValue: [] as Course[],
  });

  registerForClass() {
    this.earnedCredits.update((c) => c + 3);
  }

  handleEnroll(course: Course) {
    if (course.enrollmentCount < course.maxCapacity) {
      course.enrollmentCount++;

      this.selectedCourse.set(course);

      console.log('Enrollment requested for:', course.title);
    }
  }

  enroll(courseId: number) {
    // 1. Send create request to API
    this.enrollmentService.createEnrollment(courseId, { studentId: 101 }).subscribe({
      next: () => {
        // 2. Refresh store so new enrollment appears instantly
        this.store.loadEnrollments();
      },
      error: (err) => console.error('Failed to enroll:', err),
    });
  }
}