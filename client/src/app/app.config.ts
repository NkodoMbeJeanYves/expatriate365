import { ApplicationConfig, provideAppInitializer, provideBrowserGlobalErrorListeners, inject } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { providePrimeNG } from 'primeng/config';
import { MessageService } from 'primeng/api';
import Aura from '@primeuix/themes/aura';

import { routes } from './app.routes';
import { APP_CONFIG } from '@core/config/app-config.token';
import { environment } from '@env/environment';
import { provideI18n } from '@core/i18n/i18n.providers';
import { authInterceptor } from '@core/http/auth.interceptor';
import { errorInterceptor } from '@core/http/error.interceptor';
import { loadingInterceptor } from '@core/http/loading.interceptor';
import { AuthService } from '@core/auth/auth.service';
import { AuthStore } from '@core/auth/auth.store';
import { ThemeService } from '@core/theme/theme.service';
import { TenantService } from '@core/tenant/tenant.service';
import { TranslateService } from '@ngx-translate/core';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideAnimations(),
    provideHttpClient(withInterceptors([authInterceptor, loadingInterceptor, errorInterceptor])),
    provideRouter(routes, withComponentInputBinding()),
    provideI18n(),
    providePrimeNG({
      theme: { preset: Aura, options: { darkModeSelector: '.dark' } },
      license: 'eyJpZCI6ImI1NzQ1N2I2LTIxMDEtNDE0NC1iZWFmLTA2ODM3YjI0NGMxYSIsInByb2R1Y3QiOiJwcmltZXVpIiwidGllciI6ImNvbW11bml0eSIsInR5cGUiOiJkZXYiLCJpYXQiOjE3ODcyOTM4NjAsImV4cCI6MTgxODgyOTg2MH0.DJiMdQ6tBNtZrSfIcWGm3Kmir-7ofXZ725GUjU6BmHn4PO3vxht105PAH8oGkv0SqzuNAF70OvwPU04g836GAQ',
    }),
    { provide: APP_CONFIG, useValue: environment },
    MessageService,
    provideAppInitializer(async () => {
      // Resolve all services before any await — inject() is invalid after await
      const themeService  = inject(ThemeService);
      const translate     = inject(TranslateService);
      const authService   = inject(AuthService);
      const authStore     = inject(AuthStore);
      const tenantService = inject(TenantService);

      themeService.init();
      translate.use(localStorage.getItem('exp365_lang') ?? 'fr');
      await authService.bootstrap();
      if (localStorage.getItem('exp365_auth')) {
        await tenantService.bootstrap();
        authStore.restoreSession();
      }
    }),
  ],
};
