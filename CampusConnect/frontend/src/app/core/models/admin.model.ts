import { Course, CreateCourseRequest } from './course.model';

export interface AdminUser {
  id: string;
  email: string;
  displayName: string;
  studyProgram: string;
  course: string;
  role: string;
  isActive: boolean;
  createdAt: string;
}

export interface UpdateUserRoleRequest {
  role: string;
}

export interface UpdateUserCourseRequest {
  courseCode: string;
}

export interface CreateAdminUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  role: string;
  courseCode: string;
  initialPassword: string;
  isActive: boolean;
}

export interface ResetUserPasswordRequest {
  initialPassword: string;
}

export interface UpdateAdminUserRequest {
  displayName: string;
  email: string;
  role: string;
  courseCode: string;
  isActive: boolean;
}

export interface UpdateUserStatusRequest {
  isActive: boolean;
}

export type AdminCourse = Course;
export type AdminCreateCourseRequest = CreateCourseRequest;
