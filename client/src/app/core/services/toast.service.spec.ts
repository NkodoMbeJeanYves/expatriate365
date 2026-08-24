import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { MessageService } from 'primeng/api';
import { ToastService } from './toast.service';

describe('ToastService', () => {
  let service: ToastService;
  let addSpy: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    addSpy = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        ToastService,
        { provide: MessageService, useValue: { add: addSpy } },
      ],
    });
    service = TestBed.inject(ToastService);
  });

  it('success calls add with severity success', () => {
    service.success('done');
    expect(addSpy).toHaveBeenCalledWith(expect.objectContaining({ severity: 'success', detail: 'done' }));
  });

  it('error calls add with severity error', () => {
    service.error('oops');
    expect(addSpy).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error', detail: 'oops' }));
  });

  it('info calls add with severity info', () => {
    service.info('note');
    expect(addSpy).toHaveBeenCalledWith(expect.objectContaining({ severity: 'info' }));
  });

  it('warn calls add with severity warn', () => {
    service.warn('careful');
    expect(addSpy).toHaveBeenCalledWith(expect.objectContaining({ severity: 'warn' }));
  });

  it('success uses default summary Succès', () => {
    service.success('ok');
    expect(addSpy).toHaveBeenCalledWith(expect.objectContaining({ summary: 'Succès' }));
  });

  it('error uses life 6000', () => {
    service.error('fail');
    expect(addSpy).toHaveBeenCalledWith(expect.objectContaining({ life: 6000 }));
  });
});
