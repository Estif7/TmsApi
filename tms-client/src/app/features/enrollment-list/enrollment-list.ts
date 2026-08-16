import { Component, inject, OnInit, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { EnrollmentStore } from '../../store/enrollment.store';
import { Enrollment } from '../../models/enrollment.model';
import { effect } from '@angular/core';

@Component({
  selector: 'app-enrollment-list',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatButtonModule,
    MatChipsModule,
  ],
  templateUrl: './enrollment-list.html',
})
export class EnrollmentList implements OnInit, AfterViewInit {
  store = inject(EnrollmentStore);

  displayedColumns: string[] = [
    'id',
    'studentName',
    'courseName',
    'enrolledAt',
    'status',
    'actions',
  ];

  dataSource = new MatTableDataSource<Enrollment>([]);

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  constructor() {
    // Keep MatTableDataSource in sync with SignalStore entities
    effect(() => {
      this.dataSource.data = this.store.entities();
    });
  }

  ngOnInit(): void {
    this.store.loadEnrollments();
  }

  ngAfterViewInit(): void {
    this.dataSource.paginator = this.paginator;
    this.dataSource.sort = this.sort;
  }

  approve(id: number): void {
    this.store.approveEnrollment(id);
  }
}