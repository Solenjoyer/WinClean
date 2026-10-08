"""Draws the WinClean application icon.

The mark is a rounded tile with a gauge arc and a centre dot. Each size is rendered
on its own (supersampled, then downscaled) so the small entries stay crisp instead of
being blurry scaled-down copies of the large one. Keep eng/icon/winclean.svg in sync
when changing the geometry.

Usage: python eng/make-icon.py [output.ico]
"""

import math
import sys
from pathlib import Path

from PIL import Image, ImageDraw

SIZES = (16, 20, 24, 32, 40, 48, 64, 128, 256)
TILE = (0x1F, 0x5F, 0xA8, 0xFF)
INK = (0xFF, 0xFF, 0xFF, 0xFF)
SUPERSAMPLE = 8


def render(size: int) -> Image.Image:
    scale = SUPERSAMPLE * size / 256
    canvas = Image.new("RGBA", (size * SUPERSAMPLE,) * 2, (0, 0, 0, 0))
    draw = ImageDraw.Draw(canvas)

    def px(value: float) -> float:
        return value * scale

    draw.rounded_rectangle((0, 0, px(256) - 1, px(256) - 1), radius=px(56), fill=TILE)

    # Small sizes get a heavier stroke and a wider gap at the bottom, otherwise the arc closes up.
    stroke = px(38 if size <= 24 else 30)
    radius = px(72.5)
    centre = px(128)
    # The arc is laid down as a dense run of discs: Pillow's arc primitive leaves notched ends
    # where a flat stroke end meets a round cap, and discs give clean caps for free.
    angle = 135.0
    while angle <= 405.0:
        x, y = _point(centre, radius, angle)
        draw.ellipse((x - stroke / 2, y - stroke / 2, x + stroke / 2, y + stroke / 2), fill=INK)
        angle += 0.5

    dot = px(21 if size <= 24 else 17)
    draw.ellipse((centre - dot, centre - dot, centre + dot, centre + dot), fill=INK)

    return canvas.resize((size, size), Image.Resampling.LANCZOS)


def _point(centre: float, radius: float, degrees: float) -> tuple[float, float]:
    rad = math.radians(degrees)
    return centre + radius * math.cos(rad), centre + radius * math.sin(rad)


def main() -> None:
    output = Path(sys.argv[1]) if len(sys.argv) > 1 else Path("src/WinClean/Resources/WinClean.ico")
    images = {size: render(size) for size in SIZES}
    largest = images[max(SIZES)]
    largest.save(
        output,
        format="ICO",
        sizes=[(size, size) for size in SIZES],
        append_images=[images[size] for size in SIZES if size != max(SIZES)],
    )
    print(f"wrote {output} with sizes {', '.join(str(s) for s in SIZES)}")


if __name__ == "__main__":
    main()
