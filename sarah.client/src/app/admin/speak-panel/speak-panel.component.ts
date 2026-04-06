import { Component } from '@angular/core';
import { AdminClient } from '../../services/api/admin-service/api/admin.service';
import { SpeechVolumeModel } from '../../services/api/admin-service/model/speechVolume';
import { LoggingService } from '../../services/logging.service';

@Component({
  selector: 'app-speak-panel',
  templateUrl: './speak-panel.component.html',
  styleUrl: './speak-panel.component.css'
})
export class SpeakPanelComponent {
  text = '';
  volume: SpeechVolumeModel = SpeechVolumeModel.NUMBER_2; // Normal
  sending = false;
  success = false;
  error = false;

  readonly volumes = [
    { value: SpeechVolumeModel.NUMBER_3, labelKey: 'speakPanel.volumeQuieter' },
    { value: SpeechVolumeModel.NUMBER_2, labelKey: 'speakPanel.volumeNormal' },
    { value: SpeechVolumeModel.NUMBER_1, labelKey: 'speakPanel.volumeLouder' },
    { value: SpeechVolumeModel.NUMBER_0, labelKey: 'speakPanel.volumeVeryLoud' },
  ];

  constructor(
    private adminClient: AdminClient,
    private logger: LoggingService
  ) {}

  send(): void {
    if (!this.text.trim()) return;

    this.sending = true;
    this.success = false;
    this.error = false;

    this.adminClient.apiAdminSayPost({ text: this.text, volume: this.volume }).subscribe({
      next: () => {
        this.success = true;
        this.text = '';
        this.sending = false;
        setTimeout(() => (this.success = false), 3000);
      },
      error: (err) => {
        this.logger.error('Failed to send speech message', err);
        this.error = true;
        this.sending = false;
        setTimeout(() => (this.error = false), 5000);
      }
    });
  }
}
