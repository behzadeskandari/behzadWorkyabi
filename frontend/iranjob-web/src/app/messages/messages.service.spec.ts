import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../environments/environment';
import { MessagesService } from './messages.service';

describe('MessagesService', () => {
  let service: MessagesService;
  let http: HttpTestingController;
  const url = `${environment.apiBaseUrl}/api/v1/messages`;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(MessagesService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads conversations from the authenticated messaging endpoint', () => {
    service.list().subscribe(items => expect(items).toEqual([]));
    const request = http.expectOne(`${url}/conversations`);
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });

  it('sends only the conversation id and normalized content', () => {
    service.send('conversation-1', 'Hello').subscribe();
    const request = http.expectOne(`${url}/send`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ conversationId: 'conversation-1', content: 'Hello' });
    request.flush({});
  });

  it('posts read-state changes for the selected conversation', () => {
    service.markRead('conversation-1').subscribe();
    const request = http.expectOne(`${url}/conversations/conversation-1/read`);
    expect(request.request.method).toBe('POST');
    request.flush(null);
  });
});
