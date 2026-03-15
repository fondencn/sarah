import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

export type LogLevel = 'info' | 'warn' | 'error';

export interface ToastMessage {
  id: number;
  level: LogLevel;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class LoggingService {
  private toastCounter = 0;
  private readonly toastSubject = new Subject<ToastMessage>();

  readonly toasts$ = this.toastSubject.asObservable();

  info(message: string, context?: any): void {
    context !== undefined ? console.log(message, context) : console.log(message);
    this.emitToast('info', message);
  }

  warn(message: string, context?: any): void {
    context !== undefined ? console.warn(message, context) : console.warn(message);
    this.emitToast('warn', message);
  }

  error(message: string, context?: any): void {
    context !== undefined ? console.error(message, context) : console.error(message);
    this.emitToast('error', message);
  }

  /** Debug messages are written to the console only — no toast is shown. */
  debug(message: string, context?: any): void {
    context !== undefined ? console.log(message, context) : console.log(message);
  }

  private emitToast(level: LogLevel, message: string): void {
    this.toastSubject.next({ id: ++this.toastCounter, level, message });
  }
}
