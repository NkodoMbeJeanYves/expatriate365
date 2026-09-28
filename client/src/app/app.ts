import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { UploadProgressService } from './core/http/upload-progress.service';
import { RouterLoadingService } from './core/services/router-loading.service';

@Component({
  selector: 'app-root',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet],
  template: `
    @if (routerLoading.loading()) {
      <!-- Barre de progression -->
      <div class="route-loader-bar" aria-hidden="true">
        <div class="route-loader-bar__fill"></div>
      </div>

      <!-- Overlay bloquant -->
      <div class="route-loader-overlay" role="status" aria-label="Chargement en cours…">
        <div class="route-loader-spinner">
          <svg viewBox="0 0 50 50" xmlns="http://www.w3.org/2000/svg">
            <circle cx="25" cy="25" r="20" fill="none" stroke-width="4" />
          </svg>
          <span class="route-loader-label">Chargement…</span>
        </div>
      </div>
    }
    @if (uploadProgress.uploads().length > 0) {
      <section
        class="upload-progress-panel"
        aria-label="Téléversements en cours"
        aria-live="polite"
      >
        @for (upload of uploadProgress.uploads(); track upload.id) {
          <div class="upload-progress-item">
            <div class="upload-progress-heading">
              <span class="upload-progress-name" [title]="upload.label">{{ upload.label }}</span>
              @if (upload.percentage !== null) {
                <span>{{ upload.percentage }}%</span>
              } @else {
                <span>Envoi…</span>
              }
            </div>
            <div
              class="upload-progress-track"
              role="progressbar"
              [attr.aria-label]="'Envoi de ' + upload.label"
              [attr.aria-valuenow]="upload.percentage"
              aria-valuemin="0"
              aria-valuemax="100"
            >
              @if (upload.percentage !== null) {
                <div class="upload-progress-fill" [style.width.%]="upload.percentage"></div>
              } @else {
                <div class="upload-progress-fill upload-progress-indeterminate"></div>
              }
            </div>
          </div>
        }
      </section>
    }
    <router-outlet />
  `,
  styles: [
    `
      .upload-progress-panel {
        position: fixed;
        top: 1rem;
        right: 1rem;
        z-index: 10002;
        display: grid;
        gap: 0.75rem;
        width: min(22rem, calc(100vw - 2rem));
        max-height: min(50vh, 20rem);
        overflow-y: auto;
        padding: 0.875rem;
        color: #1f2937;
        background: #fff;
        border: 1px solid #d1d5db;
        border-radius: 0.5rem;
        box-shadow: 0 8px 24px rgb(0 0 0 / 12%);
      }
      .upload-progress-item {
        min-width: 0;
      }
      .upload-progress-heading {
        display: flex;
        justify-content: space-between;
        gap: 0.75rem;
        margin-bottom: 0.375rem;
        font-size: 0.75rem;
        font-weight: 600;
      }
      .upload-progress-name {
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }
      .upload-progress-track {
        height: 0.375rem;
        overflow: hidden;
        background: #e5e7eb;
        border-radius: 9999px;
      }
      .upload-progress-fill {
        height: 100%;
        background: #059669;
        border-radius: inherit;
        transition: width 120ms linear;
      }
      .upload-progress-indeterminate {
        width: 40%;
        animation: upload-progress-slide 1.2s ease-in-out infinite alternate;
      }
      @keyframes upload-progress-slide {
        from {
          transform: translateX(0);
        }
        to {
          transform: translateX(150%);
        }
      }

      /* Barre top */
      .route-loader-bar {
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        height: 3px;
        z-index: 10001;
        background: rgba(99, 102, 241, 0.15);
      }
      .route-loader-bar__fill {
        height: 100%;
        background: linear-gradient(90deg, #6366f1, #8b5cf6, #06b6d4);
        animation: bar-slide 1.4s ease-in-out infinite;
      }
      @keyframes bar-slide {
        0% {
          width: 0%;
          margin-left: 0;
        }
        50% {
          width: 65%;
          margin-left: 20%;
        }
        100% {
          width: 0%;
          margin-left: 100%;
        }
      }

      /* Overlay plein écran */
      .route-loader-overlay {
        position: fixed;
        inset: 0;
        z-index: 10000;
        display: flex;
        align-items: center;
        justify-content: center;
        background: rgba(15, 23, 42, 0.35);
        backdrop-filter: blur(2px);
        -webkit-backdrop-filter: blur(2px);
        cursor: wait;
        animation: overlay-in 120ms ease forwards;
      }
      @keyframes overlay-in {
        from {
          opacity: 0;
        }
        to {
          opacity: 1;
        }
      }

      /* Spinner */
      .route-loader-spinner {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 14px;
        padding: 28px 36px;
        background: #fff;
        border-radius: 16px;
        box-shadow: 0 20px 60px rgba(0, 0, 0, 0.18);
      }
      .route-loader-spinner svg {
        width: 44px;
        height: 44px;
        animation: spin 0.9s linear infinite;
      }
      .route-loader-spinner svg circle {
        stroke: #6366f1;
        stroke-linecap: round;
        stroke-dasharray: 90 60;
        animation: dash 1.6s ease-in-out infinite;
      }
      @keyframes spin {
        to {
          transform: rotate(360deg);
        }
      }
      @keyframes dash {
        0% {
          stroke-dashoffset: 0;
        }
        50% {
          stroke-dashoffset: -60;
        }
        100% {
          stroke-dashoffset: -125;
        }
      }
      .route-loader-label {
        font-size: 13px;
        font-weight: 500;
        color: #475569;
        letter-spacing: 0.02em;
      }
    `,
  ],
})
export class App {
  protected readonly routerLoading = inject(RouterLoadingService);
  protected readonly uploadProgress = inject(UploadProgressService);
}
