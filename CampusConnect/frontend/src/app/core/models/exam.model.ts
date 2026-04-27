export interface ExamEntry {
  id: string;
  moduleName: string;
  examDate: string;
  location: string | null;
  notes: string | null;
}

export interface AddExamRequest {
  moduleName: string;
  examDate: string;
  location?: string;
  notes?: string;
}
