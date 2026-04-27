export interface TimetableEvent {
  id: string;
  title: string;
  start: string;
  end: string;
  location: string | null;
  description: string | null;
  isAllDay: boolean;
  isOnline: boolean;
}

export interface TimetableDay {
  date: string;
  events: TimetableEvent[];
}

export interface TimetableResponse {
  course: string;
  timezone: string;
  days: TimetableDay[];
}
