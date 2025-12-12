# Branch Protection: ref/ollama-mcp-agent

This branch (`ref/ollama-mcp-agent`) is a reference branch for the Ollama MCP agent implementation and should **never be merged to main**.

## Local Protection Setup

Since the repository doesn't have GitHub Pro branch protection, use this local git hook to prevent accidental merges.

### Installation

1. Copy the hook script to your git hooks directory:

```bash
cp docs/pre-push-ollama-protection.sh .git/hooks/pre-push
chmod +x .git/hooks/pre-push
```

2. Verify it's executable:

```bash
ls -la .git/hooks/pre-push
```

3. Test it (this should fail):

```bash
git checkout ref/ollama-mcp-agent
git push origin main  # This will be rejected
```

### How It Works

The hook prevents:
- Pushing to `main` if the commit includes a merge from `ref/ollama-mcp-agent`
- Accidental squash-merges of this branch into main

### Override (if absolutely necessary)

To bypass the hook:

```bash
git push --no-verify
```

### Safe Operations on This Branch

These operations are safe:

```bash
# Create and switch to the branch
git checkout ref/ollama-mcp-agent

# Make commits
git add .
git commit -m "Add ollama agent implementation"

# Push to this branch
git push origin ref/ollama-mcp-agent

# Pull latest
git pull origin ref/ollama-mcp-agent
```

### What to Avoid

❌ `git merge ref/ollama-mcp-agent` (from main)
❌ `git push origin HEAD:main` (from this branch)
❌ `git rebase main` (then push to main)

### Restoring if Deleted

If someone deletes the local hook:

```bash
git config core.hooksPath .githooks
# Or reinstall manually following the Installation steps
```
