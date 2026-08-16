import { Component, input, effect, ChangeDetectionStrategy, signal, inject } from '@angular/core';
import { RouterLink, Router } from '@angular/router';
import { CourseService } from '../../services/course';
import { CourseDetail as CourseDetailModel } from '../../models/course.model';

@Component({
  selector: 'app-course-detail',
  standalone: true,
  templateUrl: './course-detail.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  imports: [RouterLink],
})
export class CourseDetail {
  id = input.required<string>();

  private courseService = inject(CourseService);
  private router = inject(Router);

  // Signal storing course details for this view
  course = signal<CourseDetailModel | null>(null);
  isDeleting = signal<boolean>(false);

  constructor() {
    effect(() => {
      const courseId = this.id();
      console.log(`Loading course detail for ID: ${courseId}`);
      
      this.courseService.getById(courseId).subscribe({
        next: (data) => this.course.set(data),
        error: (err) => console.error('Failed to load course details', err)
      });
    });
  }

  async onDelete() {
    const currentCourse = this.course();
    if (!currentCourse) return;

    this.isDeleting.set(true);

    // 1. Snapshot local state
    const snapshot = currentCourse;

    // 2. Optimistically clear local view state
    this.course.set(null);

    try {
      // 3. Request backend deletion
      await this.courseService.delete(currentCourse.id).toPromise();

      // 4. Navigate back to student dashboard on success
      await this.router.navigate(['/student-dashboard']);
    } catch (error) {
      // 5. Restore state if backend call fails
      this.course.set(snapshot);
      this.isDeleting.set(false);
      console.error('Deletion failed — course restored.');
    }
  }
}