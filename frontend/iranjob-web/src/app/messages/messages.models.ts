export interface ConversationItem {
  id: string; subject: string; jobPostingId: string | null; jobTitle: string | null;
  lastMessagePreview: string | null; lastMessageAt: string | null; unreadCount: number;
  otherParticipantName: string | null;
}
export interface MessageItem {
  id: string; conversationId: string; senderProfileId: string; senderName: string;
  content: string; sentAt: string; isRead: boolean;
  isMine: boolean;
}
