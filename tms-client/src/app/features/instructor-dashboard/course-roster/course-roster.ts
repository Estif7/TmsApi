import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EnrollmentStore } from '../../../store/enrollment.store';
import { Enrollment } from '../../../models/enrollment.model';

@Component({
  selector: 'tms-course-roster',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './course-roster.html',
  styleUrl: './course-roster.scss'
})

export class CourseRosterComponent implements OnInit {
  readonly store = inject(EnrollmentStore);
  
  // Pagination configuration
  readonly pageSize = 5;
  // Tracks current page index per course ID: { [courseId: number]: pageIndex }
  pageMap = signal<{ [key: number]: number }>({});
  
  ngOnInit(): void {
    if (this.store.entities().length === 0) {
      this.store.loadEnrollments();
    }
  }

  // Slice roster for the active page
  getPagedStudents(courseId: number, students: Enrollment[]): Enrollment[] {
    const page = this.pageMap()[courseId] || 0;
    const start = page * this.pageSize;
    return students.slice(start, start + this.pageSize);
  }

  getTotalPages(students: Enrollment[]): number {
    return Math.ceil(students.length / this.pageSize) || 1;
  }

  getCurrentPage(courseId: number): number {
    return (this.pageMap()[courseId] || 0) + 1;
  }

  setPage(courseId: number, pageIndex: number): void {
    this.pageMap.update((map) => ({ ...map, [courseId]: pageIndex }));
  }

  onApprove(id: number): void {
    this.store.approveEnrollment(id);
  }

  onReject(id: number): void {
    this.store.rejectEnrollment(id);
  }
}