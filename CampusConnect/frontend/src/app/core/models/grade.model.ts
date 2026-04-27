export interface Grade {
  id: string;
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
  moduleName: string;
  value: number;
  ects: number;
}
