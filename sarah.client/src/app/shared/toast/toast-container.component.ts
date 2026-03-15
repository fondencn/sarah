import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { LoggingService, ToastMessage } from '../../services/logging.service';

interface ActiveToast extends ToastMessage {
  visible: boolean;
}

@Component({
  selector: 'app-toast-container',
  templateUrl: './toast-container.component.html',
  styleUrls: ['./toast-container.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ToastContainerComponent implements OnInit, OnDestroy {
  toasts: ActiveToast[] = [];

  private readonly DISPLAY_DURATION: Record<string, number> = {
    info: 4000,
    warn: 6000,
    error: 8000
  };

  private subscription!: Subscription;

  constructor(
    private loggingService: LoggingService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.subscription = this.loggingService.toasts$.subscribe(toast => {
      const activeToast: ActiveToast = { ...toast, visible: true };
      this.toasts.push(activeToast);
      this.cdr.markForCheck();

      setTimeout(() => this.dismiss(toast.id), this.DISPLAY_DURATION[toast.level] ?? 5000);
    });
  }

  dismiss(id: number): void {
    this.toasts = this.toasts.filter(t => t.id !== id);
    this.cdr.markForCheck();
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
  }

  trackById(_index: number, toast: ActiveToast): number {
    return toast.id;
  }
}
