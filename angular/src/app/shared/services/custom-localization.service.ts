import { Injectable } from '@angular/core';
import { LocalizationService, LocalizationWithDefault } from '@abp/ng.core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { FALLBACK_EN, FALLBACK_VI } from '../localization/fallback-localizations';

/**
 * CustomLocalizationService:
 * Mở rộng LocalizationService của ABP Framework với cơ chế 3 lớp bảo vệ:
 * 1. Tự động nhận diện cả cú pháp chuẩn '::Key' và cú pháp trực tiếp 'Key' (không cần '::').
 * 2. Tự động fallback sang từ điển nội bộ (FALLBACK_VI / FALLBACK_EN) khi backend đang khởi động,
 *    gặp độ trễ mạng hoặc chưa kịp tải cấu hình application-configuration.
 * 3. Đảm bảo UI người dùng không bao giờ bị lộ raw localization keys trên màn hình.
 */
@Injectable({ providedIn: 'root' })
export class CustomLocalizationService extends LocalizationService {
  override instant(key: string | LocalizationWithDefault, ...interpolateParams: string[]): string {
    const { cleanKey, withPrefix, rawKey, defaultValue } = this.resolveKeyMeta(key);

    if (!cleanKey) {
      return defaultValue || '';
    }

    // 1. Thử gọi ABP instant với cú pháp chuẩn '::Key'
    const abpResult = super.instant(
      typeof key === 'object' ? { ...key, key: withPrefix } : withPrefix,
      ...interpolateParams
    );

    if (this.isValidTranslation(abpResult, rawKey, cleanKey, withPrefix)) {
      return abpResult;
    }

    // 2. Fallback sang từ điển nội bộ theo ngôn ngữ hiện tại
    const fallbackText = this.lookupFallback(cleanKey, interpolateParams);
    if (fallbackText) {
      return fallbackText;
    }

    // 3. Cuối cùng mới trả về kết quả mặc định
    return defaultValue || abpResult || rawKey;
  }

  override get(key: string | LocalizationWithDefault, ...interpolateParams: string[]): Observable<string> {
    const { cleanKey, withPrefix, rawKey, defaultValue } = this.resolveKeyMeta(key);

    if (!cleanKey) {
      return super.get(key, ...interpolateParams);
    }

    return super
      .get(
        typeof key === 'object' ? { ...key, key: withPrefix } : withPrefix,
        ...interpolateParams
      )
      .pipe(
        map(abpResult => {
          if (this.isValidTranslation(abpResult, rawKey, cleanKey, withPrefix)) {
            return abpResult;
          }

          const fallbackText = this.lookupFallback(cleanKey, interpolateParams);
          if (fallbackText) {
            return fallbackText;
          }

          return defaultValue || abpResult || rawKey;
        })
      );
  }

  private resolveKeyMeta(key: string | LocalizationWithDefault) {
    let rawKey = '';
    let defaultValue = '';

    if (typeof key === 'string') {
      rawKey = key;
    } else if (key && typeof key === 'object') {
      rawKey = key.key || '';
      defaultValue = key.defaultValue || '';
    }

    let cleanKey = rawKey;
    if (cleanKey.includes('::')) {
      const parts = cleanKey.split('::');
      cleanKey = parts[parts.length - 1];
    }

    const withPrefix = cleanKey ? `::${cleanKey}` : '';

    return { cleanKey, withPrefix, rawKey, defaultValue };
  }

  private isValidTranslation(result: string, rawKey: string, cleanKey: string, withPrefix: string): boolean {
    if (!result) return false;
    if (result === rawKey) return false;
    if (result === cleanKey) return false;
    if (result === withPrefix) return false;
    if (result === `BookingHotel::${cleanKey}`) return false;
    return true;
  }

  private lookupFallback(cleanKey: string, interpolateParams: string[]): string | null {
    let currentLang = this.currentLang;
    if (!currentLang && typeof window !== 'undefined' && window.localStorage) {
      currentLang = localStorage.getItem('bh_selected_language') || 'vi';
    }
    const isEnglish = (currentLang || '').toLowerCase().startsWith('en');
    const primaryDict = isEnglish ? FALLBACK_EN : FALLBACK_VI;
    const secondaryDict = isEnglish ? FALLBACK_VI : FALLBACK_EN;

    const rawTranslation = primaryDict[cleanKey] || secondaryDict[cleanKey];
    if (rawTranslation) {
      return this.interpolateParams(rawTranslation, interpolateParams);
    }

    return null;
  }

  private interpolateParams(text: string, params: string[]): string {
    if (!params || !params.length) return text;
    return text.replace(/\{(\d+)\}/g, (match, index) => {
      const idx = parseInt(index, 10);
      return params[idx] !== undefined ? String(params[idx]) : match;
    });
  }
}
