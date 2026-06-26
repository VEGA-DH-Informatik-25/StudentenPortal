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
