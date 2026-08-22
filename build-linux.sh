#!/usr/bin/env bash
#
# Linux-Standalone-Build ueber Unity im Batch-Modus.
# Voraussetzung: Unity 6.3 LTS ist installiert und UNITY_PATH zeigt auf den Editor.
#
# Beispiel (Flatpak Hub):
#   export UNITY_PATH="$HOME/.local/share/flatpak/app/com.unity.UnityHub/current/active/files/Unity/Hub/Editor/6000.3.0f1/Editor/Unity"
#
# Oder Hub-Installation unter ~/.Unity/Hub/Editor/<version>/Editor/Unity
#
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD_DIR="$ROOT/Builds/Linux"
LOG="$ROOT/Builds/build.log"

if [[ -z "${UNITY_PATH:-}" ]]; then
  for candidate in \
    "$HOME/.Unity/Hub/Editor/"*/Editor/Unity \
    "$HOME/Unity/Hub/Editor/"*/Editor/Unity; do
    if [[ -x "$candidate" ]]; then
      UNITY_PATH="$candidate"
      break
    fi
  done
fi

if [[ -z "${UNITY_PATH:-}" || ! -x "$UNITY_PATH" ]]; then
  echo "UNITY_PATH nicht gesetzt oder Unity nicht gefunden." >&2
  echo "Setze UNITY_PATH auf den Unity-Editor-Binary." >&2
  exit 1
fi

mkdir -p "$BUILD_DIR"

echo "Baue Linux-Standalone nach $BUILD_DIR ..."
echo "Unity: $UNITY_PATH"

"$UNITY_PATH" \
  -batchmode \
  -nographics \
  -quit \
  -projectPath "$ROOT" \
  -executeMethod HordeForge.EditorTools.ProjectSetup.SetUpProject \
  -logFile "$LOG"

"$UNITY_PATH" \
  -batchmode \
  -nographics \
  -quit \
  -projectPath "$ROOT" \
  -buildLinux64Player "$BUILD_DIR/HordeForge.x86_64" \
  -logFile "$LOG"

echo "Fertig. Log: $LOG"
echo "Starten mit: $BUILD_DIR/HordeForge.x86_64"
