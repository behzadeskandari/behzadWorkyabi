import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { MessagesService } from './messages.service';
import { ConversationItem, MessageItem } from './messages.models';
import { JalaliDateService } from '../core/services/jalali-date.service';

@Component({ standalone: true, selector: 'app-messages-page', imports: [CommonModule, FormsModule], template: `
<main class="messages" dir="rtl">
  <h1>پیام‌ها</h1>
  @if (error) { <p role="alert">{{error}}</p> }
  @if (loading) { <p>در حال بارگذاری…</p> }
  @else if (!conversations.length) { <p>هنوز گفتگویی ندارید.</p> }
  @else {
    <div class="layout"><nav aria-label="گفتگوها">
      @for (item of conversations; track item.id) {
        <button type="button" (click)="open(item)" [class.selected]="selected?.id === item.id">
          <strong>{{item.otherParticipantName || item.jobTitle || item.subject}}</strong>
          <span>{{item.lastMessagePreview || 'گفتگو را آغاز کنید'}}</span>
          @if (item.lastMessageAt) { <small>{{date.formatDisplayDateTime(item.lastMessageAt)}}</small> }
          @if (item.unreadCount) { <b aria-label="پیام خوانده‌نشده">{{item.unreadCount}}</b> }
        </button>
      }
    </nav>
    <section aria-label="پیام‌های گفتگو">
      @if (selected) {
        <h2>{{selected.otherParticipantName || selected.jobTitle || selected.subject}}</h2>
        <div class="history" aria-live="polite">
          @for (message of messages; track message.id) {
            <article [class.mine]="message.isMine"><strong>{{message.senderName}}</strong><p>{{message.content}}</p><small>{{date.formatDisplayDateTime(message.sentAt)}}</small></article>
          }
        </div>
        <form (ngSubmit)="send()"><label for="message">پیام شما</label><textarea id="message" name="message" [(ngModel)]="draft" maxlength="20000" required></textarea><button [disabled]="sending || !draft.trim()">{{sending ? 'در حال ارسال…' : 'ارسال'}}</button></form>
      } @else { <p>یک گفتگو را انتخاب کنید.</p> }
    </section></div>
  }
</main>`, styles: [`
.messages{max-width:1100px;margin:2rem auto;padding:1rem;color:#172033}.layout{display:grid;grid-template-columns:minmax(240px, .8fr) 2fr;gap:1rem;min-height:65vh}nav,section{border:1px solid #d9dee8;border-radius:12px;padding:1rem}nav button{position:relative;display:flex;flex-direction:column;text-align:right;width:100%;padding:1rem;border:0;border-bottom:1px solid #eee;background:white;cursor:pointer;gap:.35rem}nav button.selected,article.mine{background:#edf5ff}nav b{position:absolute;left:1rem;top:1rem;background:#2459a6;color:white;border-radius:1rem;padding:.15rem .5rem}.history{height:45vh;overflow:auto;display:flex;flex-direction:column;gap:.75rem}article{max-width:80%;padding:.75rem;border-radius:10px;background:#f4f5f7}article p{white-space:pre-wrap;margin:.5rem 0}form{display:grid;gap:.5rem;margin-top:1rem}textarea{min-height:80px;resize:vertical}button{font:inherit}@media(max-width:700px){.layout{grid-template-columns:1fr}.history{height:35vh}}
`] })
export class MessagesPageComponent implements OnInit {
  private readonly api = inject(MessagesService); private readonly route = inject(ActivatedRoute);
  readonly date = inject(JalaliDateService); conversations: ConversationItem[] = []; messages: MessageItem[] = [];
  selected: ConversationItem | null = null; draft = ''; error = ''; loading = true; sending = false;
  ngOnInit() { this.api.list().subscribe({ next: items => { this.conversations = items; const id = this.route.snapshot.paramMap.get('id'); const item = items.find(x => x.id === id); if (item) this.open(item); }, error: () => { this.error = 'بارگذاری پیام‌ها ناموفق بود.'; this.loading = false; }, complete: () => this.loading = false }); }
  open(item: ConversationItem) { this.selected = item; this.error = ''; this.api.history(item.id).subscribe({ next: rows => { this.messages = rows; this.api.markRead(item.id).subscribe({ next: () => { item.unreadCount = 0; } }); }, error: () => this.error = 'دریافت گفتگو ناموفق بود.' }); }
  send() { if (!this.selected || !this.draft.trim() || this.sending) return; this.sending = true; this.error = ''; this.api.send(this.selected.id, this.draft.trim()).subscribe({ next: message => { this.messages = [...this.messages, message]; this.draft = ''; }, error: () => { this.error = 'ارسال پیام ناموفق بود.'; this.sending = false; }, complete: () => this.sending = false }); }
}
