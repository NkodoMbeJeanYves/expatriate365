import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TagModule } from 'primeng/tag';
import { ButtonModule } from 'primeng/button';
import { Member } from '@core/models/member.model';
import { MembersApiService } from '@members/services/members-api.service';
import { AppCurrencyPipe } from '@core/tenant/app-currency.pipe';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-my-membership',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslatePipe, ProgressSpinnerModule, TagModule, ButtonModule, DatePipe],
  template: `
    <div class="p-6 max-w-2xl mx-auto flex flex-col gap-6">

      <div class="flex items-center gap-3">
        <a routerLink="/profile" class="text-gray-400 hover:text-gray-700 transition-colors">
          <i class="pi pi-arrow-left"></i>
        </a>
        <div>
          <h1 class="text-xl font-semibold text-gray-900 dark:text-white">Ma fiche membre</h1>
          <p class="text-sm text-gray-500">Vos informations d'adhésion</p>
        </div>
      </div>

      @if (loading()) {
        <div class="flex justify-center py-16"><p-progressspinner strokeWidth="4" /></div>
      } @else if (error()) {
        <div class="bg-amber-50 border border-amber-200 rounded-xl p-5 text-amber-700 text-sm">
          <i class="pi pi-exclamation-triangle mr-2"></i>
          Aucune fiche membre n'est liée à votre compte. Contactez un administrateur.
        </div>
      } @else if (member()) {
        <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 divide-y divide-gray-100 dark:divide-gray-700">

          <!-- Header card -->
          <div class="p-6 flex items-center gap-4">
            <div class="w-14 h-14 rounded-full bg-indigo-100 flex items-center justify-center text-indigo-700 font-bold text-xl">
              {{ (member()!.first_name[0] + member()!.last_name[0]).toUpperCase() }}
            </div>
            <div>
              <p class="text-lg font-semibold text-gray-900 dark:text-white">{{ member()!.first_name }} {{ member()!.last_name }}</p>
              <p class="text-sm text-indigo-600 font-mono font-medium">{{ member()!.membership_number }}</p>
              <p class="text-xs text-gray-400 mt-0.5">{{ member()!.category_name ?? 'Aucune catégorie' }}</p>
            </div>
            <div class="ml-auto">
              <span [class]="statusClass(member()!.status)"
                class="text-xs font-semibold px-3 py-1.5 rounded-full">
                {{ member()!.status | titlecase }}
              </span>
            </div>
          </div>

          <!-- Membership details -->
          <div class="p-6 grid grid-cols-2 gap-5">
            <div>
              <p class="text-xs text-gray-400 mb-1">Date d'adhésion</p>
              <p class="text-sm font-medium text-gray-800 dark:text-white">{{ member()!.joined_date | date:'longDate' }}</p>
            </div>
            @if (member()!.expiry_date) {
              <div>
                <p class="text-xs text-gray-400 mb-1">Date d'expiration</p>
                <p class="text-sm font-medium" [class]="isExpiringSoon() ? 'text-orange-600 font-semibold' : 'text-gray-800 dark:text-white'">
                  {{ member()!.expiry_date | date:'longDate' }}
                  @if (isExpiringSoon()) { <span class="text-xs ml-1">⚠️ Bientôt</span> }
                </p>
              </div>
            }
            @if (member()!.address) {
              <div class="col-span-2">
                <p class="text-xs text-gray-400 mb-1">Adresse</p>
                <p class="text-sm text-gray-800 dark:text-white">{{ member()!.address }}</p>
              </div>
            }
            @if (member()!.profession) {
              <div>
                <p class="text-xs text-gray-400 mb-1">Profession</p>
                <p class="text-sm text-gray-800 dark:text-white">{{ member()!.profession }}</p>
              </div>
            }
            @if (member()!.gender) {
              <div>
                <p class="text-xs text-gray-400 mb-1">Genre</p>
                <p class="text-sm text-gray-800 dark:text-white">{{ member()!.gender }}</p>
              </div>
            }
          </div>

          <!-- Emergency contact -->
          @if (member()!.emergency_contact_name || member()!.emergency_contact_phone) {
            <div class="p-6">
              <p class="text-xs font-semibold text-gray-400 uppercase tracking-wide mb-3">Contact d'urgence</p>
              <div class="grid grid-cols-2 gap-4">
                @if (member()!.emergency_contact_name) {
                  <div>
                    <p class="text-xs text-gray-400 mb-1">Nom</p>
                    <p class="text-sm text-gray-800 dark:text-white">{{ member()!.emergency_contact_name }}</p>
                  </div>
                }
                @if (member()!.emergency_contact_phone) {
                  <div>
                    <p class="text-xs text-gray-400 mb-1">Téléphone</p>
                    <p class="text-sm text-gray-800 dark:text-white">{{ member()!.emergency_contact_phone }}</p>
                  </div>
                }
              </div>
            </div>
          }

          <!-- Audit -->
          <div class="px-6 py-4 bg-gray-50 dark:bg-gray-900/30 rounded-b-xl">
            <p class="text-xs text-gray-400">
              Membre depuis le {{ member()!.created_at | date:'longDate' }}
              @if (member()!.updated_at) { · Mis à jour le {{ member()!.updated_at | date:'longDate' }} }
            </p>
          </div>
        </div>

        <!-- Quick links -->
        <div class="grid grid-cols-2 gap-3">
          <a routerLink="/contributions"
            class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl p-4 flex items-center gap-3 hover:shadow-sm transition-shadow">
            <div class="w-9 h-9 rounded-lg bg-emerald-100 flex items-center justify-center">
              <i class="pi pi-wallet text-emerald-600"></i>
            </div>
            <span class="text-sm font-medium text-gray-700 dark:text-gray-200">Mes cotisations</span>
          </a>
          <a routerLink="/payments"
            class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl p-4 flex items-center gap-3 hover:shadow-sm transition-shadow">
            <div class="w-9 h-9 rounded-lg bg-blue-100 flex items-center justify-center">
              <i class="pi pi-credit-card text-blue-600"></i>
            </div>
            <span class="text-sm font-medium text-gray-700 dark:text-gray-200">Mes paiements</span>
          </a>
        </div>
      }
    </div>
  `,
})
export class MyMembershipPage implements OnInit {
  private readonly api = inject(MembersApiService);

  readonly loading = signal(true);
  readonly member  = signal<Member | null>(null);
  readonly error   = signal(false);

  ngOnInit(): void {
    this.api.me().subscribe({
      next:  m => { this.member.set(m); this.loading.set(false); },
      error: () => { this.error.set(true); this.loading.set(false); },
    });
  }

  statusClass(status: string): string {
    const map: Record<string, string> = {
      active:    'bg-emerald-100 text-emerald-700',
      suspended: 'bg-orange-100 text-orange-700',
      inactive:  'bg-gray-100 text-gray-500',
      pending:   'bg-yellow-100 text-yellow-700',
    };
    return map[status] ?? 'bg-gray-100 text-gray-500';
  }

  isExpiringSoon(): boolean {
    const d = this.member()?.expiry_date;
    if (!d) return false;
    const diff = (new Date(d).getTime() - Date.now()) / (1000 * 60 * 60 * 24);
    return diff >= 0 && diff <= 30;
  }
}
