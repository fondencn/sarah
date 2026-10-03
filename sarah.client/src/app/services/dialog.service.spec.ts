import { TestBed } from '@angular/core/testing';
import { Modal } from 'bootstrap';
import { DialogService } from './dialog.service';
import { LoggingService } from './logging.service';

describe('DialogService', () => {
  let service: DialogService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        {
          provide: LoggingService,
          useValue: {
            debug: () => undefined
          }
        }
      ]
    });
    service = TestBed.inject(DialogService);
    spyOn(Modal.prototype, 'show').and.callFake(() => undefined);
    spyOn(Modal.prototype, 'hide').and.callFake(() => undefined);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should open a dialog', () => {
    const modalId = 'testModal';
    const modalElement = document.createElement('div');
    modalElement.id = modalId;
    document.body.appendChild(modalElement);

    spyOn(service, 'showDialog').and.callThrough();
    service.showDialog(modalId);

    expect(service.showDialog).toHaveBeenCalledWith(modalId);
    expect(service['isDialogOpen']).toBeTrue();
    expect(service['currentDialog']).toBeTruthy();

    document.body.removeChild(modalElement);
  });

  it('should close a dialog', () => {
    const modalId = 'testModal';
    const modalElement = document.createElement('div');
    modalElement.id = modalId;
    document.body.appendChild(modalElement);

    service.showDialog(modalId);
    spyOn(service, 'closeDialog').and.callThrough();
    service.closeDialog(true);

    expect(service.closeDialog).toHaveBeenCalledWith(true);
    expect(service['isDialogOpen']).toBeFalse();
    expect(service['currentDialog']).toBeNull();

    document.body.removeChild(modalElement);
  });

  it('should emit dialogClosed event on close', (done) => {
    const modalId = 'testModal';
    const modalElement = document.createElement('div');
    modalElement.id = modalId;
    document.body.appendChild(modalElement);

    service.showDialog(modalId);
    service.dialogClosed.subscribe((args) => {
      expect(args.success).toBeTrue();
      expect(args.dialogId).toBe(modalId);
      done();
    });

    service.closeDialog(true);
    document.body.removeChild(modalElement);
  });
});
