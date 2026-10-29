import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '@env/environment';
import { CurrencyValidationResponse } from '@models/currency-validation.model';

@Injectable({ providedIn: 'root' })
export class CurrencyValidationService {
  private readonly http = inject(HttpClient);

  validate(country: string, currency: string, superAdmin = false) {
    const resource = superAdmin ? 'superadmin' : 'tenant';
    return this.http.post<CurrencyValidationResponse>(
      `${environment.apiUrl}/api/v1/${resource}/currency-validation`,
      { country_code: country, base_currency: currency },
    );
  }
}
