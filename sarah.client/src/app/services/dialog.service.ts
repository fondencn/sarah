import { Injectable, EventEmitter } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class DialogService {

  constructor() { }

  private isDialogOpen: boolean = false;
  private currentDialog : any | null = null;

  closed = new EventEmitter<boolean>();

  public showDialog(modalId: string): void {
    const modalElement = document.getElementById(modalId);
    if (modalElement) {
      if (!this.isDialogOpen) {
        this.isDialogOpen = true;
        const bootstrapModal = new (window as any).bootstrap.Modal(modalElement);
        this.currentDialog = bootstrapModal
        bootstrapModal.show();
        bootstrapModal._element.addEventListener('hidden.bs.modal', () => {
          this.closeDialog(false);
        } );
      }
    }
  }

  public closeDialog(success: boolean): void {
    this.currentDialog.hide();
    this.isDialogOpen = false;
    this.currentDialog = null;
    this.closed.emit(success);
  }
}
