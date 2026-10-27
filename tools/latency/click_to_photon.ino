// Click-to-photon latency rig. Arduino (any 16 MHz+ board with USB serial).
// Wiring: photodiode/phototransistor across A0 with a 10k pull-down, taped to the screen where the muzzle flash / hit flash appears.
//         Mouse left button wired in parallel to D2 via optocoupler, OR use the "software click" mode below with a USB-HID board (Leonardo/Micro).
// Output: one CSV line per trial: trial,latency_us,threshold
// Analysis: tools/latency/analyze.py

const int PIN_LIGHT = A0;
const int PIN_CLICK = 2;      // input: goes LOW when the mouse button is pressed (optocoupler)
const int TRIALS = 200;
int baseline = 0, threshold = 0;

void setup() {
  Serial.begin(115200);
  pinMode(PIN_CLICK, INPUT_PULLUP);
  delay(500);
  long sum = 0; for (int i = 0; i < 200; i++) { sum += analogRead(PIN_LIGHT); delay(2); }
  baseline = sum / 200;
  threshold = baseline + 60;   // tune: hit flash is white on a dark range, so a large step
  Serial.println("trial,latency_us,threshold");
}

void loop() {
  static int trial = 0;
  if (trial >= TRIALS) return;
  while (digitalRead(PIN_CLICK) == HIGH) {}       // wait for click edge
  unsigned long t0 = micros();
  while (analogRead(PIN_LIGHT) < threshold) { if (micros() - t0 > 500000UL) break; }
  unsigned long dt = micros() - t0;
  Serial.print(trial++); Serial.print(','); Serial.print(dt); Serial.print(','); Serial.println(threshold);
  while (digitalRead(PIN_CLICK) == LOW) {}        // wait for release
  delay(300);
}
