#!/bin/bash
# Fixture verification script
# Ensures all expected fixtures exist and are properly formatted

set -e

FIXTURES_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "Verifying graph generator test fixtures..."
echo ""

# Color codes
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Track results
PASS=0
FAIL=0
WARN=0

# Function to check if file exists
check_file() {
    local file="$1"
    local description="$2"
    
    if [ -f "$FIXTURES_DIR/$file" ]; then
        echo -e "${GREEN}✓${NC} $description: $file"
        ((PASS++))
    else
        echo -e "${RED}✗${NC} $description: $file (MISSING)"
        ((FAIL++))
    fi
}

# Function to check file is non-empty (except for intentional empty files)
check_content() {
    local file="$1"
    local min_bytes="$2"
    local description="$3"
    
    if [ -f "$FIXTURES_DIR/$file" ]; then
        local size=$(stat -c%s "$FIXTURES_DIR/$file" 2>/dev/null || stat -f%z "$FIXTURES_DIR/$file" 2>/dev/null)
        if [ "$size" -ge "$min_bytes" ]; then
            echo -e "${GREEN}✓${NC} $description: $file ($size bytes)"
            ((PASS++))
        else
            echo -e "${YELLOW}⚠${NC} $description: $file (only $size bytes, expected >= $min_bytes)"
            ((WARN++))
        fi
    fi
}

echo "=== Valid Backend Fixtures ==="
check_file "valid/PersonEntity.cs" "Valid entity"
check_file "valid/PersonDto.cs" "Valid DTO"
check_file "valid/PersonService.cs" "Valid service"
check_file "valid/PeopleController.cs" "Valid controller"
echo ""

echo "=== Invalid Backend Fixtures ==="
check_file "invalid/BadEntity.cs" "Invalid entity"
check_file "invalid/ExposedIdDto.cs" "Invalid DTO (int Id)"
check_file "invalid/IntIdController.cs" "Invalid controller ({id})"
echo ""

echo "=== Edge Case Backend Fixtures ==="
check_file "edge-cases/EmptyFile.cs" "Empty C# file"
check_file "edge-cases/SyntaxError.cs" "Syntax error file"
check_file "edge-cases/UnicodeNames.cs" "Unicode content"
check_file "edge-cases/UnusualFormatting.cs" "Unusual formatting"
echo ""

echo "=== Valid Frontend Fixtures ==="
check_file "valid/types.ts" "Valid TypeScript types"
check_file "valid/people.ts" "Valid API service"
check_file "valid/usePeople.ts" "Valid hooks"
echo ""

echo "=== Invalid Frontend Fixtures ==="
check_file "invalid/directFetch.tsx" "Invalid component (direct fetch)"
echo ""

echo "=== Edge Case Frontend Fixtures ==="
check_file "edge-cases/emptyTypes.ts" "Empty TypeScript file"
check_file "edge-cases/syntaxError.ts" "TypeScript syntax errors"
check_file "edge-cases/unicodeContent.ts" "Unicode TypeScript"
check_file "edge-cases/unusualFormatting.ts" "Unusual TS formatting"
echo ""

echo "=== Content Verification ==="
check_content "valid/PersonEntity.cs" 1000 "Entity has content"
check_content "valid/PersonDto.cs" 500 "DTO has content"
check_content "valid/PersonService.cs" 2000 "Service has content"
check_content "valid/PeopleController.cs" 3000 "Controller has content"
check_content "valid/types.ts" 1000 "Types file has content"
check_content "valid/people.ts" 1000 "API service has content"
check_content "valid/usePeople.ts" 1000 "Hooks file has content"
check_content "edge-cases/EmptyFile.cs" 0 "Empty file is intentionally small"
check_content "edge-cases/emptyTypes.ts" 0 "Empty TS file is intentionally small"
echo ""

echo "=== Documentation ==="
check_file "README.md" "Fixtures README"
echo ""

echo "=== Summary ==="
echo -e "${GREEN}Passed:${NC} $PASS"
if [ $WARN -gt 0 ]; then
    echo -e "${YELLOW}Warnings:${NC} $WARN"
fi
if [ $FAIL -gt 0 ]; then
    echo -e "${RED}Failed:${NC} $FAIL"
    exit 1
else
    echo -e "${GREEN}All fixtures verified successfully!${NC}"
    exit 0
fi
