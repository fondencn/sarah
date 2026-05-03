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

@Component({
  selector: 'app-kernel-conversation-panel',
  templateUrl: './kernel-conversation-panel.component.html',
  styleUrl: './kernel-conversation-panel.component.css'
})
export class KernelConversationPanelComponent implements OnInit, OnDestroy {
  private readonly requestTimeoutMs = 10000;
  private tokenEventsSubscription: Subscription | null = null;

  readonly defaultHours = 2;
  readonly defaultLimit = 200;

  loading = false;
  loadError = false;
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
