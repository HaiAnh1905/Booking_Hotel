import { ChangeDetectionStrategy, Component, inject, Input, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SessionStateService } from '@abp/ng.core';
import { LanguageOption } from '../../models/ui-models';

@Component({
  selector: 'bh-language-switcher',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="dropdown bh-lang-dropdown" [class.show]="isOpen()">
      <button
        type="button"
        class="btn bh-lang-btn dropdown-toggle"
        (click)="toggleDropdown()"
        [attr.aria-expanded]="isOpen()"
      >
        <span class="flag-icon me-1">{{ currentLanguage()?.flagIcon }}</span>
        <span class="lang-code fw-bold">{{ currentLanguage()?.shortCode }}</span>
      </button>

      @if (isOpen()) {
        <div class="dropdown-menu dropdown-menu-end show bh-lang-menu shadow-sm">
          @for (lang of languages; track lang.cultureName) {
            <button
              type="button"
              class="dropdown-item d-flex align-items-center bh-lang-item"
              [class.active]="lang.cultureName === currentLanguage()?.cultureName"
              (click)="selectLanguage(lang.cultureName)"
            >
              <span class="flag-icon me-2">{{ lang.flagIcon }}</span>
              <span class="lang-name flex-grow-1">{{ lang.displayName }}</span>
              @if (lang.cultureName === currentLanguage()?.cultureName) {
                <i class="fa-solid fa-check text-success ms-2"></i>
              }
            </button>
          }
        </div>
      }
    </div>
  `,
  styleUrls: ['./bh-language-switcher.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BhLanguageSwitcherComponent implements OnInit {
  private sessionState = inject(SessionStateService);

  @Input() mode: 'compact' | 'full' = 'full';

  readonly languages: LanguageOption[] = [
    {
      cultureName: 'vi',
      displayName: 'Tiếng Việt',
      shortCode: 'VI',
      flagIcon: '🇻🇳',
    },
    {
      cultureName: 'en',
      displayName: 'English',
      shortCode: 'EN',
      flagIcon: '🇬🇧',
    },
  ];

  isOpen = signal<boolean>(false);
  currentLanguage = signal<LanguageOption | undefined>(this.languages[0]);

  ngOnInit() {
    const savedLang = localStorage.getItem('bh_selected_language');
    const activeCulture = savedLang || this.sessionState.getLanguage() || 'vi';
    const found = this.languages.find(l => l.cultureName.startsWith(activeCulture.substring(0, 2))) || this.languages[0];
    this.currentLanguage.set(found);

    if (savedLang && savedLang !== this.sessionState.getLanguage()) {
      this.sessionState.setLanguage(found.cultureName);
    }

    this.sessionState.getLanguage$().subscribe(lang => {
      if (lang) {
        const matching = this.languages.find(l => l.cultureName.startsWith(lang.substring(0, 2)));
        if (matching) {
          this.currentLanguage.set(matching);
        }
      }
    });
  }

  toggleDropdown() {
    this.isOpen.update(v => !v);
  }

  selectLanguage(cultureName: string) {
    this.isOpen.set(false);
    localStorage.setItem('bh_selected_language', cultureName);
    this.sessionState.setLanguage(cultureName);
    const selected = this.languages.find(l => l.cultureName === cultureName);
    if (selected) {
      this.currentLanguage.set(selected);
    }
  }
}
