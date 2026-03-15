import { TestBed } from '@angular/core/testing';
import { LoggingService, ToastMessage } from './logging.service';

describe('LoggingService', () => {
  let service: LoggingService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(LoggingService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should emit an info toast when info() is called', (done) => {
    service.toasts$.subscribe((toast: ToastMessage) => {
      expect(toast.level).toBe('info');
      expect(toast.message).toBe('Test info message');
      done();
    });
    service.info('Test info message');
  });

  it('should emit a warn toast when warn() is called', (done) => {
    service.toasts$.subscribe((toast: ToastMessage) => {
      expect(toast.level).toBe('warn');
      expect(toast.message).toBe('Test warning');
      done();
    });
    service.warn('Test warning');
  });

  it('should emit an error toast when error() is called', (done) => {
    service.toasts$.subscribe((toast: ToastMessage) => {
      expect(toast.level).toBe('error');
      expect(toast.message).toBe('Test error');
      done();
    });
    service.error('Test error');
  });

  it('should not emit a toast when debug() is called', () => {
    let toastEmitted = false;
    const subscription = service.toasts$.subscribe(() => {
      toastEmitted = true;
    });
    service.debug('Debug message');
    expect(toastEmitted).toBeFalse();
    subscription.unsubscribe();
  });

  it('should assign incrementing ids to toasts', (done) => {
    const ids: number[] = [];
    let count = 0;
    const subscription = service.toasts$.subscribe((toast: ToastMessage) => {
      ids.push(toast.id);
      count++;
      if (count === 2) {
        expect(ids[1]).toBeGreaterThan(ids[0]);
        subscription.unsubscribe();
        done();
      }
    });
    service.info('first');
    service.warn('second');
  });
});
