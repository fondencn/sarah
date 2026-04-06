import { Injectable } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { BehaviorSubject, Observable } from 'rxjs';

export type SupportedLang = 'de' | 'en';

const STORAGE_KEY = 'sarah-lang';
const DEFAULT_LANG: SupportedLang = 'de';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly langSubject: BehaviorSubject<SupportedLang>;
  readonly currentLang$: Observable<SupportedLang>;

  constructor(private translate: TranslateService) {
    const stored = localStorage.getItem(STORAGE_KEY) as SupportedLang | null;
    const initial: SupportedLang =
      stored === 'de' || stored === 'en' ? stored : DEFAULT_LANG;

    this.langSubject = new BehaviorSubject<SupportedLang>(initial);
    this.currentLang$ = this.langSubject.asObservable();

    translate.setDefaultLang(DEFAULT_LANG);
    translate.use(initial);
  }

  setLanguage(lang: SupportedLang): void {
    localStorage.setItem(STORAGE_KEY, lang);
    this.langSubject.next(lang);
    this.translate.use(lang);
  }

  get currentLang(): SupportedLang {
    return this.langSubject.getValue();
  }
}
