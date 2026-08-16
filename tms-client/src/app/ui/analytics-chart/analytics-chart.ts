import { Component, computed, input } from '@angular/core';
import { Enrollment } from '../../models/enrollment.model';

@Component({
  selector: 'tms-analytics-chart',
  standalone: true,
  template: `
    <div class="chart-container">
      <div class="chart-header">
        <h3>Enrollment Analytics</h3>
        <span class="chart-total">Total records: {{ data().length }}</span>
      </div>

      <div class="chart-bars">
        <div class="bar-wrapper">
          <div class="bar approved" [style.height.%]="approvedHeight()">
            {{ approvedCount() }}
          </div>
          <span class="bar-label">Approved</span>
        </div>

        <div class="bar-wrapper">
          <div class="bar pending" [style.height.%]="pendingHeight()">
            {{ pendingCount() }}
          </div>
          <span class="bar-label">Pending</span>
        </div>

        <div class="bar-wrapper">
          <div class="bar rejected" [style.height.%]="rejectedHeight()">
            {{ rejectedCount() }}
          </div>
          <span class="bar-label">Rejected</span>
        </div>
      </div>
    </div>
  `,
  styleUrl: './analytics-chart.scss'
})
export class AnalyticsChart {
  data = input.required<Enrollment[]>();

  private total = computed(() => this.data().length || 1);

  approvedCount = computed(() => this.data().filter(e => e.status === 'Approved').length);
  pendingCount = computed(() => this.data().filter(e => e.status === 'Pending').length);
  rejectedCount = computed(() => this.data().filter(e => e.status === 'Rejected').length);

  approvedHeight = computed(() => Math.max(12, (this.approvedCount() / this.total()) * 100));
  pendingHeight = computed(() => Math.max(12, (this.pendingCount() / this.total()) * 100));
  rejectedHeight = computed(() => Math.max(12, (this.rejectedCount() / this.total()) * 100));
}