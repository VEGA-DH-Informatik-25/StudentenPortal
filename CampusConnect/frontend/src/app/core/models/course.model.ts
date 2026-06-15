export interface Course {
  code: string;
  studyProgram: string;
  semester: number | null;
  isActive: boolean;
  createdAt: string;
}

export interface CreateCourseRequest {
  code: string;
  studyProgram: string;
  semester: number | null;
}
