export interface FeedPost {
  id: string;
  authorName: string;
  content: string;
  createdAt: string;
}

export interface CreatePostRequest {
  content: string;
}
