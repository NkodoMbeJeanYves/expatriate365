import {
  ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit,
  computed, inject, signal, viewChild,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { TabsModule } from 'primeng/tabs';
import { Drawer, DrawerModule } from 'primeng/drawer';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import {
  BoardRoleDto, BoardMemberDto, ResolutionDto,
} from '@models/governance.model';
import { GovernanceApiService } from '../../services/governance-api.service';
import { MembersApiService } from '@members/services/members-api.service';
import { MemberListItem } from '@models/member.model';
import { PagedResult } from '@shared/models/pagination.model';

@Component({
  selector: 'app-governance-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ConfirmationService],
  imports: [
    FormsModule, ReactiveFormsModule, ButtonModule, TagModule,
    TabsModule, DrawerModule, InputTextModule, SelectModule, TextareaModule,
    ProgressSpinnerModule, ConfirmDialog, TranslatePipe,
  ],
  template: `
    <p-confirmdialog />

    <div class="p-6 flex flex-col gap-6">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-gray-800">{{ 'governance.title' | translate }}</h1>
          <p class="text-gray-500 text-sm">{{ 'governance.subtitle' | translate }}</p>
        </div>
      </div>

      <!-- Stats -->
      @if (stats()) {
        <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div class="bg-white rounded-xl p-4 border border-gray-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-indigo-700">{{ stats()!.total_board_members }}</div>
            <div class="text-xs text-gray-500 mt-1">{{ 'governance.board_member' | translate }}</div>
          </div>
          <div class="bg-white rounded-xl p-4 border border-gray-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-gray-700">{{ stats()!.total_resolutions }}</div>
            <div class="text-xs text-gray-500 mt-1">{{ 'governance.resolutions' | translate }}</div>
          </div>
          <div class="bg-green-50 rounded-xl p-4 border border-green-100 shadow-sm text-center">
            <div class="text-2xl font-bold text-green-700">{{ stats()!.adopted_resolutions }}</div>
            <div class="text-xs text-green-600 mt-1">{{ 'governance.status_adopted' | translate }}</div>
          </div>
        </div>
      }

      <p-tabs value="board">
        <p-tablist>
          <p-tab value="board">{{ 'governance.board' | translate }}</p-tab>
          <p-tab value="resolutions">{{ 'governance.resolutions' | translate }}</p-tab>
        </p-tablist>

        <!-- BUREAU -->
        <p-tabpanels>
          <p-tabpanel value="board">
            <div class="flex items-center justify-between mb-4">
              <p-button
                [label]="'governance.manage_roles' | translate"
                icon="pi pi-cog"
                severity="secondary"
                [text]="true"
                size="small"
                (onClick)="openBoardRolesList()" />
              <p-button [label]="'governance.new_board_member' | translate" icon="pi pi-plus" (onClick)="openBoardForm()" />
            </div>

            @if (boardLoading()) {
              <div class="flex justify-center py-8"><p-progressspinner strokeWidth="4" /></div>
            } @else if (board().length === 0) {
              <div class="text-center py-12 text-gray-400">
                <i class="pi pi-users text-4xl mb-3 block"></i>
                <p>{{ 'governance.no_board_members' | translate }}</p>
              </div>
            } @else {
              <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
                @for (bm of board(); track bm.id) {
                  <div class="bg-white rounded-xl border border-gray-100 shadow-sm p-4 flex items-center gap-4">
                    <div class="w-10 h-10 rounded-full bg-indigo-100 text-indigo-700 flex items-center justify-center font-bold text-sm shrink-0">
                      {{ initials(bm.member_name) }}
                    </div>
                    <div class="flex-1 min-w-0">
                      <p class="font-semibold text-gray-800 text-sm truncate">{{ bm.member_name }}</p>
                      <p class="text-xs text-indigo-600 font-medium">
                        {{ bm.role_name ? (('governance.role_' + bm.role_name) | translate) : (bm.role_label || '') }}
                      </p>
                      <p class="text-xs text-gray-400">{{ bm.membership_number }}</p>
                    </div>
                    <p-button size="small" icon="pi pi-trash" severity="danger"
                      (onClick)="confirmRemoveBoard(bm)" />
                  </div>
                }
              </div>
            }
          </p-tabpanel>

          <!-- RESOLUTIONS -->
          <p-tabpanel value="resolutions">
            <div class="flex items-center gap-3 mb-4">
              <p-select [options]="resStatusOptions" [(ngModel)]="filterResStatus"
                optionLabel="label" optionValue="value"
                [placeholder]="'common.all' | translate" [showClear]="true" (onChange)="loadResolutions()" />
              <div class="flex-1"></div>
              <p-button [label]="'governance.new_resolution' | translate" icon="pi pi-plus" (onClick)="showResForm.set(true)" />
            </div>

            @if (resLoading()) {
              <div class="flex justify-center py-8"><p-progressspinner strokeWidth="4" /></div>
            } @else if (resolutions().length === 0) {
              <div class="text-center py-12 text-gray-400">
                <i class="pi pi-file-edit text-4xl mb-3 block"></i>
                <p>{{ 'governance.no_resolutions' | translate }}</p>
              </div>
            } @else {
              <div class="flex flex-col gap-4">
                @for (res of resolutions(); track res.id) {
                  <div class="bg-white rounded-xl border border-gray-100 shadow-sm p-4 flex flex-col gap-3">
                    <div class="flex items-start justify-between gap-2">
                      <h3 class="font-semibold text-gray-800">{{ res.title }}</h3>
                      <p-tag [value]="res.status"
                        [severity]="res.status === 'adopted' ? 'success' : 'secondary'" class="shrink-0" />
                    </div>
                    <p class="text-sm text-gray-600 line-clamp-3">{{ res.content }}</p>

                    @if (res.status === 'adopted') {
                      <div class="text-xs text-gray-500 flex gap-4">
                        <span>{{ 'governance.votes_for' | translate }} : <strong class="text-green-600">{{ res.votes_for }}</strong></span>
                        <span>{{ 'governance.votes_against' | translate }} : <strong class="text-red-500">{{ res.votes_against }}</strong></span>
                        <span>{{ 'governance.abstentions' | translate }} : {{ res.abstentions }}</span>
                      </div>
                    }

                    <div class="flex gap-2 pt-1 border-t border-gray-50">
                      @if (res.status === 'draft') {
                        <p-button size="small" [label]="'governance.adopt' | translate" icon="pi pi-check" severity="success"
                          (onClick)="openAdopt(res)" />
                      }
                      <p-button size="small" icon="pi pi-trash" severity="danger"
                        (onClick)="confirmDeleteRes(res)" />
                    </div>
                  </div>
                }
              </div>
            }
          </p-tabpanel>
        </p-tabpanels>
      </p-tabs>
    </div>

    <!-- Drawer: ajout membre bureau -->
    <p-drawer #boardDrawerEl [visible]="showBoardForm()" [header]="'governance.new_board_member' | translate"
      position="right" styleClass="!w-full md:!w-[480px]"
      (visibleChange)="showBoardForm.set($event)">
      <form [formGroup]="boardForm" (ngSubmit)="submitBoard()" class="flex flex-col gap-4 p-2">
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'common.member' | translate }} *</label>
          <p-select [options]="members()" optionLabel="label" optionValue="value"
            formControlName="member_id"
            [filter]="true" filterBy="label"
            [placeholder]="'Rechercher un membre…'" />
        </div>
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'governance.role' | translate }}</label>
          <p-select formControlName="role_id" [options]="boardRoleOptions()"
            optionLabel="label" optionValue="value"
            [placeholder]="'governance.role_placeholder' | translate"
            [showClear]="true" />
        </div>
        <div class="grid grid-cols-2 gap-4">
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'governance.start_date' | translate }}</label>
            <input pInputText type="date" formControlName="start_date" />
          </div>
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'governance.end_date' | translate }}</label>
            <input pInputText type="date" formControlName="end_date" />
          </div>
        </div>
        @if (boardError()) { <p class="text-red-500 text-sm">{{ boardError() }}</p> }
        <div class="flex justify-end gap-2">
          <p-button type="button" severity="secondary" [label]="'common.cancel' | translate" (onClick)="boardDrawerRef()?.close($event)" />
          <p-button type="submit" [label]="'common.add' | translate" [loading]="boardSaving()" [disabled]="boardForm.invalid" />
        </div>
      </form>
    </p-drawer>

    <!-- Drawer: gestion des rôles du bureau -->
    <p-drawer #boardRolesDrawerEl [visible]="showBoardRolesList()" [header]="'governance.manage_roles' | translate"
      position="right" styleClass="!w-full md:!w-[480px]"
      (visibleChange)="showBoardRolesList.set($event)">
      <div class="flex flex-col gap-4 p-2">
        <div class="flex justify-end">
          <p-button [label]="'governance.new_role' | translate" icon="pi pi-plus" size="small"
            (onClick)="openBoardRoleForm(null)" />
        </div>

        @if (boardRolesLoading()) {
          <div class="flex justify-center py-8"><p-progressspinner strokeWidth="4" /></div>
        } @else if (boardRoles().length === 0) {
          <div class="text-center py-12 text-gray-400">
            <i class="pi pi-tag text-4xl mb-3 block"></i>
            <p>{{ 'governance.no_roles' | translate }}</p>
          </div>
        } @else {
          <div class="flex flex-col gap-2">
            @for (br of boardRoles(); track br.id) {
              <div class="flex items-center justify-between bg-gray-50 rounded-lg px-4 py-3 border border-gray-100">
                <div>
                  <p class="text-sm font-medium text-gray-800">{{ br.label }}</p>
                  <p class="text-xs text-gray-400">{{ br.name }}</p>
                </div>
                <div class="flex gap-1">
                  <p-button size="small" icon="pi pi-pencil" severity="secondary" [text]="true"
                    (onClick)="openBoardRoleForm(br)" />
                  <p-button size="small" icon="pi pi-trash" severity="danger" [text]="true"
                    (onClick)="confirmDeleteBoardRole(br)" />
                </div>
              </div>
            }
          </div>
        }
      </div>
    </p-drawer>

    <!-- Drawer: créer / modifier un rôle du bureau -->
    <p-drawer #boardRoleFormDrawerEl [visible]="showBoardRoleForm()"
      [header]="editingBoardRole() ? ('governance.edit_role' | translate) : ('governance.new_role' | translate)"
      position="right" styleClass="!w-full md:!w-[420px]"
      (visibleChange)="showBoardRoleForm.set($event)">
      <form [formGroup]="boardRoleForm" (ngSubmit)="submitBoardRole()" class="flex flex-col gap-4 p-2">
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'governance.board_role_name' | translate }} *</label>
          <input pInputText formControlName="name"
            [placeholder]="'governance.board_role_name_hint' | translate" />
          <p class="text-xs text-gray-400">{{ 'governance.board_role_name_hint' | translate }}</p>
        </div>
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'governance.board_role_label' | translate }} *</label>
          <input pInputText formControlName="label"
            [placeholder]="'governance.board_role_label_hint' | translate" />
        </div>
        @if (boardRoleError()) { <p class="text-red-500 text-sm">{{ boardRoleError() }}</p> }
        <div class="flex justify-end gap-2">
          <p-button type="button" severity="secondary" [label]="'common.cancel' | translate"
            (onClick)="boardRoleFormDrawerRef()?.close($event)" />
          <p-button type="submit" [label]="'common.save' | translate"
            [loading]="boardRoleSaving()" [disabled]="boardRoleForm.invalid" />
        </div>
      </form>
    </p-drawer>

    <!-- Drawer: nouvelle résolution -->
    <p-drawer #resDrawerEl [visible]="showResForm()" [header]="'governance.new_resolution' | translate"
      position="right" styleClass="!w-full md:!w-[520px]"
      (visibleChange)="showResForm.set($event)">
      <form [formGroup]="resForm" (ngSubmit)="submitRes()" class="flex flex-col gap-4 p-2">
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'common.title' | translate }}</label>
          <input pInputText formControlName="title" />
        </div>
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'governance.content' | translate }}</label>
          <textarea pTextarea formControlName="content" rows="6" class="w-full"></textarea>
        </div>
        @if (resError()) { <p class="text-red-500 text-sm">{{ resError() }}</p> }
        <div class="flex justify-end gap-2">
          <p-button type="button" severity="secondary" [label]="'common.cancel' | translate" (onClick)="resDrawerRef()?.close($event)" />
          <p-button type="submit" [label]="'common.save' | translate" [loading]="resSaving()" [disabled]="resForm.invalid" />
        </div>
      </form>
    </p-drawer>

    <!-- Drawer: adopter résolution -->
    <p-drawer #adoptDrawerEl [visible]="showAdoptForm()" [header]="'governance.adopt' | translate"
      position="right" styleClass="!w-full md:!w-[420px]"
      (visibleChange)="showAdoptForm.set($event)">
      <form [formGroup]="adoptForm" (ngSubmit)="submitAdopt()" class="flex flex-col gap-4 p-2">
        <div class="flex flex-col gap-1">
          <label class="text-sm font-medium">{{ 'governance.adopted_at' | translate }}</label>
          <input pInputText type="date" formControlName="adopted_at" />
        </div>
        <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium text-green-700">{{ 'governance.votes_for' | translate }}</label>
            <input pInputText type="number" formControlName="votes_for" min="0" />
          </div>
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium text-red-600">{{ 'governance.votes_against' | translate }}</label>
            <input pInputText type="number" formControlName="votes_against" min="0" />
          </div>
          <div class="flex flex-col gap-1">
            <label class="text-sm font-medium">{{ 'governance.abstentions' | translate }}</label>
            <input pInputText type="number" formControlName="abstentions" min="0" />
          </div>
        </div>
        <div class="flex justify-end gap-2">
          <p-button type="button" severity="secondary" [label]="'common.cancel' | translate" (onClick)="adoptDrawerRef()?.close($event)" />
          <p-button type="submit" [label]="'common.confirm' | translate" [loading]="adoptSaving()" [disabled]="adoptForm.invalid" />
        </div>
      </form>
    </p-drawer>
  `,
})
export class GovernancePage implements OnInit {
  private readonly api = inject(GovernanceApiService);
  private readonly membersApi = inject(MembersApiService);
  private readonly confirm = inject(ConfirmationService);
  private readonly fb = inject(FormBuilder);
  private readonly translate = inject(TranslateService);
  private readonly cdr = inject(ChangeDetectorRef);

  protected readonly boardDrawerRef       = viewChild<Drawer>('boardDrawerEl');
  protected readonly boardRolesDrawerRef  = viewChild<Drawer>('boardRolesDrawerEl');
  protected readonly boardRoleFormDrawerRef = viewChild<Drawer>('boardRoleFormDrawerEl');
  protected readonly resDrawerRef         = viewChild<Drawer>('resDrawerEl');
  protected readonly adoptDrawerRef       = viewChild<Drawer>('adoptDrawerEl');

  readonly stats = signal<any>(null);
  readonly board = signal<BoardMemberDto[]>([]);
  readonly boardLoading = signal(false);
  readonly boardRoles = signal<BoardRoleDto[]>([]);
  readonly boardRolesLoading = signal(false);
  readonly resolutions = signal<ResolutionDto[]>([]);
  readonly resLoading = signal(false);
  readonly members = signal<{ label: string; value: string }[]>([]);

  // Board member form
  readonly showBoardForm = signal(false);
  readonly boardSaving = signal(false);
  readonly boardError = signal<string | null>(null);

  // Board roles list panel
  readonly showBoardRolesList = signal(false);

  // Board role create/edit form
  readonly showBoardRoleForm = signal(false);
  readonly boardRoleSaving = signal(false);
  readonly boardRoleError = signal<string | null>(null);
  readonly editingBoardRole = signal<BoardRoleDto | null>(null);

  // Resolution forms
  readonly showResForm = signal(false);
  readonly resSaving = signal(false);
  readonly resError = signal<string | null>(null);

  readonly showAdoptForm = signal(false);
  readonly adoptSaving = signal(false);
  private adoptingId = '';

  filterResStatus: string | null = null;

  get resStatusOptions() {
    return [
      { label: this.translate.instant('governance.status_draft'), value: 'draft' },
      { label: this.translate.instant('governance.status_adopted'), value: 'adopted' },
    ];
  }

  readonly boardRoleOptions = computed(() =>
    this.boardRoles()
      .filter(r => r.is_active)
      .map(r => ({
        label: this.translate.instant('governance.role_' + r.name, { defaultValue: r.label }) || r.label,
        value: r.id,
      }))
  );

  readonly boardForm = this.fb.group({
    member_id: ['', Validators.required],
    role_id:   [null as string | null],
    start_date: ['', Validators.required],
    end_date: [''],
  });

  readonly boardRoleForm = this.fb.group({
    name:  ['', [Validators.required, Validators.pattern(/^[a-z_]+$/)]],
    label: ['', Validators.required],
  });

  readonly resForm = this.fb.group({
    title:   ['', Validators.required],
    content: ['', Validators.required],
  });

  readonly adoptForm = this.fb.group({
    adopted_at:    ['', Validators.required],
    votes_for:     [0, [Validators.required, Validators.min(0)]],
    votes_against: [0, [Validators.required, Validators.min(0)]],
    abstentions:   [0, [Validators.required, Validators.min(0)]],
  });

  ngOnInit(): void {
    this.loadStats();
    this.loadBoard();
    this.loadResolutions();
    this.loadMembers();
    this.loadBoardRoles();
  }

  loadMembers(): void {
    this.membersApi.list({ page: 1, limit: 500, status: 'active' }).subscribe({
      next: (res: PagedResult<MemberListItem>) => this.members.set(res.data.map((m: MemberListItem) => ({
        label: `${m.first_name} ${m.last_name} (${m.membership_number})`,
        value: m.id,
      }))),
    });
  }

  loadStats(): void {
    this.api.stats().subscribe(s => this.stats.set(s));
  }

  loadBoard(): void {
    this.boardLoading.set(true);
    this.api.listBoard().subscribe({
      next: b => { this.board.set(b); this.boardLoading.set(false); },
      error: () => this.boardLoading.set(false),
    });
  }

  loadBoardRoles(): void {
    this.boardRolesLoading.set(true);
    this.api.listBoardRoles().subscribe({
      next: roles => { this.boardRoles.set(roles); this.boardRolesLoading.set(false); },
      error: () => this.boardRolesLoading.set(false),
    });
  }

  loadResolutions(): void {
    this.resLoading.set(true);
    this.api.listResolutions(1, 50, this.filterResStatus ?? undefined).subscribe({
      next: r => { this.resolutions.set(r.data); this.resLoading.set(false); },
      error: () => this.resLoading.set(false),
    });
  }

  // ── Board member ──────────────────────────────────────────────
  openBoardForm(): void {
    this.boardForm.reset();
    this.boardError.set(null);
    this.showBoardForm.set(true);
  }

  submitBoard(): void {
    if (this.boardForm.invalid) return;
    this.boardSaving.set(true);
    this.boardError.set(null);
    const v = this.boardForm.value;
    this.api.addBoardMember({
      member_id: v.member_id!,
      role_id:   v.role_id ?? undefined,
      start_date: v.start_date!,
      end_date: v.end_date || undefined,
    }).subscribe({
      next: bm => {
        this.board.update(b => [bm, ...b]);
        this.boardSaving.set(false);
        this.boardDrawerRef()?.close(new MouseEvent('click'));
        this.boardForm.reset();
        this.loadStats();
      },
      error: () => { this.boardError.set('Erreur.'); this.boardSaving.set(false); },
    });
  }

  confirmRemoveBoard(bm: BoardMemberDto): void {
    this.confirm.confirm({
      message: `Retirer ${bm.member_name} du bureau ?`,
      header: 'Confirmation',
      icon: 'pi pi-trash',
      accept: () => {
        this.api.removeBoardMember(bm.id).subscribe(() => {
          this.board.update(b => b.filter(x => x.id !== bm.id));
          this.loadStats();
        });
      },
    });
  }

  // ── Board roles ──────────────────────────────────────────────
  openBoardRolesList(): void {
    this.showBoardRolesList.set(true);
  }

  openBoardRoleForm(role: BoardRoleDto | null): void {
    this.editingBoardRole.set(role);
    this.boardRoleError.set(null);
    if (role) {
      this.boardRoleForm.reset({ name: role.name, label: role.label });
    } else {
      this.boardRoleForm.reset();
    }
    this.showBoardRoleForm.set(true);
  }

  submitBoardRole(): void {
    if (this.boardRoleForm.invalid) return;
    this.boardRoleSaving.set(true);
    this.boardRoleError.set(null);
    const v = this.boardRoleForm.value;
    const editing = this.editingBoardRole();

    const obs = editing
      ? this.api.updateBoardRole(editing.id, { name: v.name!, label: v.label!, is_active: editing.is_active })
      : this.api.createBoardRole({ name: v.name!, label: v.label! });

    obs.subscribe({
      next: (saved: BoardRoleDto) => {
        if (editing) {
          this.boardRoles.update(list => list.map(r => r.id === saved.id ? saved : r));
        } else {
          this.boardRoles.update(list => [...list, saved]);
        }
        this.boardRoleSaving.set(false);
        this.boardRoleFormDrawerRef()?.close(new MouseEvent('click'));
      },
      error: (err: any) => {
        this.boardRoleError.set(err?.error?.error ?? 'Erreur.');
        this.boardRoleSaving.set(false);
      },
    });
  }

  confirmDeleteBoardRole(role: BoardRoleDto): void {
    this.confirm.confirm({
      message: `Supprimer le rôle "${role.label}" ?`,
      header: 'Confirmation',
      icon: 'pi pi-trash',
      accept: () => {
        this.api.deleteBoardRole(role.id).subscribe({
          next: () => this.boardRoles.update(list => list.filter(r => r.id !== role.id)),
          error: (err: any) => {
            this.translate.get('common.error').subscribe(msg =>
              this.confirm.confirm({ message: err?.error?.error ?? msg, header: 'Erreur', icon: 'pi pi-times' })
            );
          },
        });
      },
    });
  }

  // ── Resolutions ───────────────────────────────────────────────
  submitRes(): void {
    if (this.resForm.invalid) return;
    this.resSaving.set(true);
    this.resError.set(null);
    const v = this.resForm.value;
    this.api.createResolution({ title: v.title!, content: v.content! }).subscribe({
      next: r => {
        this.resolutions.update(list => [r, ...list]);
        this.resSaving.set(false);
        this.resDrawerRef()?.close(new MouseEvent('click'));
        this.resForm.reset();
        this.loadStats();
      },
      error: () => { this.resError.set('Erreur.'); this.resSaving.set(false); },
    });
  }

  openAdopt(res: ResolutionDto): void {
    this.adoptingId = res.id;
    this.adoptForm.reset({ votes_for: 0, votes_against: 0, abstentions: 0 });
    this.showAdoptForm.set(true);
  }

  submitAdopt(): void {
    if (this.adoptForm.invalid) return;
    this.adoptSaving.set(true);
    const v = this.adoptForm.value;
    this.api.adoptResolution(this.adoptingId, {
      adopted_at: v.adopted_at!,
      votes_for: v.votes_for ?? 0,
      votes_against: v.votes_against ?? 0,
      abstentions: v.abstentions ?? 0,
    }).subscribe({
      next: r => {
        this.resolutions.update(list => list.map(x => x.id === r.id ? r : x));
        this.adoptSaving.set(false);
        this.adoptDrawerRef()?.close(new MouseEvent('click'));
        this.loadStats();
      },
      error: () => this.adoptSaving.set(false),
    });
  }

  confirmDeleteRes(res: ResolutionDto): void {
    this.confirm.confirm({
      message: `Supprimer la résolution "${res.title}" ?`,
      header: 'Confirmation',
      icon: 'pi pi-trash',
      accept: () => {
        this.api.deleteResolution(res.id).subscribe(() => {
          this.resolutions.update(list => list.filter(x => x.id !== res.id));
          this.loadStats();
        });
      },
    });
  }

  initials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }
}
