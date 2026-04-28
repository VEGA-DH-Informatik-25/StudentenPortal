import { CampusGroup } from './group.model';

export interface FeedPost {
  id: string;
  authorName: string;
  group: CampusGroup;
  content: string;
  createdAt: string;
  canDelete: boolean;
  canComment: boolean;
  comments: FeedComment[];
  reactions: FeedReaction[];
}

export interface CreatePostRequest {
  content: string;
  groupId?: string | null;
}

export interface FeedComment {
  id: string;
  authorName: string;
  content: string;
  createdAt: string;
  canDelete: boolean;
}

export interface FeedReaction {
  emoji: string;
  count: number;
  reactedByCurrentUser: boolean;
}

export interface CreateCommentRequest {
  content: string;
}

export interface ToggleReactionRequest {
  emoji: string;
}
