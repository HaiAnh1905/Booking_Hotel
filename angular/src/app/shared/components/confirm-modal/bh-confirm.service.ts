import { Injectable, signal } from '@angular/core';

export type BhConfirmType = 'danger' | 'warning' | 'info' | 'primary';

export interface BhConfirmOptions {
  title?: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  type?: BhConfirmType;
  iconClass?: string;
}

export interface BhConfirmState extends Required<Pick<BhConfirmOptions, 'message' | 'type'>> {
  title: string;
  confirmText: string;
  cancelText: string;
  iconClass: string;
}

@Injectable({
  providedIn: 'root',
})
export class BhConfirmService {
  private readonly stateSignal = signal<BhConfirmState | null>(null);
  readonly state = this.stateSignal.asReadonly();

  private pendingResolve: ((result: boolean) => void) | null = null;

  confirm(options: BhConfirmOptions | string): Promise<boolean> {
    if (this.pendingResolve) {
      this.pendingResolve(false);
      this.pendingResolve = null;
    }

    const normalized: BhConfirmOptions =
      typeof options === 'string' ? { message: options } : options;

    const type: BhConfirmType = normalized.type ?? 'primary';

    const state: BhConfirmState = {
      title: normalized.title ?? this.getDefaultTitle(type),
      message: normalized.message,
      confirmText: normalized.confirmText ?? 'Xác nhận',
      cancelText: normalized.cancelText ?? 'Hủy',
      type,
      iconClass: normalized.iconClass ?? this.getDefaultIcon(type),
    };

    this.stateSignal.set(state);

    return new Promise<boolean>(resolve => {
      this.pendingResolve = resolve;
    });
  }

  accept(): void {
    this.close(true);
  }

  reject(): void {
    this.close(false);
  }

  private close(result: boolean): void {
    const resolve = this.pendingResolve;
    this.pendingResolve = null;
    this.stateSignal.set(null);
    if (resolve) {
      resolve(result);
    }
  }

  private getDefaultTitle(type: BhConfirmType): string {
    switch (type) {
      case 'danger':
        return 'Xác nhận thao tác';
      case 'warning':
        return 'Cảnh báo xác nhận';
      case 'info':
        return 'Thông báo xác nhận';
      default:
        return 'Xác nhận';
    }
  }

  private getDefaultIcon(type: BhConfirmType): string {
    switch (type) {
      case 'danger':
        return 'fa-solid fa-trash-can';
      case 'warning':
        return 'fa-solid fa-triangle-exclamation';
      case 'info':
        return 'fa-solid fa-circle-info';
      default:
        return 'fa-solid fa-circle-question';
    }
  }
}
