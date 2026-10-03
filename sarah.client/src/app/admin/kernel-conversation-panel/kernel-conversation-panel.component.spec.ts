import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { FormsModule } from '@angular/forms';
import { OAuthService } from 'angular-oauth2-oidc';
import { of } from 'rxjs';
import { TranslateModule } from '@ngx-translate/core';

import { KernelConversationPanelComponent } from './kernel-conversation-panel.component';
import { LoggingService } from '../../services/logging.service';

describe('KernelConversationPanelComponent', () => {
  let component: KernelConversationPanelComponent;
  let fixture: ComponentFixture<KernelConversationPanelComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [KernelConversationPanelComponent],
      imports: [HttpClientTestingModule, FormsModule, TranslateModule.forRoot()],
      providers: [
        {
          provide: OAuthService,
          useValue: {
            hasValidAccessToken: () => false,
            events: of()
          }
        },
        {
          provide: LoggingService,
          useValue: {
            error: () => undefined
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(KernelConversationPanelComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should default to 2h history window', () => {
    expect(component.defaultHours).toBe(2);
  });

  it('should default truncate state to false', () => {
    expect(component.truncating).toBeFalse();
    expect(component.truncateError).toBeFalse();
    expect(component.truncateSuccess).toBeFalse();
  });

  it('should default send-message state to empty and false', () => {
    expect(component.chatInput).toBe('');
    expect(component.sendingMessage).toBeFalse();
    expect(component.sendMessageError).toBeFalse();
    expect(component.sendMessageSuccess).toBeFalse();
  });
});
