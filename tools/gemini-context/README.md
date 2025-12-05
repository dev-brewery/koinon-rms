# Gemini Context Specialist Tools

This directory contains tools for the Gemini Context Specialist agent, which provides massive context window (1M+ tokens) and multimodal (vision) capabilities for Koinon RMS development.

## What is This For?

The Gemini Context Specialist agent handles tasks that exceed standard LLM context windows:

- **Large-scale code analysis** - Analyze entire codebase at once
- **Visual debugging** - Process UI screenshots, wireframes, diagrams
- **Log analysis** - Process massive log files (50MB+)
- **Full repo refactoring** - Understand system-wide impacts

## Setup

### 1. Get Gemini API Key

1. Go to https://aistudio.google.com/app/apikey
2. Create a new API key
3. Copy the key (starts with `AI...`)

### 2. Configure Environment

**Option A: Add to .devenv (Recommended)**

Add this line to `/home/mbrewer/projects/koinon-rms/.devenv`:

```bash
# Gemini API for large context analysis
export GOOGLE_API_KEY='AIza...your_actual_key'
```

Then reload: `source .devenv`

**Option B: Local .env file**

```bash
cd tools/gemini-context
cp .env.template .env
# Edit .env and add your key
```

### 3. Create Virtual Environment

```bash
cd tools/gemini-context

# Create virtual environment
python -m venv venv_gemini

# Activate (Linux/Mac)
source venv_gemini/bin/activate

# Activate (Windows)
.\venv_gemini\Scripts\activate

# Install dependencies
pip install google-generativeai

# Test
python list_models.py
```

## Usage

### List Available Models

```bash
./venv_gemini/bin/python list_models.py
```

This shows all Gemini models available to your API key. Use this if the default model fails.

### Process Images (Multimodal)

```bash
./venv_gemini/bin/python process_image.py <image_path> "<prompt>"
```

**Examples:**

```bash
# Analyze check-in kiosk screenshot
./venv_gemini/bin/python process_image.py screenshots/checkin.png "Analyze this check-in kiosk UI. Identify CSS issues and suggest TailwindCSS fixes."

# Compare wireframe to implementation
./venv_gemini/bin/python process_image.py wireframes/person-detail.png "Compare this wireframe with the implementation in PersonDetail.tsx. List all differences."

# Debug CSS alignment
./venv_gemini/bin/python process_image.py bug_screenshot.png "Why are the buttons misaligned? Provide CSS fix."
```

**Supported Image Formats:**
- PNG (`.png`)
- JPEG (`.jpg`, `.jpeg`)
- GIF (`.gif`)
- WebP (`.webp`)

### Text-Only Analysis (Gemini CLI)

For large-scale code analysis without images, use the Gemini CLI:

```bash
# Install Gemini CLI (one time)
npm install -g @google/generative-ai-cli

# Or use npx
npx -y @google/generative-ai-cli -p prompt "Find all usages of Person entity" --files "src/**/*.cs"
```

**Examples:**

```bash
# Find all integer ID usages in APIs
gemini -p prompt "Find all API routes using integer IDs instead of IdKey" --files "src/Koinon.Api/Controllers/**/*.cs"

# Analyze entity relationships
gemini -p prompt "Create entity relationship diagram description" --files "src/Koinon.Domain/Entities/*.cs"

# Check migration completeness
gemini -p prompt "Verify all entities have migrations" --files "src/Koinon.Domain/Entities/*.cs,src/Koinon.Infrastructure/Migrations/*.cs"
```

## Troubleshooting

### Error: "404 Model Not Found"

The default model (`gemini-flash-latest`) may not be available.

**Fix:**
1. Run `python list_models.py` to see available models
2. Edit `process_image.py` line 68
3. Replace with a valid model (e.g., `models/gemini-2.0-flash-exp`)

### Error: "GOOGLE_API_KEY not set"

**Fix:**
```bash
# Check if key is set
echo $GOOGLE_API_KEY

# If empty, add to .devenv
echo 'export GOOGLE_API_KEY="AIza...your_key"' >> /home/mbrewer/projects/koinon-rms/.devenv
source /home/mbrewer/projects/koinon-rms/.devenv
```

### Error: "google.generativeai not installed"

**Fix:**
```bash
cd tools/gemini-context
./venv_gemini/bin/pip install google-generativeai
```

### Virtual Environment Not Working

**Fix:**
```bash
# Delete and recreate
cd tools/gemini-context
rm -rf venv_gemini
python -m venv venv_gemini
./venv_gemini/bin/pip install google-generativeai
```

## Cost Optimization

Gemini API usage incurs costs. To minimize:

1. **Use targeted file globs** instead of entire repo
   - ❌ `--files "src/**/*"`
   - ✅ `--files "src/Koinon.Api/Controllers/**/*.cs"`

2. **Store findings in MCP memory** to avoid re-analysis
   ```javascript
   memory.store({key: "gemini-analysis-entities", value: {...}})
   ```

3. **Use CLI for text**, Python API only for images
   - CLI has better caching
   - API needed only for multimodal

4. **Prefer flash models** over pro models
   - `gemini-flash-latest` - Faster, cheaper
   - `gemini-pro` - More capable, more expensive

## Integration with Agents

The Gemini Context Specialist agent (`.claude/agents/gemini-context.md`) uses these tools automatically when delegated to by other agents.

**Example Delegation Flow:**

1. Frontend agent: "I need to analyze this check-in UI screenshot"
2. Frontend agent delegates to Gemini Context Specialist
3. Gemini agent runs: `process_image.py screenshot.png "..."`
4. Returns analysis to frontend agent

## Files in This Directory

- **process_image.py** - Send images to Gemini for analysis
- **list_models.py** - List available Gemini models
- **.env.template** - Template for API key configuration
- **venv_gemini/** - Python virtual environment (created during setup)
- **README.md** - This file

## Security

- ✅ `.env` and `venv_gemini/` are gitignored
- ✅ API keys stored in .devenv (gitignored)
- ⚠️ Never commit API keys to git
- ⚠️ Rotate keys if exposed

## Support

For issues with the Gemini Context Specialist agent:
1. Check this README troubleshooting section
2. Verify API key is valid: https://aistudio.google.com/app/apikey
3. Run `list_models.py` to check API access
4. See `.claude/agents/gemini-context.md` for agent-specific help

---

**Last Updated**: 2025-12-05
**Agent**: gemini-context
**Purpose**: Massive context analysis and multimodal debugging for Koinon RMS
