import { TestBed } from '@angular/core/testing';
import { DialogService } from './dialog.service';

describe('DialogService', () => {
  let service: DialogService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(DialogService);
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
    service.dialogClosed.subscribe((success) => {
      expect(success).toBeTrue();
      done();
    });

    service.closeDialog(true);
    document.body.removeChild(modalElement);
  });
});
