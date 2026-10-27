#!/usr/bin/env bash
# Usage: tools/run_tests.sh [EditMode|PlayMode]   (default: EditMode)
source "$(dirname "$0")/common.sh"
PLATFORM="${1:-EditMode}"
OUT="$REPO/build/test-results-$PLATFORM.xml"
LOG="$LOGDIR/tests-$PLATFORM.log"
"$UNITY_BIN" -batchmode -nographics -projectPath "$PROJECT" -runTests -testPlatform "$PLATFORM" \
  -testResults "$OUT" -logFile "$LOG"
STATUS=$?
if [[ -f "$OUT" ]]; then
  python3 - "$OUT" <<'PY'
import sys, xml.etree.ElementTree as ET
r = ET.parse(sys.argv[1]).getroot()
print(f"total={r.get('total')} passed={r.get('passed')} failed={r.get('failed')} skipped={r.get('skipped')}")
for tc in r.iter('test-case'):
    if tc.get('result') != 'Passed':
        msg = tc.find('.//message'); print(f"  {tc.get('result')}: {tc.get('fullname')}\n    {(msg.text or '').strip()[:300] if msg is not None else ''}")
PY
else
  echo "No results file; see $LOG"; grep -E "error CS|Exception" "$LOG" | tail -20
fi
exit $STATUS
