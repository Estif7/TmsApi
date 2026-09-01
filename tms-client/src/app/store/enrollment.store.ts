import { computed, inject, effect } from '@angular/core';
import {
  signalStore,
  withComputed,
  withMethods,
  patchState,
  withState,
  withHooks,
} from '@ngrx/signals';
import {
  withEntities,
  setAllEntities,
  updateEntity,
} from '@ngrx/signals/entities';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap, tap, catchError, retry, timer, EMPTY } from 'rxjs';
import { EnrollmentService } from '../services/enrollment';
import { SignalrService } from '../services/signalr';
import { Enrollment } from '../models/enrollment.model';

export const EnrollmentStore = signalStore(
  { providedIn: 'root' },
  withState({ isLoading: false, error: null as string | null }),
  withEntities<Enrollment>(),
  withComputed((store) => ({
    pendingCount: computed(
      () => store.entities().filter((e) => e.status === 'Pending').length
    ),
    rosterByCourse: computed(() => {
      const map = new Map<
        number,
        { courseId: number; courseName: string; students: Enrollment[] }
      >();

      for (const item of store.entities()) {
        const id = item.courseId;
        // Fallback name if API property was missing or empty
        const name = item.courseName || `Course #${id}`;

        if (!map.has(id)) {
          map.set(id, {
            courseId: id,
            courseName: name,
            students: [],
          });
        }
        map.get(id)!.students.push(item);
      }

      return Array.from(map.values());
    }),
  })),
  
  withMethods(
    (
      store,
      api = inject(EnrollmentService),
      signalr = inject(SignalrService)
    ) => ({
      loadEnrollments: rxMethod<void>(
        pipe(
          tap(() => patchState(store, { isLoading: true, error: null })),
          switchMap(() =>
            api.getAll().pipe(
              // Defensive RxJS: Retry up to 2 times with 1-second delay
              retry({ count: 2, delay: 1000 }),
              tap((rows) => {
                const mapped: Enrollment[] = rows.map((r) => ({
                  id: r.id,
                  courseId: r.courseId,
                  studentId: r.studentId,
                  enrolledAt: r.enrolledAt,
                  studentName: r.studentName ?? `Student #${r.studentId}`,
                  courseName: r.courseName ?? `Course #${r.courseId}`,
                  status:
                    (r.status as 'Pending' | 'Approved' | 'Rejected') ||
                    'Pending',
                }));
                patchState(store, setAllEntities(mapped), { isLoading: false });
              }),
              catchError((err) => {
                patchState(store, {
                  isLoading: false,
                  error: 'Failed to load enrollments after retries.',
                });
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
                // Rollback state if server request fails
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

  rejectEnrollment: rxMethod<number>(
    pipe(
      tap((id) => {
        // Optimistic update using NgRx entities helper
        patchState(
          store,
          updateEntity({ id, changes: { status: 'Rejected' } })
        );
      }),
      switchMap((id) =>
        api.reject(id).pipe(
          catchError(() => {
            // Rollback state if server request fails
            patchState(
              store,
              updateEntity({ id, changes: { status: 'Pending' } }),
              { error: 'Server rejected the request.' }
            );
            return EMPTY;
          })
        )
      )
    )
  ),

      // Real-time listener method
      handleRealtimeUpdate(update: {
        enrollmentId: number;
        status: 'Pending' | 'Approved' | 'Rejected';
      }) {
        patchState(
          store,
          updateEntity({
            id: update.enrollmentId,
            changes: { status: update.status },
          })
        );
      },
    })
  ),
  withHooks({
    onInit(store) {
      const signalr = inject(SignalrService);
      signalr.startConnection();

      // Listen to incoming SignalR messages and update store state automatically
      effect(() => {
        const update = signalr.latestUpdate();
        if (update) {
          store.handleRealtimeUpdate(update);
        }
      });
    },
  })
);