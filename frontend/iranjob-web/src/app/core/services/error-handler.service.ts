import { Injectable } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class ErrorHandlerService {
  private lastMessage = '';

  handleHttpError(error: HttpErrorResponse): void {
    if (error.status === 0) {
      this.lastMessage = 'ارتباط با سرور برقرار نشد.';
      return;
    }

    const problem = error.error as { title?: string; detail?: string } | null;
    this.lastMessage = problem?.detail ?? problem?.title ?? 'خطایی رخ داد.';
  }

  getLastMessage(): string {
    return this.lastMessage;
  }
}
