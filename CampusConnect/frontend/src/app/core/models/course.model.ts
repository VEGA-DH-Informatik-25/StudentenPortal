export interface Course {
  code: string;
  studyProgram: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreateCourseRequest {
  code: string;
  studyProgram: string;
}
