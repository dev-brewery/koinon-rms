#!/usr/bin/env python3
"""Analyze Claude instruction docs for redundancy and bloat."""

import os
import sys
import glob
import google.generativeai as genai

# Configure API
api_key = os.environ.get('GOOGLE_API_KEY')
if not api_key:
    print("Error: GOOGLE_API_KEY not set")
    sys.exit(1)

genai.configure(api_key=api_key)

# Collect all docs
project_root = "/home/mbrewer/projects/koinon-rms"
doc_patterns = [
    f"{project_root}/CLAUDE.md",
    f"{project_root}/.claude/*.md",
    f"{project_root}/.claude/agents/*.md",
    f"{project_root}/.claude/templates/*.md",
    f"{project_root}/.claude/context/*.md",
    f"{project_root}/.claude/commands/*.md",
]

docs = {}
for pattern in doc_patterns:
    for filepath in glob.glob(pattern):
        with open(filepath, 'r') as f:
            content = f.read()
            rel_path = filepath.replace(project_root + "/", "")
            docs[rel_path] = content

# Build analysis prompt
prompt = """Analyze these Claude Code instruction files for context bloat and effectiveness issues.

SPECIFIC PROBLEMS TO INVESTIGATE:
1. The agent keeps writing code directly instead of delegating to subagents (ignoring PM role instructions)
2. The agent skips checking memory MCP at session start
3. The agent uses wrong GitHub owner ("koinon-dev" instead of "dev-brewery")

FOR EACH FILE, PROVIDE:
- Line count
- Token estimate (chars/4)
- Key instructions it contains
- Redundancy with other files (list specific overlaps)

THEN PROVIDE:
1. TOTAL token estimate
2. Top 5 most redundant instruction topics (with file references)
3. Critical instructions that are buried/diluted
4. Conflicting instructions between files
5. Recommended consolidation: Which files to merge/delete

FORMAT: Markdown with specific file:line references

=== FILES ===

"""

for path, content in sorted(docs.items()):
    prompt += f"\n### {path} ({len(content)} chars, ~{len(content)//4} tokens)\n```\n{content[:5000]}{'...[TRUNCATED]' if len(content) > 5000 else ''}\n```\n"

# Call Gemini
model = genai.GenerativeModel('gemini-2.0-flash-exp')
response = model.generate_content(prompt)

print(response.text)
