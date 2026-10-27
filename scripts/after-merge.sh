#!/usr/bin/env bash
# Run this after merging a pull request on GitHub.
# Usage: scripts/after-merge.sh [next-branch-name]
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

if [[ -n "$(git status --porcelain)" ]]; then
  echo "You have uncommitted changes. Commit or stash them first:"
  git status --short
  exit 1
fi

old="$(git branch --show-current)"

git checkout main
git pull
git fetch --prune

# -d (not -D) only deletes a branch that has been merged, so nothing unmerged is lost.
if [[ -n "$old" && "$old" != "main" ]]; then
  git branch -d "$old" || echo "Kept '$old' because it is not merged into main yet."
fi

echo
echo "main is now at:"
git log --oneline -3

next="${1:-}"
if [[ -n "$next" ]]; then
  git checkout -b "$next"
  echo "Now on new branch: $next"
fi
