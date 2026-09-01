import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

export interface Course {
  id: number;
  code: string;
  title: string;
  maxCapacity: number;
  enrollmentCount?: number;
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './admin-dashboard.html',
  styleUrl: './admin-dashboard.scss'
})
export class AdminDashboard implements OnInit {
  private http = inject(HttpClient);
  private fb = inject(FormBuilder);

  courses = signal<Course[]>([]);
  isLoading = signal<boolean>(false);
  errorMessage = signal<string | null>(null);

  // Match the pattern required by CreateCourseRequest: ^[A-Z]{3}-\d{3}$
  courseForm = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.pattern(/^[A-Z]{3}-\d{3}$/)]],
    title: ['', [Validators.required, Validators.maxLength(200)]],
    maxCapacity: [30, [Validators.required, Validators.min(1), Validators.max(200)]]
  });

  ngOnInit(): void {
    this.loadCourses();
  }

  loadCourses(): void {
    this.isLoading.set(true);
    this.http.get<any>('/api/v1/courses').subscribe({
      next: (response) => {
        // Unwraps PagedResponse<CourseResponseDto> or array
        const items = Array.isArray(response) ? response : (response.items ?? []);
        this.courses.set(items);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.detail || 'Failed to load courses.');
        this.isLoading.set(false);
      }
    });
  }

  onCreateCourse(): void {
    if (this.courseForm.invalid) {
      this.courseForm.markAllAsTouched();
      return;
    }

    // Explicitly match CreateCourseRequest keys: { Code, Title, MaxCapacity }
    const payload = this.courseForm.getRawValue();

    this.http.post<Course>('/api/v1/courses', payload).subscribe({
      next: (created) => {
        this.courses.update(list => [...list, created]);
        this.courseForm.reset({ maxCapacity: 30 });
        this.errorMessage.set(null);
      },
      error: (err) => {
        const errorMsg = err.error?.detail || err.error?.title || 'Error creating course.';
        this.errorMessage.set(errorMsg);
      }
    });
  }

  onDeleteCourse(id: number): void {
    if (!confirm('Are you sure you want to delete this course?')) return;

    this.http.delete(`/api/v1/courses/${id}`).subscribe({
      next: () => {
        this.courses.update(list => list.filter(c => c.id !== id));
        this.errorMessage.set(null);
      },
      error: (err) => {
        const detail = err.error?.detail || 'Failed to delete course. Ensure the course has no active enrollments.';
        this.errorMessage.set(detail);
      }
    });
  }
}