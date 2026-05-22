#!/usr/bin/env bash
set -e

# Directorio base del script
BASE_DIR="$(cd "$(dirname "$0")" && pwd)"

# Directorios de test
UNIT_TESTS="../ATLAS.Kernel.JsonEngine.Tests"
INTEGRATION_TESTS="../ATLAS.Kernel.JsonEngine.Integration.Tests"

# Archivo de configuración
RUNSETTINGS="$BASE_DIR/coverage.runsettings"

# Directorio de salida
OUTPUT_DIR="$BASE_DIR/coverage-report"

echo "🧹 Limpiando reportes anteriores..."
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

echo "🧪 Ejecutando Unit Tests con cobertura..."
dotnet test "$UNIT_TESTS" \
  --settings "$RUNSETTINGS" \
  --collect:"XPlat Code Coverage"

echo "🧪 Ejecutando Integration Tests con cobertura..."
dotnet test "$INTEGRATION_TESTS" \
  --settings "$RUNSETTINGS" \
  --collect:"XPlat Code Coverage"

echo "📊 Generando dashboard HTML..."
reportgenerator \
  -reports:"../**/coverage.cobertura.xml" \
  -targetdir:"$OUTPUT_DIR" \
  -reporttypes:Html

echo "✅ Dashboard generado en:"
echo "$OUTPUT_DIR/index.html"
