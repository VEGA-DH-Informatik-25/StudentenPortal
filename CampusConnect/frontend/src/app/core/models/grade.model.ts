export interface Grade {
  id: string;
  moduleCode?: string | null;
  moduleName: string;
  value: number;
  ects: number;
  createdAt: string;
}

export interface GradeSummary {
  grades: Grade[];
  weightedAverage: number;
  totalEcts: number;
}

export interface AddGradeRequest {
  moduleName?: string | null;
  moduleCode?: string | null;
  value: number;
  ects?: number | null;
}

export interface GradePlan {
  courseCode: string;
  studyProgram: string;
  sourceUrl: string;
  retrievedAt: string;
  modules: GradePlanModule[];
}

export interface GradePlanModule {
  code: string;
  name: string;
  studyYear: number | null;
  ects: number;
  isRequired: boolean;
  isCompleted: boolean;
  grade: number | null;
  exams: GradePlanExam[];
}

export interface GradePlanExam {
  name: string;
  scope: string;
  isGraded: boolean | null;
}
