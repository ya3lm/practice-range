#!/usr/bin/env python3
"""Plot a training run for a post: cumulative reward and episode length vs. steps, plus curriculum lesson changes.
Usage: uv run ml/scripts/plot_training.py ml/results/<run-id> [--out chart.png]
Reads TensorBoard event files, so it works on any ML-Agents run."""
import argparse, glob, os, sys
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import pandas as pd
from tensorboard.backend.event_processing.event_accumulator import EventAccumulator

def load(run_dir):
    frames = {}
    for ev in glob.glob(os.path.join(run_dir, "**", "events.out.tfevents.*"), recursive=True):
        acc = EventAccumulator(ev, size_guidance={"scalars": 0}); acc.Reload()
        for tag in acc.Tags()["scalars"]:
            rows = [(e.step, e.value) for e in acc.Scalars(tag)]
            df = pd.DataFrame(rows, columns=["step", "value"])
            frames[tag] = pd.concat([frames[tag], df]) if tag in frames else df
    return {k: v.sort_values("step").reset_index(drop=True) for k, v in frames.items()}

def main():
    ap = argparse.ArgumentParser(); ap.add_argument("run_dir"); ap.add_argument("--out"); a = ap.parse_args()
    data = load(a.run_dir)
    if not data: sys.exit(f"no TensorBoard events under {a.run_dir}")
    reward = next((v for k, v in data.items() if k.endswith("Environment/Cumulative Reward")), None)
    length = next((v for k, v in data.items() if k.endswith("Environment/Episode Length")), None)
    lessons = {k: v for k, v in data.items() if "/Lesson/" in k or k.startswith("Environment/Lesson")}
    fig, ax = plt.subplots(2, 1, figsize=(9, 6), sharex=True, dpi=160)
    bg, fg, acc = "#0b0d11", "#e8ecf1", "#19c7c7"
    fig.patch.set_facecolor(bg)
    for axis, df, title in ((ax[0], reward, "Cumulative reward per episode"), (ax[1], length, "Episode length (steps survived)")):
        axis.set_facecolor(bg); axis.tick_params(colors=fg); [s.set_color("#2a323c") for s in axis.spines.values()]
        axis.set_title(title, color=fg, loc="left", fontsize=11)
        if df is not None:
            axis.plot(df.step, df.value, color=acc, lw=1.2, alpha=0.35)
            axis.plot(df.step, df.value.rolling(10, min_periods=1).mean(), color=acc, lw=2.2)
    for k, v in lessons.items():
        changes = v[v.value.diff() != 0]
        for s in changes.step[1:]:
            ax[0].axvline(s, color="#ff8c1a", lw=1, ls="--", alpha=0.8)
    ax[1].set_xlabel("training steps", color=fg)
    fig.text(0.01, 0.01, "dashed = curriculum lesson change (synthetic player gets better)", color="#8d98a5", fontsize=8)
    fig.tight_layout()
    out = a.out or os.path.join(a.run_dir, "training.png")
    fig.savefig(out, facecolor=bg); print(out)
    if reward is not None:
        print(f"final reward (last 10 avg): {reward.value.tail(10).mean():.3f}  · steps: {int(reward.step.max())}")

if __name__ == "__main__": main()
