export interface Course {
  code: string;
  studyProgram: string;
  semester: number;
  isActive: boolean;
  createdAt: string;
}

export interface CreateCourseRequest {
  code: string;
  studyProgram: string;
  semester: number;
}
