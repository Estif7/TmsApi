import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EnrollmentStore } from '../../store/enrollment.store';
import { AnalyticsChart } from '../../ui/analytics-chart/analytics-chart';
import { CourseRosterComponent } from './course-roster/course-roster';

@Component({
  selector: 'app-instructor-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, AnalyticsChart, CourseRosterComponent], // <-- Add here
  templateUrl: './instructor-dashboard.html',
  styleUrl: './instructor-dashboard.scss'
})
export class InstructorDashboard implements OnInit {
  readonly store = inject(EnrollmentStore);

  filterStatus = signal<'All' | 'Pending' | 'Approved' | 'Rejected'>('All');

  ngOnInit(): void {
    this.store.loadEnrollments();
  }

  onApprove(id: number): void {
    this.store.approveEnrollment(id);
  }

  onReject(id: number): void {
    this.store.rejectEnrollment(id);
  }

  filteredEnrollments() {
    const status = this.filterStatus();
    const all = this.store.entities();
    if (status === 'All') return all;
    return all.filter((e) => e.status === status);
  }
}