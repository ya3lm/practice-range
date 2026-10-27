#!/usr/bin/env python3
"""Histogram + stats for click_to_photon.ino output. Usage: python3 analyze.py capture.csv [--out latency.png]"""
import argparse, statistics as st
import matplotlib; matplotlib.use("Agg"); import matplotlib.pyplot as plt
import pandas as pd
ap = argparse.ArgumentParser(); ap.add_argument("csv"); ap.add_argument("--out", default="latency.png"); a = ap.parse_args()
df = pd.read_csv(a.csv); ms = df.latency_us / 1000.0; ms = ms[ms < 400]
print(f"n={len(ms)} median={ms.median():.1f} ms  p95={ms.quantile(.95):.1f} ms  min={ms.min():.1f} ms  sd={ms.std():.1f} ms")
bg, fg, acc = "#0b0d11", "#e8ecf1", "#19c7c7"
fig, ax = plt.subplots(figsize=(8, 4), dpi=160); fig.patch.set_facecolor(bg); ax.set_facecolor(bg)
ax.hist(ms, bins=40, color=acc, alpha=.9); ax.axvline(ms.median(), color="#ff8c1a", ls="--", label=f"median {ms.median():.1f} ms")
ax.tick_params(colors=fg); [s.set_color("#2a323c") for s in ax.spines.values()]
ax.set_xlabel("click → photon (ms)", color=fg); ax.set_ylabel("trials", color=fg); ax.legend(facecolor=bg, labelcolor=fg, edgecolor="#2a323c")
ax.set_title("End-to-end input latency, Practice Range WebGL build", color=fg, loc="left")
fig.tight_layout(); fig.savefig(a.out, facecolor=bg); print(a.out)
