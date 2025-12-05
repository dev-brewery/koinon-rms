#!/usr/bin/env python3
"""
Gemini Image Processing Script for Koinon RMS
Sends images to Gemini API for multimodal analysis

Usage:
    python process_image.py <path_to_image> "<prompt_text>"

Prerequisites:
    - GOOGLE_API_KEY environment variable set
    - google-generativeai library installed
"""

import sys
import os
import base64

# Load API key from .env file if present
try:
    env_path = os.path.join(os.path.dirname(__file__), '.env')
    with open(env_path, 'r') as f:
        for line in f:
            if line.strip() and not line.startswith('#'):
                key, value = line.strip().split('=', 1)
                os.environ[key.strip()] = value.strip().strip('"').strip("'")
except FileNotFoundError:
    print("INFO: .env file not found. Using environment variables.", file=sys.stderr)

try:
    import google.generativeai as genai
except ImportError:
    print("ERROR: The 'google.generativeai' library is not installed.", file=sys.stderr)
    print("Install it with: pip install google-generativeai", file=sys.stderr)
    sys.exit(1)

# Configure Gemini API
try:
    genai.configure(api_key=os.environ["GOOGLE_API_KEY"])
except KeyError:
    print("ERROR: GOOGLE_API_KEY environment variable is not set.", file=sys.stderr)
    print("Set it in .devenv or export it: export GOOGLE_API_KEY='your_key'", file=sys.stderr)
    sys.exit(1)

def send_image_to_gemini(image_path_str: str, text_prompt: str):
    """
    Reads an image, encodes it to base64, and sends it to the Gemini API
    with a text prompt.

    Args:
        image_path_str: Path to the image file
        text_prompt: Text instructions for Gemini

    Returns:
        str: Gemini's response text or error message
    """
    try:
        with open(image_path_str, "rb") as image_file:
            image_bytes = image_file.read()
        image_base64 = base64.b64encode(image_bytes).decode('utf-8')

        # Determine mime type from extension
        ext = os.path.splitext(image_path_str)[1].lower()
        mime_map = {
            '.png': 'image/png',
            '.jpg': 'image/jpeg',
            '.jpeg': 'image/jpeg',
            '.gif': 'image/gif',
            '.webp': 'image/webp'
        }
        mime_type = mime_map.get(ext, 'image/png')

        image_part = {
            "inline_data": {
                "mime_type": mime_type,
                "data": image_base64
            }
        }
        prompt_parts = [text_prompt, image_part]

        # Using Gemini 2.5 Flash - latest stable multimodal model with 1M token context
        # Available alternatives: gemini-2.0-flash, gemini-flash-latest, gemini-2.5-pro
        model = genai.GenerativeModel('models/gemini-2.5-flash')
        response = model.generate_content(prompt_parts)
        return response.text

    except FileNotFoundError:
        return f"ERROR: Image file not found at {image_path_str}"
    except Exception as e:
        error_msg = str(e)
        if "404" in error_msg or "not found" in error_msg.lower():
            return (
                f"ERROR: Model not available. Run list_models.py to find available models.\n"
                f"Then update line 68 in this script with a valid model name.\n"
                f"Original error: {error_msg}"
            )
        return f"ERROR: {error_msg}"

if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("Usage: python process_image.py <path_to_image> \"<prompt_text>\"", file=sys.stderr)
        print("", file=sys.stderr)
        print("Example:", file=sys.stderr)
        print('  python process_image.py screenshot.png "Analyze this UI and suggest improvements"', file=sys.stderr)
        sys.exit(1)

    image_path = sys.argv[1]
    prompt = sys.argv[2]

    result = send_image_to_gemini(image_path, prompt)
    print(result)
