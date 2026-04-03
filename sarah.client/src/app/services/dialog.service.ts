import { Injectable, EventEmitter } from '@angular/core';
import { Modal } from 'bootstrap';
import { LoggingService } from './logging.service';

@Injectable({
  providedIn: 'root'
})
export class DialogService {


  constructor(private logger: LoggingService) { }

  private isDialogOpen: boolean = false;
  private currentDialog : any | null = null;

  public dialogClosed : EventEmitter<DialogClosedEventArgs> = new EventEmitter<DialogClosedEventArgs>();

  public showDialog(modalId: string): void {
    const modalElement = document.getElementById(modalId);
    if (modalElement) {
      if (!this.isDialogOpen) {
        this.isDialogOpen = true;
        const bootstrapModal = new Modal(modalElement);
        this.currentDialog = bootstrapModal
        this.logger.debug("Showing Dialog: " + modalId);
        bootstrapModal.show();
        modalElement.addEventListener('hidden.bs.modal', () => {
          this.closeDialog(false);
        } );
      }
    }
  }

  public closeDialog(success: boolean): void {
    if(this.currentDialog != null) {
      var dialogId = this.currentDialog._element.id;
      this.logger.debug("Closing Dialog: " + dialogId);
      this.currentDialog.hide();
      this.isDialogOpen = false;
      this.currentDialog = null;
      this.dialogClosed.emit(new DialogClosedEventArgs(dialogId, success));
    }
  }


  
  showConfirmDialog(msg: string, title: string): Promise<boolean> {
    this.logger.debug("showConfirmDialog");
    return new Promise((resolve) => {
      const modalId = 'confirmDialog';
      let modalElement = document.getElementById(modalId);

      if (!modalElement) {
        modalElement = document.createElement('div');
        modalElement.id = modalId;
        modalElement.className = 'modal fade';
        modalElement.tabIndex = -1;
        modalElement.innerHTML = `
          <div class="modal-dialog">
            <div class="modal-content">
              <div class="modal-header">
                <h5 class="modal-title">${title}</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
              </div>
              <div class="modal-body">
                <p>${msg}</p>
              </div>
              <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                <button type="button" class="btn btn-primary" id="confirmBtn">OK</button>
              </div>
            </div>
          </div>
        `;
        document.body.appendChild(modalElement);
      }

      const bootstrapModal = new Modal(modalElement);
      this.currentDialog = bootstrapModal;
      this.isDialogOpen = true;

      modalElement.querySelector('#confirmBtn')?.addEventListener('click', () => {
        this.closeDialog(true);
        resolve(true);
      });

      modalElement.addEventListener('hidden.bs.modal', () => {
        if (this.isDialogOpen) {
          this.closeDialog(false);
          resolve(false);
        }
      });

      bootstrapModal.show();
    });
  }
}

export class DialogClosedEventArgs {
  constructor(public dialogId: string, public success: boolean) 
  { }
}
