import { HttpClient } from '@angular/common/http';
import { EnvironmentProviders, makeEnvironmentProviders } from '@angular/core';
import { TranslateLoader, TranslationObject, provideTranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';

class HttpTranslateLoader implements TranslateLoader {
  constructor(private http: HttpClient) {}
  getTranslation(lang: string): Observable<TranslationObject> {
    return this.http.get<TranslationObject>(`/i18n/${lang}.json`);
  }
}

export function provideI18n(): EnvironmentProviders {
  return makeEnvironmentProviders([
    provideTranslateService({
      loader: {
        provide: TranslateLoader,
        useFactory: (http: HttpClient) => new HttpTranslateLoader(http),
        deps: [HttpClient],
      },
    }),
  ]);
}
