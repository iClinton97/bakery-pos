#!/usr/bin/env bash
# Builds, tests, commits, pushes and opens a pull request for the current branch.
# Usage: scripts/ship.sh "feat: what this change does"
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

message="${1:-}"
if [[ -z "$message" ]]; then
  echo 'Usage: scripts/ship.sh "feat: what this change does"'
  exit 1
fi

branch="$(git branch --show-current)"
if [[ "$branch" == "main" ]]; then
  echo "You are on main. Make a branch first, for example: git checkout -b feat/my-change"
  exit 1
fi

echo "Branch: $branch"
echo "Changes:"
git status --short
echo

# Never ship code that does not build or pass its tests.
dotnet build -c Release --nologo -v q
dotnet test -c Release --no-build --nologo

echo
read -r -p "Commit, push and open a pull request? [y/N] " answer
if [[ "$answer" != "y" ]]; then
  echo "Stopped. Nothing was committed."
  exit 1
fi

git add -A
git commit \
  -m "$message" \
  -m "Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>" \
  -m "Claude-Session: https://claude.ai/code/session_01Cb617vBUyVHRzGiymUxe2G"

git push -u origin "$branch"

if gh pr view >/dev/null 2>&1; then
  echo "A pull request already exists for this branch. It has been updated."
else
  gh pr create --title "$message" --body "$(printf '%s\n\n%s\n\n%s' \
    "$message" \
    "🤖 Generated with [Claude Code](https://claude.com/claude-code)" \
    "https://claude.ai/code/session_01Cb617vBUyVHRzGiymUxe2G")"
fi
