#!/usr/bin/env bash
# Screenshots of the game's real UI, which the camera captures in the play checks cannot show.
#   bash Tools/run_ui_shots.sh      PNGs go to validation/ui/ (look at them, then delete them)
# It works on a copy of the project in the temp folder, so the Unity editor can stay open. A small game window
# appears for a few minutes while it photographs each state at three screen sizes (Assets/Scripts/UiShots.cs).
set -u
cd "$(dirname "$0")/.."
UNITY="C:/Program Files/Unity/Hub/Editor/2022.3.62f1/Editor/Unity.exe"
WORK="${TEMP:-/tmp}/seonbi-uishots"; COPY="$WORK/project"; EXE="$WORK/build/seonbi.exe"
mkdir -p "$COPY" "$WORK/build"
rm -rf "$COPY/Assets" "$COPY/ProjectSettings" "$COPY/Packages"
cp -r Assets ProjectSettings Packages "$COPY/"
# The first run borrows the import cache, so it need not reimport everything.
[ -d "$COPY/Library" ] || cp -r Library "$COPY/" 2>/dev/null
rm -rf "$COPY/Library/ScriptAssemblies" "$COPY/Temp"
MSYS_NO_PATHCONV=1 "$UNITY" -batchmode -quit -projectPath "$(cygpath -m "$COPY")" -executeMethod UiShotsBuild.Build -uishotsexe "$(cygpath -m "$EXE")" -logFile "$(cygpath -m "$WORK/build.log")"
if ! grep -q "UI_SHOTS_BUILD: Succeeded" "$WORK/build.log"; then
  grep -E "error CS|UI_SHOTS_BUILD|Error building" "$WORK/build.log" | head -20; echo "build FAILED (log: $WORK/build.log)"; exit 1
fi
rm -rf validation/ui; mkdir -p validation/ui
SHOTS="$(cd validation/ui && pwd -W)"
"$EXE" -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -logFile "$(cygpath -w "$WORK/player.log")" -uishots "$SHOTS"
grep -E "Exception" "$WORK/player.log" | head -10
python -c "import glob,os; from PIL import Image; [(Image.open(f).save(f[:-3]+'png'), os.remove(f)) for f in glob.glob('validation/ui/*.ppm')]"
echo "$(ls validation/ui | wc -l) screenshots in validation/ui"
