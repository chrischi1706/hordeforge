#!/usr/bin/env bash
#
# HordeForge Entwickler-Helfer.
#
# Kapselt das projektlokale .NET SDK, damit die Spiellogik ohne Unity-Editor
# gebaut und getestet werden kann.
#
#   ./dev.sh build    Core-Assembly kompilieren
#   ./dev.sh test     NUnit-Tests der Core-Logik ausfuehren
#   ./dev.sh unity    Unity-Layer gegen nachgebildete Unity-APIs uebersetzen
#   ./dev.sh play     Headless-Simulation (Balancing), z.B. ./dev.sh play 12 2026
#   ./dev.sh all      build + unity + test
#   ./dev.sh clean    Build-Artefakte entfernen
#
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DOTNET_DIR="$ROOT/.tools/dotnet"

if [[ ! -x "$DOTNET_DIR/dotnet" ]]; then
  cat >&2 <<EOF
Das projektlokale .NET SDK fehlt unter:
  $DOTNET_DIR

Einmalig nachinstallieren mit:
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel LTS --install-dir "$DOTNET_DIR" --no-path
EOF
  exit 1
fi

export DOTNET_ROOT="$DOTNET_DIR"
export PATH="$DOTNET_DIR:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

CORE_PROJ="$ROOT/Tools/HordeForge.Core.Standalone/HordeForge.Core.Standalone.csproj"
TEST_PROJ="$ROOT/Tools/HordeForge.Core.Tests/HordeForge.Core.Tests.csproj"
UNITY_PROJ="$ROOT/Tools/HordeForge.UnityLayer.Check/HordeForge.UnityLayer.Check.csproj"
PLAY_PROJ="$ROOT/Tools/HordeForge.Playthrough/HordeForge.Playthrough.csproj"

case "${1:-test}" in
  build)
    dotnet build "$CORE_PROJ" -v minimal --nologo
    ;;
  test)
    dotnet test "$TEST_PROJ" -v minimal --nologo
    ;;
  unity)
    dotnet build "$UNITY_PROJ" -v minimal --nologo
    ;;
  play)
    shift || true
    dotnet build "$PLAY_PROJ" -v quiet --nologo
    dotnet run --project "$PLAY_PROJ" -v quiet --nologo --no-build -- "$@"
    ;;
  all)
    dotnet build "$CORE_PROJ" -v minimal --nologo
    dotnet build "$UNITY_PROJ" -v minimal --nologo
    dotnet test "$TEST_PROJ" -v minimal --nologo
    ;;
  clean)
    rm -rf "$ROOT"/Tools/*/bin "$ROOT"/Tools/*/obj
    echo "Build-Artefakte entfernt."
    ;;
  *)
    echo "Unbekannter Befehl: $1" >&2
    echo "Erlaubt: build | unity | test | play | all | clean" >&2
    exit 1
    ;;
esac
