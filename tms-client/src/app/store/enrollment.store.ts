import { computed, inject } from '@angular/core';
import {
  signalStore,
  withComputed,
  withMethods,
  patchState,
  withState,
} from '@ngrx/signals';
import {
  withEntities,
  setAllEntities,
  updateEntity,
} from '@ngrx/signals/entities';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap, tap, catchError, EMPTY } from 'rxjs';
import { EnrollmentService } from '../services/enrollment';
import { Enrollment } from '../models/enrollment.model';

export const EnrollmentStore = signalStore(
  { providedIn: 'root' },
  withState({ isLoading: false, error: null as string | null }),
  withEntities<Enrollment>(),
  withComputed((store) => ({
    pendingCount: computed(
      () => store.entities().filter((e) => e.status === 'Pending').length
    ),
  })),
  withMethods((store, api = inject(EnrollmentService)) => ({
    loadEnrollments: rxMethod<void>(
      pipe(
        tap(() => patchState(store, { isLoading: true, error: null })),
        switchMap(() =>
          api.getAll().pipe(
            tap((rows) => {
              // Strictly map API DTOs into full Enrollment state objects
              const mapped: Enrollment[] = rows.map((r) => ({
                id: r.id,
                courseId: r.courseId,
                studentId: r.studentId,
                enrolledAt: r.enrolledAt,
                studentName: r.studentName ?? `Student #${r.studentId}`,
                courseName: r.courseName ?? `Course #${r.courseId}`,
                status: (r.status as 'Pending' | 'Approved' | 'Rejected') ?? 'Pending',
              }));

              patchState(store, setAllEntities(mapped), { isLoading: false });
            }),
            catchError((err) => {
              patchState(store, { isLoading: false, error: err.message });
              return EMPTY;
            })
          )
        )
      )
    ),
    approveEnrollment: rxMethod<number>(
      pipe(
        tap((id) => {
          patchState(
            store,
            updateEntity({ id, changes: { status: 'Approved' } })
          );
        }),
        switchMap((id) =>
          api.approve(id).pipe(
            catchError(() => {
              patchState(
                store,
                updateEntity({ id, changes: { status: 'Pending' } }),
                { error: 'Server rejected approval.' }
              );
              return EMPTY;
            })
          )
        )
      )
    ),
  }))
);