#!/bin/bash
# Pre-push hook to prevent merging ref/ollama-mcp-agent into main
# Installation: cp docs/pre-push-ollama-protection.sh .git/hooks/pre-push && chmod +x .git/hooks/pre-push
# See docs/branch-protection-ollama-mcp.md for details

set -e

current_branch=$(git rev-parse --abbrev-ref HEAD)
remote="$1"
url="$2"

# Get commits that would be pushed
while IFS= read -r local_ref local_sha remote_ref remote_sha; do
    # Check if pushing to main
    if [[ "$remote_ref" == "refs/heads/main" ]]; then
        # Prevent pushing from ref/ollama-mcp-agent to main
        if [[ "$current_branch" == "ref/ollama-mcp-agent" ]]; then
            echo "❌ PUSH REJECTED: Cannot push ref/ollama-mcp-agent branch to main"
            echo ""
            echo "The ref/ollama-mpc-agent branch is a reference implementation."
            echo "Only push to: origin ref/ollama-mcp-agent"
            echo ""
            echo "To bypass this check (not recommended):"
            echo "  git push --no-verify"
            echo ""
            exit 1
        fi

        # Check if any commit being pushed is a merge from ref/ollama-mcp-agent
        if [[ -z "$remote_sha" || "$remote_sha" == "0000000000000000000000000000000000000000" ]]; then
            commits_to_push=$(git rev-list "$local_sha")
        else
            commits_to_push=$(git rev-list "$remote_sha..$local_sha" 2>/dev/null || echo "")
        fi

        if [[ -n "$commits_to_push" ]]; then
            while IFS= read -r commit; do
                merge_message=$(git log --format=%B -n1 "$commit" 2>/dev/null || true)

                if echo "$merge_message" | grep -qi "merge.*ref/ollama-mcp-agent"; then
                    echo "❌ PUSH REJECTED: Cannot merge ref/ollama-mcp-agent into main"
                    echo ""
                    echo "The ref/ollama-mcp-agent branch is a reference implementation"
                    echo "and must not be merged to main."
                    echo ""
                    echo "If you need code from this branch, cherry-pick specific commits instead:"
                    echo "  git cherry-pick <commit-hash>"
                    echo ""
                    echo "To bypass this check (not recommended):"
                    echo "  git push --no-verify"
                    echo ""
                    exit 1
                fi
            done <<< "$commits_to_push"
        fi
    fi
done

exit 0
