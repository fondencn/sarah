import { DialogService } from "./dialog.service";

export abstract class DialogContent {


  // Abstract method that must be implemented by subclasses
  protected abstract canOk(): boolean;


  constructor(private dialogService: DialogService) { }
  
  public onCancel() {
    this.dialogService.closeDialog(false);
  }


  public onOk() {
    if (this.canOk()) {
      this.dialogService.closeDialog(true);
    }
  }
}