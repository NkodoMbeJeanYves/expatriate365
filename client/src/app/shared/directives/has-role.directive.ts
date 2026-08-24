import { Directive, effect, inject, input, TemplateRef, ViewContainerRef } from '@angular/core';
import { AuthStore } from '@core/auth/auth.store';
import { Role } from '@core/auth/models/role.model';

@Directive({ selector: '[appHasRole]', standalone: true })
export class HasRoleDirective {
  readonly appHasRole = input.required<Role | Role[]>();

  private readonly store = inject(AuthStore);
  private readonly tpl = inject(TemplateRef);
  private readonly vcr = inject(ViewContainerRef);

  constructor() {
    effect(() => {
      const roles = this.appHasRole();
      const allowed = Array.isArray(roles) ? this.store.hasAnyRole(roles) : this.store.hasRole(roles);
      this.vcr.clear();
      if (allowed) this.vcr.createEmbeddedView(this.tpl);
    });
  }
}
