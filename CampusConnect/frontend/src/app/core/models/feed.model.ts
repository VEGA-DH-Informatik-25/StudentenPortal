import { CampusGroup } from './group.model';

export interface FeedPost {
  id: string;
  authorName: string;
  group: CampusGroup;
  content: string;
  createdAt: string;
}

export interface CreatePostRequest {
  content: string;
  groupId?: string | null;
}
