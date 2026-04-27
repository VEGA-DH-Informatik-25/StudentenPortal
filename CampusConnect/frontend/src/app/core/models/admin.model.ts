export interface AdminUser {
  id: string;
  email: string;
  displayName: string;
  studyProgram: string;
  semester: number;
  course: string;
  role: string;
  createdAt: string;
}

export interface UpdateUserRoleRequest {
  role: string;
}