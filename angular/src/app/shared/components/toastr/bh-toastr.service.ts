import { Injectable, signal } from '@angular/core';
import { BhToastMessage, ToastType } from '../../models/ui-models';

@Injectable({
  providedIn: 'root',
})
export class BhToastrService {
  private toastsSignal = signal<BhToastMessage[]>([]);
  readonly toasts = this.toastsSignal.asReadonly();

  success(messageKey: string, titleKey: string = 'Toastr:Success', durationMs: number = 4000): void {
    this.addToast('success', messageKey, titleKey, durationMs);
  }

  error(messageKey: string, titleKey: string = 'Toastr:Error', durationMs: number = 5000): void {
    this.addToast('error', messageKey, titleKey, durationMs);
  }

  warning(messageKey: string, titleKey: string = 'Toastr:Warning', durationMs: number = 4500): void {
    this.addToast('warning', messageKey, titleKey, durationMs);
  }

  info(messageKey: string, titleKey: string = 'Toastr:Info', durationMs: number = 4000): void {
    this.addToast('info', messageKey, titleKey, durationMs);
  }

  remove(id: string): void {
    this.toastsSignal.update(toasts => toasts.filter(t => t.id !== id));
  }

  clearAll(): void {
    this.toastsSignal.set([]);
  }

  private addToast(type: ToastType, messageKey: string, titleKey: string, durationMs: number): void {
    const id = `toast-${Date.now()}-${Math.random().toString(36).substring(2, 7)}`;
    const newToast: BhToastMessage = {
      id,
      type,
      titleKey,
      messageKey,
      durationMs,
      timestamp: new Date(),
    };

    this.toastsSignal.update(toasts => [...toasts, newToast]);

    if (durationMs > 0) {
      setTimeout(() => {
        this.remove(id);
      }, durationMs);
    }
  }
}
