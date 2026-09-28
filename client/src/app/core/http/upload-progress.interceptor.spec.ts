import {
  HttpClient,
  HttpEventType,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { uploadProgressInterceptor } from './upload-progress.interceptor';
import { UploadProgressService } from './upload-progress.service';

describe('uploadProgressInterceptor', () => {
  let http: HttpClient;
  let httpTesting: HttpTestingController;
  let progress: UploadProgressService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([uploadProgressInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
    progress = TestBed.inject(UploadProgressService);
  });

  afterEach(() => httpTesting.verify());

  it('tracks multipart upload progress and clears it when the request completes', () => {
    const formData = new FormData();
    formData.append('file', new Blob(['test']), 'report.pdf');

    http.post('/api/upload', formData).subscribe();

    const request = httpTesting.expectOne('/api/upload');
    expect(request.request.reportProgress).toBe(true);
    expect(progress.uploads()).toHaveLength(1);
    expect(progress.uploads()[0].label).toBe('report.pdf');

    request.event({ type: HttpEventType.UploadProgress, loaded: 40, total: 100 });

    expect(progress.uploads()[0].percentage).toBe(40);
    expect(progress.overallPercentage()).toBe(40);

    request.flush({ uploaded: true });

    expect(progress.uploads()).toHaveLength(0);
    expect(progress.overallPercentage()).toBeNull();
  });

  it('does not track requests that do not use FormData', () => {
    http.post('/api/update', { value: 'test' }).subscribe();

    httpTesting.expectOne('/api/update').flush({ updated: true });

    expect(progress.uploads()).toHaveLength(0);
  });

  it('aggregates concurrent uploads by their total byte sizes', () => {
    const firstForm = new FormData();
    firstForm.append('file', new Blob(['first']), 'first.pdf');
    const secondForm = new FormData();
    secondForm.append('file', new Blob(['second']), 'second.pdf');

    http.post('/api/upload/first', firstForm).subscribe();
    http.post('/api/upload/second', secondForm).subscribe();

    const firstRequest = httpTesting.expectOne('/api/upload/first');
    const secondRequest = httpTesting.expectOne('/api/upload/second');
    firstRequest.event({ type: HttpEventType.UploadProgress, loaded: 25, total: 100 });
    secondRequest.event({ type: HttpEventType.UploadProgress, loaded: 50, total: 300 });

    expect(progress.uploads()).toHaveLength(2);
    expect(progress.overallPercentage()).toBe(19);

    firstRequest.flush({ uploaded: true });
    secondRequest.flush({ uploaded: true });
    expect(progress.uploads()).toHaveLength(0);
  });

  it('keeps unknown progress indeterminate and clears an upload when cancelled', () => {
    const formData = new FormData();
    formData.append('file', new Blob(['test']), 'report.pdf');

    const subscription = http.post('/api/upload', formData).subscribe();
    const request = httpTesting.expectOne('/api/upload');
    request.event({ type: HttpEventType.UploadProgress, loaded: 10 });

    expect(progress.uploads()[0].percentage).toBeNull();
    expect(progress.overallPercentage()).toBeNull();

    subscription.unsubscribe();

    expect(progress.uploads()).toHaveLength(0);
  });
});
