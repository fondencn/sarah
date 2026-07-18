import { Component, OnDestroy, OnInit } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { finalize, timeout } from 'rxjs/operators';
import { Subscription } from 'rxjs';
import { OAuthService } from 'angular-oauth2-oidc';
import { environment } from '../../../environments/environment';
import { LoggingService } from '../../services/logging.service';

interface KernelConversationMessage {
  id: number;
  conversationId: string;
  role: string;
  content: string;
  createdAtUtc: string;
}

interface KernelChatMessageResponse {
  assistantMessage: string;
}

@Component({
  selector: 'app-kernel-conversation-panel',
  templateUrl: './kernel-conversation-panel.component.html',
  styleUrl: './kernel-conversation-panel.component.css'
})
export class KernelConversationPanelComponent implements OnInit, OnDestroy {
  private readonly requestTimeoutMs = 10000;
  private readonly chatRequestTimeoutMs = 180000;
  private tokenEventsSubscription: Subscription | null = null;

  readonly defaultHours = 2;
  readonly defaultLimit = 200;

  loading = false;
  loadError = false;
  truncating = false;
  truncateError = false;
  truncateSuccess = false;
  sendingMessage = false;
  sendMessageError = false;
  sendMessageSuccess = false;
  chatInput = '';
  messages: KernelConversationMessage[] = [];

  constructor(
    private http: HttpClient,
    private oauthService: OAuthService,
    private logger: LoggingService
  ) {}

  ngOnInit(): void {
    if (this.oauthService.hasValidAccessToken()) {
      this.loadMessages();
      return;
    }

    this.tokenEventsSubscription = this.oauthService.events.subscribe((event) => {
      if (event.type === 'token_received' && this.oauthService.hasValidAccessToken()) {
        this.loadMessages();
      }
    });
  }

  ngOnDestroy(): void {
    this.tokenEventsSubscription?.unsubscribe();
    this.tokenEventsSubscription = null;
  }

  loadMessages(): void {
    this.loading = true;
    this.loadError = false;

    const url = `${environment.api.rulesService}/api/Rules/kernel-conversation`;
    const params = new HttpParams()
      .set('hours', this.defaultHours.toString())
      .set('limit', this.defaultLimit.toString());

    this.http.get<KernelConversationMessage[]>(url, { params }).pipe(
      timeout(this.requestTimeoutMs),
      finalize(() => {
        this.loading = false;
      })
    ).subscribe({
      next: (rows) => {
        this.messages = rows ?? [];
      },
      error: (err) => {
        this.logger.error('Fehler beim Laden der Kernel-Konversation', err);
        this.loadError = true;
      }
    });
  }

  truncateMessages(): void {
    const confirmed = window.confirm('Kernel-Konversationsverlauf wirklich komplett loeschen?');
    if (!confirmed) {
      return;
    }

    this.truncating = true;
    this.truncateError = false;
    this.truncateSuccess = false;

    const url = `${environment.api.rulesService}/api/Rules/kernel-conversation`;

    this.http.delete(url).pipe(
      timeout(this.requestTimeoutMs),
      finalize(() => {
        this.truncating = false;
      })
    ).subscribe({
      next: () => {
        this.truncateSuccess = true;
        this.messages = [];
        this.loadMessages();
      },
      error: (err) => {
        this.logger.error('Fehler beim Loeschen der Kernel-Konversation', err);
        this.truncateError = true;
      }
    });
  }

  sendMessage(): void {
    const message = this.chatInput.trim();
    if (!message) {
      return;
    }

    this.sendingMessage = true;
    this.sendMessageError = false;
    this.sendMessageSuccess = false;

    const url = `${environment.api.rulesService}/api/Rules/kernel-conversation/message`;

    this.http.post<KernelChatMessageResponse>(url, { message }).pipe(
      timeout(this.chatRequestTimeoutMs),
      finalize(() => {
        this.sendingMessage = false;
      })
    ).subscribe({
      next: () => {
        this.chatInput = '';
        this.sendMessageSuccess = true;
        this.loadMessages();
      },
      error: (err) => {
        this.logger.error('Fehler beim Senden einer Kernel-Chat-Nachricht', err);
        this.sendMessageError = true;
      }
    });
  }

  asRoleClass(role: string | undefined): string {
    switch ((role ?? '').toLowerCase()) {
      case 'assistant':
        return 'bg-primary';
      case 'tool':
        return 'bg-info text-dark';
      case 'system':
        return 'bg-secondary';
      default:
        return 'bg-success';
    }
  }
}
