# Click-to-photon rig (hardware post)

Parts: Arduino Uno/Nano (~$4–20), phototransistor or photodiode + 10k resistor, PC817 optocoupler to sense the mouse button
without loading the mouse circuit (or a Leonardo/Micro acting as a USB mouse that both clicks and times).

1. Tape the sensor over the spot where the hit flash appears (aim at a target so the white flash lands under the sensor).
2. Flash `click_to_photon.ino`, open serial at 115200, click 200 times, save to `capture.csv`.
3. `python3 analyze.py capture.csv` → median, p95, histogram PNG for the post.

Compare: native build vs WebGL vs WebGPU, 60 Hz vs 144 Hz, mouse polling 125 Hz vs 1000 Hz. Each comparison is one chart, one claim.
