export interface AuthResponse {
  token: string;
  displayName: string;
  email: string;
  role: string;
  profile?: UserProfile;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface ChangeInitialPasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  studyProgram: string;
  course: string;
  phoneNumber: string;
  location: string;
  role: string;
  mustChangePassword: boolean;
  onboardingCompleted: boolean;
  onboardingCompletedAt: string | null;
  createdAt: string;
}

export interface UpdateProfileRequest {
  displayName: string;
  course: string;
  phoneNumber: string;
  location: string;
}
