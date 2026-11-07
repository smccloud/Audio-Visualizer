#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="$ROOT/dist"
mkdir -p "$OUT"

dotnet publish "$ROOT/ShaunsAudioVisualizer/ShaunsAudioVisualizer.csproj" \
  --configuration Release \
  --framework net10.0 \
  --runtime linux-x64 \
  --self-contained true \
  /p:PublishSingleFile=true \
  --output "$OUT/app"

# Requires appimagetool on PATH.
APPDIR="$OUT/ShaunsAudioVisualizer.AppDir"
rm -rf "$APPDIR"
mkdir -p "$APPDIR/usr/bin" "$APPDIR/usr/share/applications" "$APPDIR/usr/share/icons/hicolor/256x256/apps"

cp "$OUT/app/ShaunsAudioVisualizer" "$APPDIR/usr/bin/shauns-audio-visualizer"
chmod +x "$APPDIR/usr/bin/shauns-audio-visualizer"

cat > "$APPDIR/shauns-audio-visualizer.desktop" <<'EOF'
[Desktop Entry]
Type=Application
Name=Shauns Audio Visualizer
Exec=shauns-audio-visualizer
Icon=shauns-audio-visualizer
Categories=AudioVideo;Audio;
Terminal=true
EOF
cp "$APPDIR/shauns-audio-visualizer.desktop" "$APPDIR/usr/share/applications/"

# Placeholder icon reference; replace with real artwork.
touch "$APPDIR/shauns-audio-visualizer.png"
cp "$APPDIR/shauns-audio-visualizer.png" "$APPDIR/usr/share/icons/hicolor/256x256/apps/shauns-audio-visualizer.png"

cat > "$APPDIR/AppRun" <<'EOF'
#!/bin/sh
HERE="$(dirname "$(readlink -f "$0")")"
exec "$HERE/usr/bin/shauns-audio-visualizer" "$@"
EOF
chmod +x "$APPDIR/AppRun"

appimagetool "$APPDIR" "$OUT/ShaunsAudioVisualizer-x86_64.AppImage"