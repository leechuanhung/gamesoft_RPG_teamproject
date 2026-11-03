# Block-resolution pixel drawing in the Jina sprite style: flat fills, one shade, 1-block black outlines.
import numpy as np
from PIL import Image, ImageDraw
K = (11, 9, 11)
class Canvas:
    def __init__(s, w, h):
        s.w, s.h = w, h
        s.c = np.zeros((h, w, 3), np.uint8); s.a = np.zeros((h, w), bool)
    def blank(s): return np.zeros((s.h, s.w), bool)
    def rect(s, x0, y0, x1, y1):
        m = s.blank(); m[max(0, y0):y1 + 1, max(0, x0):x1 + 1] = True; return m
    def poly(s, pts):
        im = Image.new('1', (s.w, s.h)); ImageDraw.Draw(im).polygon(pts, fill=1); return np.array(im, bool)
    def ellipse(s, x0, y0, x1, y1):
        im = Image.new('1', (s.w, s.h)); ImageDraw.Draw(im).ellipse((x0, y0, x1, y1), fill=1); return np.array(im, bool)
    def line(s, pts, width=1):
        im = Image.new('1', (s.w, s.h)); ImageDraw.Draw(im).line(pts, fill=1, width=width); return np.array(im, bool)
    def spans(s, d):
        m = s.blank()
        for y, segs in d.items():
            for x0, x1 in (segs if isinstance(segs, list) else [segs]): m[y, x0:x1 + 1] = True
        return m
    def ring(s, m):
        r = s.blank(); r[1:] |= m[:-1]; r[:-1] |= m[1:]; r[:, 1:] |= m[:, :-1]; r[:, :-1] |= m[:, 1:]
        return r & ~m
    def layer(s, m, color, outline=True):
        if outline:
            r = s.ring(m); s.c[r] = K; s.a[r] = True
        s.c[m] = color; s.a[m] = True
    def paint(s, m, color, inside=True):
        if inside: m = m & s.a
        s.c[m] = color; s.a |= m
    def save(s, path, scale=1):
        img = np.zeros((s.h, s.w, 4), np.uint8); img[..., :3] = s.c; img[..., 3] = np.where(s.a, 255, 0)
        im = Image.fromarray(img); im = im.crop(im.getbbox())
        if scale > 1: im = im.resize((im.width * scale, im.height * scale), Image.NEAREST)
        im.save(path); return im
