import { calculateImageCrop } from './image-crop.utils';

describe('calculateImageCrop', () => {
  it('uses the full height and crops a wide image to 16:9', () => {
    const crop = calculateImageCrop(2400, 1200, 50, 50, 1);

    expect(crop.x).toBeCloseTo(133.33333333333334);
    expect(crop.y).toBe(0);
    expect(crop.width).toBeCloseTo(2133.3333333333335);
    expect(crop.height).toBe(1200);
  });

  it('keeps the selected focus within the source bounds when zoomed', () => {
    const crop = calculateImageCrop(1200, 2400, 100, 100, 2);

    expect(crop.x).toBe(600);
    expect(crop.y).toBe(2062.5);
    expect(crop.x + crop.width).toBeLessThanOrEqual(1200);
    expect(crop.y + crop.height).toBeLessThanOrEqual(2400);
    expect(crop.width / crop.height).toBeCloseTo(16 / 9);
  });

  it('clamps focus and zoom values to supported ranges', () => {
    const crop = calculateImageCrop(1600, 900, 200, -20, 10);

    expect(crop.x).toBeCloseTo(1066.6666666666667);
    expect(crop.y).toBe(0);
    expect(crop.width).toBeCloseTo(533.3333333333334);
    expect(crop.height).toBeCloseTo(300);
  });

  it('rejects invalid source dimensions', () => {
    expect(() => calculateImageCrop(0, 100, 50, 50, 1)).toThrow(RangeError);
  });
});
