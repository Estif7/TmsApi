import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EnrollmentStore } from '../../../store/enrollment.store';
import { Enrollment } from '../../../models/enrollment.model';

@Component({
  selector: 'tms-course-roster',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './course-roster.html',
  styleUrl: './course-roster.scss',
})
export class CourseRosterComponent implements OnInit {
  readonly store = inject(EnrollmentStore);
  readonly pageSize = 5;

  // Key-value store: { courseId: pageIndex }
  readonly pageState = signal<Record<number, number>>({});

  ngOnInit(): void {
    if (this.store.entities().length === 0) {
      this.store.loadEnrollments();
    }
  }

  getPageIndex(courseId: number): number {
    return this.pageState()[courseId] ?? 0;
  }

  getPagedStudents(courseId: number, students: Enrollment[]): Enrollment[] {
    const pageIndex = this.getPageIndex(courseId);
    const start = pageIndex * this.pageSize;
    return students.slice(start, start + this.pageSize);
  }

  getTotalPages(students: Enrollment[]): number {
    return Math.ceil(students.length / this.pageSize) || 1;
  }

  getPageNumber(courseId: number): number {
    return this.getPageIndex(courseId) + 1;
  }

  changePage(courseId: number, delta: number, totalPages: number): void {
    const current = this.getPageIndex(courseId);
    const target = current + delta;

    if (target >= 0 && target < totalPages) {
      this.pageState.update((prev) => ({
        ...prev,
        [courseId]: target
      }));
    }
  }

  onApprove(id: number): void {
    this.store.approveEnrollment(id);
  }

  onReject(id: number): void {
    this.store.rejectEnrollment(id);
  }
}