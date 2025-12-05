#!/usr/bin/env python3
"""
List Available Gemini Models
Useful for finding model names when default model fails

Usage:
    python list_models.py
"""

import sys
import os

# Load API key from .env file if present
try:
    env_path = os.path.join(os.path.dirname(__file__), '.env')
    with open(env_path, 'r') as f:
        for line in f:
            if line.strip() and not line.startswith('#'):
                key, value = line.strip().split('=', 1)
                os.environ[key.strip()] = value.strip().strip('"').strip("'")
except FileNotFoundError:
    pass

try:
    import google.generativeai as genai
except ImportError:
    print("ERROR: The 'google.generativeai' library is not installed.")
    print("Install it with: pip install google-generativeai")
    sys.exit(1)

try:
    genai.configure(api_key=os.environ["GOOGLE_API_KEY"])
except KeyError:
    print("ERROR: GOOGLE_API_KEY environment variable is not set.")
    print("Set it in .devenv or export it: export GOOGLE_API_KEY='your_key'")
    sys.exit(1)

print("Available Gemini Models:")
print("=" * 80)
print("")

for model in genai.list_models():
    # Filter for models that support generateContent
    if 'generateContent' in model.supported_generation_methods:
        vision_capable = "🖼️  VISION" if "vision" in model.name.lower() or "flash" in model.name.lower() else ""
        print(f"Model: {model.name}")
        print(f"  Display Name: {model.display_name}")
        print(f"  Description: {model.description}")
        if vision_capable:
            print(f"  {vision_capable}")
        print("")

print("=" * 80)
print("TIP: Use models with 'flash' or 'vision' for image processing")
print("     Example: models/gemini-2.0-flash-exp")
