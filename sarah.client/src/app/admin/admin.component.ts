import { Component } from '@angular/core';

type AdminTab = 'ruleMonitor' | 'kernelConversation' | 'promptRules';

@Component({
  selector: 'app-admin',
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.css'
})
export class AdminComponent {
  activeTab: AdminTab = 'ruleMonitor';

  setActiveTab(tab: AdminTab): void {
    this.activeTab = tab;
  }
}
