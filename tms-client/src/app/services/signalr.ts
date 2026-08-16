import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';

export interface EnrollmentUpdatedMessage {
  enrollmentId: number;
  status: 'Pending' | 'Approved' | 'Rejected';
}

@Injectable({ providedIn: 'root' })
export class SignalrService {
  private hubConnection!: signalR.HubConnection;
  
  // Signal to hold incoming real-time updates
  latestUpdate = signal<EnrollmentUpdatedMessage | null>(null);

  startConnection(): void {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/enrollments')
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => console.log('SignalR Hub Connection Established'))
      .catch((err) => console.error('Error connecting to SignalR Hub:', err));

    this.hubConnection.on('EnrollmentStatusUpdated', (data: EnrollmentUpdatedMessage) => {
      this.latestUpdate.set(data);
    });
  }
}