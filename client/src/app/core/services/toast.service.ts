import { Injectable, inject } from '@angular/core';
import { MessageService } from 'primeng/api';

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly msg = inject(MessageService);

  success(detail: string, summary = 'Succès'): void {
    this.msg.add({ severity: 'success', summary, detail, life: 4000 });
  }
  error(detail: string, summary = 'Erreur'): void {
    this.msg.add({ severity: 'error', summary, detail, life: 6000 });
  }
  info(detail: string, summary = 'Info'): void {
    this.msg.add({ severity: 'info', summary, detail, life: 4000 });
  }
  warn(detail: string, summary = 'Attention'): void {
    this.msg.add({ severity: 'warn', summary, detail, life: 5000 });
  }
}
