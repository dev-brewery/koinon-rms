#!/bin/bash
# Test script for performance report generator
# Creates sample metrics and runs the report generator

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WEB_DIR="$(dirname "$SCRIPT_DIR")"

cd "$WEB_DIR"

echo "Performance Report Test Script"
echo "=============================="
echo ""

# Create sample metrics
echo "Creating sample performance metrics..."
mkdir -p playwright-report
cat > playwright-report/performance-metrics.json << 'EOF'
{
  "onlineSearch": 145.23,
  "memberSelect": 67.89,
  "confirmCheckIn": 178.45,
  "fullFlow": 487.32,
  "offlineSearch": 32.11,
  "offlineCheckIn": 28.76
}
EOF

echo "Sample metrics created."
echo ""

# Run report generator
echo "Running performance report generator..."
echo ""

npx tsx scripts/performance-report.ts

echo ""
echo "=============================="
echo "Test complete!"
echo ""
echo "Generated reports:"
echo "  - playwright-report/performance-report.md"
echo "  - playwright-report/performance-report.json"
echo ""
echo "View markdown report:"
echo "  cat playwright-report/performance-report.md"
