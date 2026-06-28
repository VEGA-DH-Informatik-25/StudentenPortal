import { CampusGroup } from './group.model';
import { ContactProfile } from './contact.model';

export interface FeedPost {
  id: string;
  authorName: string;
  author?: ContactProfile | null;
  group: CampusGroup;
  content: string;
  translations?: FeedPostTranslations | null;
  attachments?: FeedAttachment[] | null;
  createdAt: string;
  status: 'Pending' | 'Published';
  allowComments: boolean;
  canDelete: boolean;
  canComment: boolean;
  comments: FeedComment[];
  reactions: FeedReaction[];
}

export interface CreatePostRequest {
  content: string;
  groupId?: string | null;
  allowComments?: boolean;
  translations?: FeedPostTranslations | null;
  attachments?: File[];
}

export interface FeedPostTranslations {
  de: string;
  en: string;
  fr: string;
}

export interface FeedAttachment {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  isImage: boolean;
  downloadUrl: string;
}

export interface FeedComment {
  id: string;
  authorName: string;
  author?: ContactProfile | null;
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
