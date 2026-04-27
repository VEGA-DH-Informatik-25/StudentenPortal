export interface AuthResponse {
  token: string;
  displayName: string;
  email: string;
  role: string;
  profile?: UserProfile;
}

export interface RegisterRequest {
  email: string;
  password: string;
  displayName: string;
  studyProgram: string;
  semester: number;
  course: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  studyProgram: string;
  semester: number;
  course: string;
  role: string;
  createdAt: string;
}

export interface UpdateProfileRequest {
  displayName: string;
  studyProgram: string;
  semester: number;
  course: string;
}
