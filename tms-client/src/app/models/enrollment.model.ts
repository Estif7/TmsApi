// What the store & components use internally
export interface Enrollment {
  id: number;
  courseId: number;
  studentId: number;
  enrolledAt: string;
  studentName: string;
  courseName: string;
  status: 'Pending' | 'Approved' | 'Rejected';
}

// What comes back directly from the ASP.NET Core API DTO
export interface EnrollmentResponseDto {
  id: number;
  courseId: number;
  studentId: number;
  enrolledAt: string;
  studentName?: string;
  courseName?: string;
  status?: string;
}