# Implementation Guide: Ollama MCP Server

This guide provides a step-by-step process to deploy and run the Ollama MCP server using the files restored to the `docs/ollama-gemini/` directory. This guide is based on the state of the configuration from when the server was successfully building and running, before the introduction of more complex logic.

---

## Section 1: File Deployment

This section details how to copy each restored file from the `docs/ollama-gemini/` directory to its correct operational location.

Execute these commands from your WSL terminal in the `/home/mbrewer/projects/koinon-rms/` directory.

1.  **Create destination directories:** This ensures the target folders exist.
    ```bash
    mkdir -p /mnt/g/repos/wsl-mcp-ollama/src
    mkdir -p /home/mbrewer/projects/koinon-rms/.claude
    mkdir -p /home/mbrewer/.claude/scripts
    ```

2.  **Copy the Node.js project files:**
    ```bash
    cp docs/ollama-gemini/package.json /mnt/g/repos/wsl-mcp-ollama/package.json
    cp docs/ollama-gemini/tsconfig.json /mnt/g/repos/wsl-mcp-ollama/tsconfig.json
    cp -r docs/ollama-gemini/src /mnt/g/repos/wsl-mcp-ollama/
    ```

3.  **Copy the MCP configuration file:**
    ```bash
    cp docs/ollama-gemini/mcp.json /home/mbrewer/projects/koinon-rms/.claude/.mcp.json
    ```

4.  **Copy and set permissions for the wrapper script:**
    ```bash
    cp docs/ollama-gemini/ollama-mcp.sh /home/mbrewer/.claude/scripts/ollama-mcp.sh
    chmod +x /home/mbrewer/.claude/scripts/ollama-mcp.sh
    ```

After these steps, all files are in their correct places.

---

## Section 2: Build the MCP Server

These steps must be performed from a **Windows PowerShell terminal**.

1.  **Navigate to the project directory:**
    ```powershell
    cd G:\repos\wsl-mcp-ollama
    ```

2.  **Install dependencies:** This reads the `package.json` and installs the required libraries.
    ```powershell
    npm install
    ```

3.  **Build the code:** This reads the `tsconfig.json` and compiles the TypeScript files from `src/` into JavaScript in the `dist/` directory.
    ```powershell
    npm run build
    ```
    At this point, the code should build without any errors.

---

## Section 3: Run the Ollama Service

This version of the MCP server requires you to **manually start the Ollama service** before launching Claude Code.

1.  **Ensure no other Ollama instances are running.** Use these PowerShell commands to find and stop any existing processes.
    ```powershell
    # Find the Process ID (PID) listening on port 11434
    netstat -ano | findstr "11434"

    # If the command above shows a PID, use it in the command below (e.g., if PID is 1234)
    # taskkill /pid 1234 /f
    ```

2.  **Start a single, clean Ollama instance.** Open a **new, separate PowerShell window** and run the following command. Leave this window open.
    ```powershell
    ollama serve
    ```

---

## Section 4: Launch and Verify

1.  With the `ollama serve` process running in its own window, you can now start your Claude Code session.
2.  The `ollama` MCP server should now start without any errors.

---

## Section 5: The Final Known Issue (Empty Tools List)

You may find that after all these steps, the Claude UI still reports a "failed" connection. As we discovered from the logs, this is the final bug: the server runs, but it incorrectly tells the client that it has zero tools available (`"capabilities":{"tools":{}}`).

The fix requires adding a new dependency and making a small change to `index.ts` to correctly declare the tools at initialization.

### The Final Fix

If you encounter the "empty tools" issue, perform these steps in the `G:\repos\wsl-mcp-ollama` project:

1.  **Add the `zod-to-json-schema` dependency.** In your PowerShell window in `G:\repos\wsl-mcp-ollama`, run:
    ```powershell
    npm install zod-to-json-schema@^3.23.0
    ```

2.  **Overwrite `src/index.ts` with the corrected version.** The file `docs/ollama-gemini/src/index.ts` that I restored already contains this final fix. You can simply copy it over again:
    ```bash
    # From your WSL terminal
    cp docs/ollama-gemini/src/index.ts /mnt/g/repos/wsl-mcp-ollama/src/index.ts
    ```
    *This corrected version is the one that declares the tools inside the `new Server(...)` constructor, which solves the empty tools list problem.*

3.  **Rebuild the project one last time:**
    ```powershell
    # In your PowerShell window in G:\repos\wsl-mcp-ollama
    npm run build
    ```

After applying this final fix and restarting your Claude Code session (with `ollama serve` running manually), the server should connect and correctly display its four tools.
