export interface SendMessageRequest {
  content: string;
}

export interface SendMessageResponse {
  id: string;
  createdDateTime: string | null;
  webUrl: string | null;
}
