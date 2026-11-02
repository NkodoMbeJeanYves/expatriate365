import { ChangeDetectionStrategy, Component, ElementRef, OnDestroy, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { applyEach, form, FormField, maxLength, min, required, submit } from '@angular/forms/signals';
import { firstValueFrom } from 'rxjs';
import { TranslatePipe } from '@ngx-translate/core';
import { TenantStore } from '@core/tenant/tenant.store';
import { CommunityShowcase, SaveShowcaseRequest, ShowcaseOfferRequest, ShowcasesApiService } from '../services/showcases-api.service';
import { calculateImageCrop } from '../utils/image-crop.utils';

interface OfferDraft {
  name: string;
  description: string;
  price: number;
  price_on_request: boolean;
  details: string;
}

interface ShowcaseDraft {
  display_name: string;
  title: string;
  description: string;
  category: string;
  service_area: string;
  contact_phone: string;
  contact_link: string;
  photo_url: string;
  offers: OfferDraft[];
}

@Component({
  selector: 'app-showcase-manage-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormField, TranslatePipe],
  template: `
    <div class="mx-auto max-w-4xl space-y-6">
      <header>
        <h1 class="text-2xl font-bold text-gray-900 dark:text-white">{{ 'showcases.manage_title' | translate }}</h1>
        <p class="mt-1 text-sm text-gray-500">{{ 'showcases.manage_intro' | translate }}</p>
      </header>

      @if (loading()) {
        <p class="py-10 text-center text-gray-500">{{ 'common.loading' | translate }}</p>
      } @else {
        @if (error()) {
          <p class="rounded-lg bg-red-50 p-4 text-red-700">{{ 'showcases.save_error' | translate }}</p>
        }
        @if (showcase()?.status === 'published') {
          <div class="flex flex-wrap items-center justify-between gap-3 rounded-xl bg-emerald-50 p-4 text-emerald-900">
            <span>{{ 'showcases.published' | translate }}</span>
            <a [href]="publicUrl()" target="_blank" rel="noopener noreferrer" class="font-medium underline">{{ 'showcases.preview_public' | translate }}</a>
          </div>
        } @else if (showcase()?.status === 'suspended') {
          <p class="rounded-xl bg-amber-50 p-4 text-amber-900">{{ 'showcases.suspended' | translate }}</p>
        }

        <section class="rounded-2xl border border-emerald-200 bg-emerald-50/70 p-5 dark:border-emerald-900 dark:bg-emerald-950/30">
          <p class="text-sm font-semibold text-emerald-900 dark:text-emerald-200">{{ 'showcases.preview_title' | translate }}</p>
          <h2 class="mt-2 text-xl font-bold text-gray-900 dark:text-white">{{ showcaseModel().title || ('showcases.title_field' | translate) }}</h2>
          <p class="mt-1 text-sm text-gray-500">{{ showcaseModel().display_name || ('showcases.display_name' | translate) }} · {{ showcaseModel().category || ('showcases.category_field' | translate) }}</p>
          @if (showcaseModel().photo_url) {
            <img [src]="showcaseModel().photo_url" [alt]="showcaseModel().title" class="mt-4 aspect-video w-full rounded-xl object-cover" />
          }
          <p class="mt-4 whitespace-pre-line text-gray-700 dark:text-gray-300">{{ showcaseModel().description || ('showcases.description_field' | translate) }}</p>
          @if (showcaseModel().service_area) {
            <p class="mt-3 text-sm text-gray-500">{{ 'showcases.service_area' | translate }}: {{ showcaseModel().service_area }}</p>
          }
          <div class="mt-4 grid gap-3 sm:grid-cols-2">
            @for (offer of showcaseModel().offers; track $index; let i = $index) {
              @if (offer.name || offer.description) {
                <article class="rounded-xl bg-white p-4 dark:bg-gray-900">
                  <h3 class="font-semibold">{{ offer.name || ('showcases.offer_number' | translate : { number: i + 1 }) }}</h3>
                  <p class="mt-1 text-sm text-gray-600 dark:text-gray-300">{{ offer.description }}</p>
                  <p class="mt-2 text-sm text-emerald-700">
                    {{ offer.price_on_request ? ('showcases.price_on_request' | translate) : offer.price + ' ' + tenantStore.symbol() }}
                  </p>
                  @if (offer.details) {
                    <p class="mt-1 text-xs text-gray-500">{{ offer.details }}</p>
                  }
                </article>
              }
            }
          </div>
          <p class="mt-4 text-xs text-gray-600 dark:text-gray-400">{{ 'showcases.preview_privacy' | translate }}</p>
        </section>

        <section class="space-y-4 rounded-2xl border border-gray-200 bg-white p-5 dark:border-gray-800 dark:bg-gray-900">
          <h2 class="font-semibold text-gray-900 dark:text-white">{{ 'showcases.profile_section' | translate }}</h2>
          <label class="block text-sm">{{ 'showcases.display_name' | translate }}
            <input class="mt-1 w-full rounded-lg border p-3 dark:bg-gray-950" [formField]="showcaseForm.display_name" />
          </label>
          <label class="block text-sm">{{ 'showcases.title_field' | translate }}
            <input class="mt-1 w-full rounded-lg border p-3 dark:bg-gray-950" [formField]="showcaseForm.title" />
          </label>
          <label class="block text-sm">{{ 'showcases.description_field' | translate }}
            <textarea class="mt-1 w-full rounded-lg border p-3 dark:bg-gray-950" [formField]="showcaseForm.description" rows="5"></textarea>
          </label>
          <div class="grid gap-4 sm:grid-cols-2">
            <label class="block text-sm">{{ 'showcases.category_field' | translate }}
              <input class="mt-1 w-full rounded-lg border p-3 dark:bg-gray-950" [formField]="showcaseForm.category" [placeholder]="'showcases.category_hint' | translate" />
            </label>
            <label class="block text-sm">{{ 'showcases.service_area_field' | translate }}
              <input class="mt-1 w-full rounded-lg border p-3 dark:bg-gray-950" [formField]="showcaseForm.service_area" />
            </label>
          </div>
          <label class="block text-sm">{{ 'showcases.photo_field' | translate }}
            <input class="mt-1 block w-full text-sm" type="file" accept="image/jpeg,image/png,image/webp,image/gif" (change)="uploadPhoto($event)" />
          </label>
          @if (cropSourceUrl()) {
            <div class="space-y-4 rounded-xl border border-emerald-200 p-4 dark:border-emerald-900">
              <div>
                <h3 class="font-medium">{{ 'showcases.crop_title' | translate }}</h3>
                <p class="mt-1 text-sm text-gray-500">{{ 'showcases.crop_instructions' | translate }}</p>
              </div>
              <canvas #cropCanvas class="block aspect-video w-full rounded-lg bg-gray-100" [attr.aria-label]="'showcases.crop_preview' | translate"></canvas>
              <label class="block text-sm">{{ 'showcases.crop_horizontal' | translate }}
                <input class="mt-2 block w-full" type="range" min="0" max="100" [value]="cropFocusX()" (input)="updateCropFocusX($event)" />
              </label>
              <label class="block text-sm">{{ 'showcases.crop_vertical' | translate }}
                <input class="mt-2 block w-full" type="range" min="0" max="100" [value]="cropFocusY()" (input)="updateCropFocusY($event)" />
              </label>
              <label class="block text-sm">{{ 'showcases.crop_zoom' | translate }}
                <input class="mt-2 block w-full" type="range" min="1" max="3" step="0.1" [value]="cropZoom()" (input)="updateCropZoom($event)" />
              </label>
              <div class="flex flex-wrap gap-3">
                <button type="button" class="rounded-lg bg-emerald-700 px-4 py-2 font-medium text-white disabled:opacity-50" [disabled]="uploading()" (click)="confirmCrop()">
                  {{ 'showcases.crop_apply' | translate }}
                </button>
                <button type="button" class="rounded-lg border px-4 py-2" [disabled]="uploading()" (click)="cancelCrop()">
                  {{ 'common.cancel' | translate }}
                </button>
              </div>
            </div>
          }
          @if (showcaseModel().photo_url) {
            <div class="flex items-center gap-3">
              <img [src]="showcaseModel().photo_url" [alt]="showcaseModel().title || ('showcases.photo_field' | translate)" class="aspect-video w-36 rounded-lg object-cover" />
              <button type="button" class="text-sm text-red-700" (click)="removePhoto()">{{ 'showcases.remove_photo' | translate }}</button>
            </div>
          }
          @if (uploading()) {
            <p class="text-sm text-gray-500">{{ 'common.uploading' | translate }}</p>
          }
          @if (uploadError()) {
            <p class="text-sm text-red-600">{{ 'errors.upload_failed' | translate }}</p>
          }
        </section>

        <section class="space-y-4 rounded-2xl border border-gray-200 bg-white p-5 dark:border-gray-800 dark:bg-gray-900">
          <div class="flex items-center justify-between gap-3">
            <h2 class="font-semibold text-gray-900 dark:text-white">{{ 'showcases.offers' | translate }}</h2>
            <button type="button" class="rounded-lg border px-3 py-2 text-sm" (click)="addOffer()">{{ 'showcases.add_offer' | translate }}</button>
          </div>
          @for (offer of showcaseForm.offers; track $index; let i = $index) {
            <div class="space-y-3 rounded-xl border border-gray-200 p-4 dark:border-gray-700">
              <div class="flex justify-between gap-3">
                <h3 class="font-medium">{{ 'showcases.offer_number' | translate : { number: i + 1 } }}</h3>
                <button type="button" class="text-sm text-red-700" (click)="removeOffer(i)">{{ 'common.delete' | translate }}</button>
              </div>
              <input class="w-full rounded-lg border p-3 dark:bg-gray-950" [formField]="offer.name" [placeholder]="'showcases.offer_name' | translate" />
              <textarea class="w-full rounded-lg border p-3 dark:bg-gray-950" [formField]="offer.description" rows="3" [placeholder]="'showcases.offer_description' | translate"></textarea>
              <label class="flex items-center gap-2 text-sm">
                <input type="checkbox" [formField]="offer.price_on_request" />
                {{ 'showcases.price_on_request' | translate }}
              </label>
              @if (!offer.price_on_request().value()) {
                <input class="w-full rounded-lg border p-3 dark:bg-gray-950" type="number" step="0.01" [formField]="offer.price" [placeholder]="'showcases.price_field' | translate" />
              }
              <input class="w-full rounded-lg border p-3 dark:bg-gray-950" [formField]="offer.details" [placeholder]="'showcases.details_field' | translate" />
            </div>
          }
        </section>

        <section class="space-y-4 rounded-2xl border border-gray-200 bg-white p-5 dark:border-gray-800 dark:bg-gray-900">
          <h2 class="font-semibold text-gray-900 dark:text-white">{{ 'showcases.contact_options' | translate }}</h2>
          <p class="text-sm text-gray-500">{{ 'showcases.contact_options_hint' | translate }}</p>
          <div class="grid gap-4 sm:grid-cols-2">
            <label class="block text-sm">{{ 'showcases.phone_field' | translate }}
              <input class="mt-1 w-full rounded-lg border p-3 dark:bg-gray-950" [formField]="showcaseForm.contact_phone" />
            </label>
            <label class="block text-sm">{{ 'showcases.link_field' | translate }}
              <input class="mt-1 w-full rounded-lg border p-3 dark:bg-gray-950" type="url" [formField]="showcaseForm.contact_link" placeholder="https://" />
            </label>
          </div>
        </section>

        @if (success()) {
          <p class="rounded-lg bg-emerald-50 p-3 text-emerald-800">{{ 'showcases.saved' | translate }}</p>
        }
        <div class="flex flex-wrap gap-3">
          <button type="button" class="rounded-lg bg-gray-800 px-5 py-3 font-medium text-white disabled:opacity-50" [disabled]="saving() || uploading() || cropSourceUrl() !== null" (click)="save()">{{ 'showcases.save_draft' | translate }}</button>
          <button type="button" class="rounded-lg bg-emerald-700 px-5 py-3 font-medium text-white disabled:opacity-50" [disabled]="saving() || uploading() || cropSourceUrl() !== null" (click)="saveAndPublish()">{{ 'showcases.publish' | translate }}</button>
          @if (showcase()?.status === 'published') {
            <button type="button" class="rounded-lg border px-5 py-3 font-medium" [disabled]="saving()" (click)="unpublish()">{{ 'showcases.unpublish' | translate }}</button>
          }
        </div>
      }
    </div>
  `,
})
export class ShowcaseManagePage implements OnInit, OnDestroy {
  private readonly api = inject(ShowcasesApiService);
  private cropImage: HTMLImageElement | null = null;
  private cropObjectUrl: string | null = null;
  private cropCanvasElement: HTMLCanvasElement | null = null;
  readonly tenantStore = inject(TenantStore);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly uploading = signal(false);
  readonly uploadError = signal(false);
  readonly cropSourceUrl = signal<string | null>(null);
  readonly cropFocusX = signal(50);
  readonly cropFocusY = signal(50);
  readonly cropZoom = signal(1);
  readonly error = signal(false);
  readonly success = signal(false);
  readonly showcase = signal<CommunityShowcase | null>(null);
  readonly showcaseModel = signal<ShowcaseDraft>({
    display_name: '',
    title: '',
    description: '',
    category: '',
    service_area: '',
    contact_phone: '',
    contact_link: '',
    photo_url: '',
    offers: [],
  });
  readonly showcaseForm = form(this.showcaseModel, (path) => {
    required(path.display_name);
    maxLength(path.display_name, 160);
    required(path.title);
    maxLength(path.title, 200);
    required(path.description);
    maxLength(path.description, 4000);
    required(path.category);
    maxLength(path.category, 100);
    maxLength(path.service_area, 200);
    maxLength(path.contact_phone, 40);
    maxLength(path.contact_link, 500);
    maxLength(path.photo_url, 1000);
    applyEach(path.offers, (offer) => {
      required(offer.name);
      maxLength(offer.name, 160);
      required(offer.description);
      maxLength(offer.description, 1000);
      min(offer.price, 0);
      maxLength(offer.details, 1000);
    });
  });
  readonly publicUrl = computed(() => {
    const showcase = this.showcase();
    return showcase
      ? `/showcases/${encodeURIComponent(showcase.tenant_slug)}/${encodeURIComponent(showcase.slug)}`
      : '/showcases';
  });

  @ViewChild('cropCanvas')
  set cropCanvas(canvas: ElementRef<HTMLCanvasElement> | undefined) {
    this.cropCanvasElement = canvas?.nativeElement ?? null;
    this.drawCropPreview();
  }

  ngOnInit(): void {
    this.api.getMine().subscribe({
      next: (showcase) => {
        if (showcase) this.applyShowcase(showcase);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }

  ngOnDestroy(): void {
    if (this.cropObjectUrl) URL.revokeObjectURL(this.cropObjectUrl);
  }

  addOffer(): void {
    this.showcaseModel.update((model) => model.offers.length < 20
      ? { ...model, offers: [...model.offers, this.emptyOffer()] }
      : model);
  }

  removeOffer(index: number): void {
    this.showcaseModel.update((model) => ({
      ...model,
      offers: model.offers.filter((_, i) => i !== index),
    }));
  }

  uploadPhoto(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    if (!['image/jpeg', 'image/png', 'image/webp', 'image/gif'].includes(file.type) ||
        file.size > 20 * 1024 * 1024) {
      this.uploadError.set(true);
      input.value = '';
      return;
    }

    this.clearCropSource();
    this.uploadError.set(false);
    const objectUrl = URL.createObjectURL(file);
    const image = new Image();
    image.onload = () => {
      this.cropObjectUrl = objectUrl;
      this.cropImage = image;
      this.cropFocusX.set(50);
      this.cropFocusY.set(50);
      this.cropZoom.set(1);
      this.cropSourceUrl.set(objectUrl);
    };
    image.onerror = () => {
      URL.revokeObjectURL(objectUrl);
      this.uploadError.set(true);
    };
    image.src = objectUrl;
    input.value = '';
  }

  updateCropFocusX(event: Event): void {
    this.cropFocusX.set(Number((event.target as HTMLInputElement).value));
    this.drawCropPreview();
  }

  updateCropFocusY(event: Event): void {
    this.cropFocusY.set(Number((event.target as HTMLInputElement).value));
    this.drawCropPreview();
  }

  updateCropZoom(event: Event): void {
    this.cropZoom.set(Number((event.target as HTMLInputElement).value));
    this.drawCropPreview();
  }

  cancelCrop(): void {
    this.clearCropSource();
    this.uploadError.set(false);
  }

  async confirmCrop(): Promise<void> {
    if (!this.cropImage || this.uploading()) return;

    this.uploading.set(true);
    this.uploadError.set(false);
    try {
      const croppedFile = await this.createCroppedPhoto(this.cropImage);
      const result = await firstValueFrom(this.api.uploadPhoto(croppedFile));
      this.showcaseModel.update((model) => ({ ...model, photo_url: result.file_url }));
      this.clearCropSource();
    } catch {
      this.uploadError.set(true);
    } finally {
      this.uploading.set(false);
    }
  }

  removePhoto(): void {
    this.cancelCrop();
    this.showcaseModel.update((model) => ({ ...model, photo_url: '' }));
  }

  private drawCropPreview(): void {
    const image = this.cropImage;
    const canvas = this.cropCanvasElement;
    const context = canvas?.getContext('2d');
    if (!image || !canvas || !context) return;

    canvas.width = 960;
    canvas.height = 540;
    const crop = calculateImageCrop(
      image.naturalWidth,
      image.naturalHeight,
      this.cropFocusX(),
      this.cropFocusY(),
      this.cropZoom(),
    );
    context.fillStyle = '#fff';
    context.fillRect(0, 0, canvas.width, canvas.height);
    context.drawImage(
      image,
      crop.x,
      crop.y,
      crop.width,
      crop.height,
      0,
      0,
      canvas.width,
      canvas.height,
    );
  }

  private async createCroppedPhoto(image: HTMLImageElement): Promise<File> {
    const crop = calculateImageCrop(
      image.naturalWidth,
      image.naturalHeight,
      this.cropFocusX(),
      this.cropFocusY(),
      this.cropZoom(),
    );
    const canvas = document.createElement('canvas');
    canvas.width = Math.max(1, Math.min(1600, Math.round(crop.width)));
    canvas.height = Math.max(1, Math.round(canvas.width * 9 / 16));
    const context = canvas.getContext('2d');
    if (!context) throw new Error('Unable to prepare the image crop.');

    context.fillStyle = '#fff';
    context.fillRect(0, 0, canvas.width, canvas.height);
    context.drawImage(
      image,
      crop.x,
      crop.y,
      crop.width,
      crop.height,
      0,
      0,
      canvas.width,
      canvas.height,
    );
    const blob = await new Promise<Blob>((resolve, reject) => {
      canvas.toBlob((result) => result ? resolve(result) : reject(new Error('Unable to encode the image crop.')), 'image/jpeg', 0.9);
    });
    return new File([blob], 'showcase-preview.jpg', { type: 'image/jpeg' });
  }

  private clearCropSource(): void {
    if (this.cropObjectUrl) URL.revokeObjectURL(this.cropObjectUrl);
    this.cropObjectUrl = null;
    this.cropImage = null;
    this.cropSourceUrl.set(null);
  }

  save(): void {
    this.submitSave(async () => {
      const showcase = await firstValueFrom(this.api.saveMine(this.toRequest()));
      this.applyShowcase(showcase);
    });
  }

  saveAndPublish(): void {
    this.submitSave(async () => {
      await firstValueFrom(this.api.saveMine(this.toRequest()));
      const showcase = await firstValueFrom(this.api.publishMine());
      this.applyShowcase(showcase);
    });
  }

  unpublish(): void {
    this.saving.set(true);
    this.error.set(false);
    this.api.unpublishMine().subscribe({
      next: (showcase) => {
        this.applyShowcase(showcase);
        this.saving.set(false);
      },
      error: () => {
        this.error.set(true);
        this.saving.set(false);
      },
    });
  }

  private submitSave(action: () => Promise<void>): void {
    this.error.set(false);
    this.success.set(false);
    submit(this.showcaseForm, async () => {
      this.saving.set(true);
      try {
        await action();
        this.success.set(true);
      } catch {
        this.error.set(true);
      } finally {
        this.saving.set(false);
      }
    });
    if (this.showcaseForm().invalid()) this.error.set(true);
  }

  private toRequest(): SaveShowcaseRequest {
    const model = this.showcaseModel();
    return {
      display_name: model.display_name,
      title: model.title,
      description: model.description,
      category: model.category,
      service_area: model.service_area,
      contact_phone: model.contact_phone,
      contact_link: model.contact_link,
      photo_url: model.photo_url,
      offers: model.offers.map((offer): ShowcaseOfferRequest => ({
        ...offer,
        price: offer.price_on_request ? null : offer.price,
      })),
    };
  }

  private applyShowcase(showcase: CommunityShowcase): void {
    this.showcase.set(showcase);
    this.showcaseModel.set({
      display_name: showcase.display_name,
      title: showcase.title,
      description: showcase.description,
      category: showcase.category,
      service_area: showcase.service_area ?? '',
      contact_phone: showcase.contact_phone ?? '',
      contact_link: showcase.contact_link ?? '',
      photo_url: showcase.photo_url ?? '',
      offers: showcase.offers.map((offer) => ({
        name: offer.name,
        description: offer.description,
        price: offer.price ?? 0,
        price_on_request: offer.price_on_request,
        details: offer.details ?? '',
      })),
    });
  }

  private emptyOffer(): OfferDraft {
    return { name: '', description: '', price: 0, price_on_request: true, details: '' };
  }
}
