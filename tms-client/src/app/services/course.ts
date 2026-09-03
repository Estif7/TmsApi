import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs/operators';
import { firstValueFrom } from 'rxjs';

import { Course, CourseDetail, PagedResponse } from '../models/course.model';

@Injectable({
  providedIn: 'root'
})
export class CourseService {
  private http = inject(HttpClient);

  private baseUrl = 'http://localhost:5121/api/v1/courses';

  getAll() {
    return this.http
      .get<PagedResponse<Course>>(this.baseUrl, {
        params: {
          page: '1',
          pageSize: '200'
        }
      })
      .pipe(map((p) => p.items ?? []));
  }

  getById(id: string) {
    return this.http.get<CourseDetail>(`${this.baseUrl}/${id}`);
  }

  // Session 3: Delete API call
  delete(id: number | string) {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  // Session 3: Helper for optimistic deletion with snapshot rollback
  async deleteOptimistic(
    courses: Course[],
    targetId: number | string,
    updateState: (updated: Course[]) => void
  ): Promise<void> {
    // 1. Take a snapshot of current state
    const snapshot = [...courses];

    // 2. Immediately remove from UI (optimistic update)
    const filtered = courses.filter((c) => c.id !== targetId);
    updateState(filtered);

    try {
      // 3. Perform backend API deletion
      await firstValueFrom(this.delete(targetId));
    } catch (error) {
      // 4. On failure, revert back to original snapshot
      updateState(snapshot);
      throw error;
    }
  }
}