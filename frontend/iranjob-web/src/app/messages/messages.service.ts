import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { ConversationItem, MessageItem } from './messages.models';

@Injectable({ providedIn: 'root' })
export class MessagesService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiBaseUrl}/api/v1/messages`;
  list() { return this.http.get<ConversationItem[]>(`${this.url}/conversations`); }
  create(jobPostingId: string, jobApplicationId: string) { return this.http.post<ConversationItem>(`${this.url}/conversations`, { jobPostingId, jobApplicationId, subject: null }); }
  history(id: string) { return this.http.get<MessageItem[]>(`${this.url}/conversations/${id}/messages`); }
  send(conversationId: string, content: string) { return this.http.post<MessageItem>(`${this.url}/send`, { conversationId, content }); }
  markRead(id: string) { return this.http.post<void>(`${this.url}/conversations/${id}/read`, {}); }
}
