#!/usr/bin/env bash
# Seeds the MenuItems table by posting each entry in seed-menu.json to /api/menu.
#
# Usage:  bash docs/seed-menu.sh [baseUrl]
# Default baseUrl is http://localhost:7071
#
# Populates Hot Drinks, Cold Drinks and Pastries. Sandwiches is left empty and
# COF-001 is not used, because the Postman collection reserves both.

set -euo pipefail

BASE_URL="${1:-http://localhost:7071}"
SEED_FILE="$(dirname "$0")/seed-menu.json"

command -v jq >/dev/null 2>&1 || {
  echo "jq is required. Install it with: brew install jq"
  exit 1
}

count=$(jq 'length' "$SEED_FILE")
echo "Seeding $count menu items to $BASE_URL/api/menu"

for i in $(seq 0 $((count - 1))); do
  item=$(jq -c ".[$i]" "$SEED_FILE")
  key=$(printf '%s' "$item" | jq -r '.rowKey')
  status=$(curl -s -o /dev/null -w '%{http_code}' \
    -X POST "$BASE_URL/api/menu" \
    -H 'Content-Type: application/json' \
    -d "$item")
  case "$status" in
    201) echo "  created  $key" ;;
    *)   echo "  $status      $key" ;;
  esac
done

echo "Done."
