#!/usr/bin/env bash
# =============================================================================
# generate-coverage.sh
# Collect code coverage from the running HRMgmt ASP.NET app while Selenium
# tests execute, then produce a detailed HTML report grouped by MVC layer.
#
# Usage:
#   ./scripts/generate-coverage.sh [--rebuild] [--port PORT] [--open]
#
# Options:
#   --rebuild   Force a clean rebuild before running (default: build only)
#   --port      Port for the app (default: 5175)
#   --open      Open the HTML report in the browser after generation
#
# Prerequisites (installed once):
#   dotnet tool install --global dotnet-coverage
#   dotnet tool install --global dotnet-reportgenerator-globaltool
# =============================================================================

set -euo pipefail

# ---------------------------------------------------------------------------
# Defaults
# ---------------------------------------------------------------------------
REBUILD=false
APP_PORT=5175
OPEN_REPORT=false
SESSION_ID="hrmgmt-coverage-$$"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

COVERAGE_DIR="$REPO_ROOT/coverage"
REPORT_DIR="$REPO_ROOT/coverage-html"
HISTORY_DIR="$REPO_ROOT/coverage-history"
COVERAGE_FILE="$COVERAGE_DIR/app-coverage.cobertura.xml"

APP_LOG="$REPO_ROOT/app-coverage.log"
APP_URL="http://127.0.0.1:$APP_PORT"

# ---------------------------------------------------------------------------
# Argument parsing
# ---------------------------------------------------------------------------
while [[ $# -gt 0 ]]; do
  case "$1" in
    --rebuild) REBUILD=true ;;
    --port)    APP_PORT="$2"; APP_URL="http://127.0.0.1:$APP_PORT"; shift ;;
    --open)    OPEN_REPORT=true ;;
    *) echo "Unknown option: $1" && exit 1 ;;
  esac
  shift
done

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------
check_tool() {
  if ! command -v "$1" &>/dev/null; then
    echo "ERROR: '$1' not found. Install it with:"
    echo "  $2"
    exit 1
  fi
}

wait_for_app() {
  local url="$1" timeout=90
  echo "Waiting for app at $url ..."
  for ((i = 1; i <= timeout; i++)); do
    if curl -fsS "$url" >/dev/null 2>&1; then
      echo "App ready after ${i}s"
      return 0
    fi
    sleep 1
  done
  echo "ERROR: App did not start within ${timeout}s"
  echo "=== App log ==="
  cat "$APP_LOG" || true
  return 1
}

cleanup() {
  echo ""
  echo "--- Cleaning up ---"
  if [[ -n "${COVERAGE_PID:-}" ]] && kill -0 "$COVERAGE_PID" 2>/dev/null; then
    echo "Shutting down dotnet-coverage session '$SESSION_ID' ..."
    dotnet-coverage shutdown "$SESSION_ID" 2>/dev/null || true
    wait "$COVERAGE_PID" 2>/dev/null || true
  fi
}
trap cleanup EXIT

# ---------------------------------------------------------------------------
# Prerequisites
# ---------------------------------------------------------------------------
check_tool dotnet-coverage \
  "dotnet tool install --global dotnet-coverage"

check_tool reportgenerator \
  "dotnet tool install --global dotnet-reportgenerator-globaltool"

# ---------------------------------------------------------------------------
# Build
# ---------------------------------------------------------------------------
cd "$REPO_ROOT"

if [[ "$REBUILD" == true ]]; then
  echo "--- Clean rebuild ---"
  dotnet clean HRMgmt.sln
fi

echo "--- Building solution ---"
dotnet build HRMgmt.sln --configuration Debug --no-restore 2>&1 | tail -5

# ---------------------------------------------------------------------------
# Prepare output directories
# ---------------------------------------------------------------------------
mkdir -p "$COVERAGE_DIR" "$REPORT_DIR" "$HISTORY_DIR"
rm -f "$COVERAGE_FILE" "$APP_LOG"

# ---------------------------------------------------------------------------
# Start app under dotnet-coverage
# ---------------------------------------------------------------------------
echo ""
echo "--- Starting app on $APP_URL under coverage ---"
dotnet-coverage collect \
  --output "$COVERAGE_FILE" \
  --output-format cobertura \
  --session-id "$SESSION_ID" \
  dotnet run \
    --project HRMgmt/HRMgmt.csproj \
    --configuration Debug \
    --no-build \
    --urls "$APP_URL" \
  >"$APP_LOG" 2>&1 &

COVERAGE_PID=$!
echo "Coverage collector PID: $COVERAGE_PID (session: $SESSION_ID)"

wait_for_app "$APP_URL"

# ---------------------------------------------------------------------------
# Run Selenium tests
# ---------------------------------------------------------------------------
echo ""
echo "--- Running Selenium tests ---"
TEST_EXIT=0
dotnet test HRMgmtTest/HRMgmtTest.csproj \
  --no-build \
  --configuration Debug \
  --verbosity normal \
  --settings HRMgmtTest/test.runsettings \
  --logger "trx;LogFileName=test-results.trx" \
  --results-directory "$COVERAGE_DIR" \
  || TEST_EXIT=$?

# ---------------------------------------------------------------------------
# Stop app & flush coverage
# ---------------------------------------------------------------------------
echo ""
echo "--- Stopping app and flushing coverage ---"
dotnet-coverage shutdown "$SESSION_ID" 2>/dev/null || true
wait "$COVERAGE_PID" 2>/dev/null || true

# Build exited; unset so cleanup trap doesn't double-call
COVERAGE_PID=""

# ---------------------------------------------------------------------------
# Check coverage file was produced
# ---------------------------------------------------------------------------
if [[ ! -f "$COVERAGE_FILE" ]]; then
  echo "ERROR: Coverage file was not produced at $COVERAGE_FILE"
  exit 1
fi
echo "Coverage file: $COVERAGE_FILE ($(wc -c < "$COVERAGE_FILE") bytes)"

# ---------------------------------------------------------------------------
# Generate HTML report
# ---------------------------------------------------------------------------
echo ""
echo "--- Generating HTML report ---"
reportgenerator \
  -reports:"$COVERAGE_FILE" \
  -targetdir:"$REPORT_DIR" \
  -reporttypes:"Html;HtmlSummary;Badges" \
  -sourcedirs:"$REPO_ROOT/HRMgmt" \
  -historydir:"$HISTORY_DIR" \
  -assemblyfilters:"+HRMgmt" \
  -classfilters:"-HRMgmt.Migrations.*;-HRMgmt.Program;-HRMgmt.OrgDbContext" \
  -title:"HRMgmt Selenium Coverage" \
  -tag:"local-$(date +%Y%m%d-%H%M%S)"

echo ""
echo "=================================================================="
echo " Report:  $REPORT_DIR/index.html"
echo " Summary: $REPORT_DIR/summary.html"
echo "=================================================================="

if [[ "$OPEN_REPORT" == true ]]; then
  xdg-open "$REPORT_DIR/index.html" 2>/dev/null \
    || open "$REPORT_DIR/index.html" 2>/dev/null \
    || echo "(Could not auto-open browser)"
fi

# Propagate test failures so CI can catch them, but still produce the report
exit $TEST_EXIT
