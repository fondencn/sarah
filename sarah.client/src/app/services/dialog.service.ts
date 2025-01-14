import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class DialogService {

  constructor() { }

  private isDialogOpen: boolean = false;
  private currentDialog : HTMLElement | null = null;

  public showDialog(modalId: string): void {
    const modalElement = document.getElementById(modalId);
    if (modalElement) {
      if (!this.isDialogOpen) {
        this.isDialogOpen = true;
        const bootstrapModal = new (window as any).bootstrap.Modal(modalElement);
        this.currentDialog = bootstrapModal
        bootstrapModal.show();
      }
    }
  }

  public hideDialog(): void {
    const modalElement = this.currentDialog;
    if (modalElement && this.isDialogOpen) {
      const bootstrapModal = new (window as any).bootstrap.Modal(modalElement);
      bootstrapModal.hide();
    }
    this.isDialogOpen = false;
    this.currentDialog = null;
  }
}
