import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { EnrollmentResponseDto } from '../models/enrollment.model';

@Injectable({ providedIn: 'root' })
export class EnrollmentService {
  private http = inject(HttpClient);
  private baseUrl = '/api/enrollments';

  getAll(): Observable<EnrollmentResponseDto[]> {
    return this.http.get<EnrollmentResponseDto[]>(this.baseUrl);
  }

  createEnrollment(courseId: number, payload: { studentId: number }): Observable<EnrollmentResponseDto> {
    return this.http.post<EnrollmentResponseDto>(`/api/courses/${courseId}/enrollments`, payload);
  }

  approve(id: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/approve`, {});
  }
}