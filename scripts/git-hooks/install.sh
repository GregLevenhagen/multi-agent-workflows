#!/usr/bin/env bash
# =============================================================================
# Install git hooks by setting core.hooksPath to this directory
# Run once after cloning: ./scripts/git-hooks/install.sh
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

GREEN='\033[0;32m'
NC='\033[0m'

# Make hooks executable
chmod +x "$SCRIPT_DIR/pre-commit"
chmod +x "$SCRIPT_DIR/commit-msg"

# Set git to use our hooks directory
cd "$REPO_ROOT"
git config core.hooksPath scripts/git-hooks

echo -e "${GREEN}✓ Git hooks installed successfully${NC}"
echo "  Hooks directory: scripts/git-hooks/"
echo "  Hooks enabled: pre-commit, commit-msg"
echo ""
echo "  To bypass a hook temporarily: git commit --no-verify"
