import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { environment } from '../../environments/environment';
import { MessagesPageComponent } from './messages-page.component';

describe('MessagesPageComponent', () => {
  let fixture: ComponentFixture<MessagesPageComponent>;
  let http: HttpTestingController;
  const url = `${environment.apiBaseUrl}/api/v1/messages`;

  function create(id: string | null = null): void {
    TestBed.configureTestingModule({
      imports: [MessagesPageComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), {
        provide: ActivatedRoute,
        useValue: { snapshot: { paramMap: convertToParamMap(id ? { id } : {}) } }
      }]
    });
    fixture = TestBed.createComponent(MessagesPageComponent);
    http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  }

  afterEach(() => {
    http?.verify();
    TestBed.resetTestingModule();
  });

  it('renders the empty conversation state', () => {
    create();
    http.expectOne(`${url}/conversations`).flush([]);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('هنوز گفتگویی ندارید');
  });

  it('loads a selected conversation, then marks it read', () => {
    create('conversation-1');
    http.expectOne(`${url}/conversations`).flush([{
      id: 'conversation-1', subject: 'Job', jobPostingId: 'job-1', jobTitle: 'Engineer',
      lastMessagePreview: 'Hello', lastMessageAt: null, unreadCount: 2, otherParticipantName: 'Candidate'
    }]);
    http.expectOne(`${url}/conversations/conversation-1/messages`).flush([{
      id: 'message-1', conversationId: 'conversation-1', senderProfileId: 'profile-1', senderName: 'Candidate',
      content: 'Hello', sentAt: '2026-10-24T08:00:00Z', isRead: false, isMine: false
    }]);
    const read = http.expectOne(`${url}/conversations/conversation-1/read`);
    expect(read.request.method).toBe('POST');
    read.flush(null);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Hello');
    expect(fixture.nativeElement.textContent).toContain('Candidate');
    expect(fixture.componentInstance.conversations[0].unreadCount).toBe(0);
  });

  it('prevents duplicate sends while a request is pending and resets after failure', () => {
    create();
    http.expectOne(`${url}/conversations`).flush([]);
    const component = fixture.componentInstance;
    component.selected = {
      id: 'conversation-1', subject: 'Job', jobPostingId: null, jobTitle: null,
      lastMessagePreview: null, lastMessageAt: null, unreadCount: 0, otherParticipantName: 'Employer'
    };
    component.draft = '  Hello  ';
    component.send();
    component.send();
    const request = http.expectOne(`${url}/send`);
    expect(request.request.body).toEqual({ conversationId: 'conversation-1', content: 'Hello' });
    request.flush('failure', { status: 500, statusText: 'Server Error' });
    expect(component.sending).toBeFalse();
    expect(component.error).toContain('ارسال پیام ناموفق بود');
  });
});
