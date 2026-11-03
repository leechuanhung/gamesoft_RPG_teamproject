#!/usr/bin/env bash
# Runs the project's checks from the project root and prints only what matters.
#   bash Tools/run_checks.sh          rule checks, then play checks (renders kept in validation/ only with --keep)
#   bash Tools/run_checks.sh --keep   keep validation/ renders for viewing (delete them afterwards)
#   bash Tools/run_checks.sh --art    also regenerate all art and fail if any picture changes
# With the Unity editor open the batch checks cannot run, so it falls back to compiling the game and editor
# scripts with Unity's own compiler (the editor scripts against the freshly built game assembly).
set -u
cd "$(dirname "$0")/.."
UNITY="C:/Program Files/Unity/Hub/Editor/2022.3.62f1/Editor/Unity.exe"
DATA="C:/Program Files/Unity/Hub/Editor/2022.3.62f1/Editor/Data"
LOGS="${TMPDIR:-${TEMP:-/tmp}}/seonbi-checks"; mkdir -p "$LOGS"
KEEP=0; ART=0
for a in "$@"; do case "$a" in --keep) KEEP=1;; --art) ART=1;; esac; done
status=0

if [ "$ART" = 1 ]; then
  python Tools/ArtGen/make_all.py --check || status=1
fi

if [ -f Temp/UnityLockfile ] && tasklist //FI "IMAGENAME eq Unity.exe" 2>/dev/null | grep -qi unity; then
  echo "Unity editor is open: compile-only check"
  OUT=$(cygpath -m "$LOGS" 2>/dev/null || echo "$LOGS")
  DAG=$(ls -d Library/Bee/artifacts/*.dag | head -1)
  grep -v -E '^-out:|^-refout:|^"?Assets/' "$DAG/Assembly-CSharp.rsp" > "$LOGS/game.rsp"
  ls Assets/Scripts/*.cs | sed 's/.*/"&"/' >> "$LOGS/game.rsp"; echo "-out:$OUT/Assembly-CSharp.dll" >> "$LOGS/game.rsp"
  grep -v -E '^-out:|^-refout:|^"?Assets/' "$DAG/Assembly-CSharp-Editor.rsp" \
    | sed "s#-r:\"[^\"]*Assembly-CSharp.ref.dll\"#-r:\"$OUT/Assembly-CSharp.dll\"#" > "$LOGS/editor.rsp"
  ls Assets/Editor/*.cs | sed 's/.*/"&"/' >> "$LOGS/editor.rsp"; echo "-out:$OUT/Assembly-CSharp-Editor.dll" >> "$LOGS/editor.rsp"
  for part in game editor; do
    MSYS_NO_PATHCONV=1 "$DATA/NetCoreRuntime/dotnet.exe" "$DATA/DotNetSdkRoslyn/csc.dll" /nologo "@$OUT/$part.rsp" > "$LOGS/$part.txt" 2>&1
    if grep -q "error CS" "$LOGS/$part.txt"; then grep "error CS" "$LOGS/$part.txt" | head -20; status=1; else echo "$part scripts compile"; fi
  done
  exit $status
fi

rm -rf validation
"$UNITY" -batchmode -quit -projectPath "$PWD" -executeMethod PlayerArtChecks.Run -logFile "$LOGS/rules.log"
grep -E "error CS" "$LOGS/rules.log" | head -20
grep -oE "^[A-Z_]+_PASSED[^:]*" "$LOGS/rules.log"
if ! grep -q "PLAYER_ART_CHECKS_PASSED" "$LOGS/rules.log"; then
  grep -m1 -A3 "^Exception\|Exception:" "$LOGS/rules.log"; echo "rule checks FAILED (log: $LOGS/rules.log)"; exit 1
fi
"$UNITY" -batchmode -projectPath "$PWD" -executeMethod JinaPlayChecks.Begin -logFile "$LOGS/play.log"
if grep -q "JINA_PLAY_CHECKS_PASSED" "$LOGS/play.log"; then echo "JINA_PLAY_CHECKS_PASSED"
else grep -m1 -A3 "^Exception: " "$LOGS/play.log"; echo "play checks FAILED (log: $LOGS/play.log)"; status=1; fi
if [ "$KEEP" = 1 ]; then echo "renders kept in validation/ ($(ls validation 2>/dev/null | wc -l) files)"; else rm -rf validation; fi
exit $status
