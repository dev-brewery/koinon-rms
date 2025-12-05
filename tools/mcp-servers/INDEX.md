# MCP Servers Documentation Index

Complete documentation for the Model Context Protocol (MCP) server implementation for Koinon RMS.

## Quick Navigation

### 🚀 Getting Started
- **[QUICK-START.md](QUICK-START.md)** - Get up and running in 5 minutes
- **[setup.sh](setup.sh)** - Automated installation script
- **[test-servers.sh](test-servers.sh)** - Verify installation

### 📘 Core Documentation
- **[README.md](README.md)** - Complete server overview and configuration
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - System architecture and design
- **[EXAMPLES.md](EXAMPLES.md)** - Practical usage examples

### 🛠️ Implementation Details
- **[../mcp-koinon-dev/README.md](../mcp-koinon-dev/README.md)** - Custom server documentation
- **[claude-code-config.json](claude-code-config.json)** - Ready-to-use configuration

### 📊 Reference
- **[../../MCP-SERVERS-REPORT.md](../../MCP-SERVERS-REPORT.md)** - Complete implementation report

---

## Document Purposes

### QUICK-START.md
**Read this if:** You want to get MCP servers running as fast as possible.

**Contains:**
- 5-minute installation guide
- Step-by-step setup
- Quick verification
- Troubleshooting basics

**Time to read:** 3 minutes

---

### README.md
**Read this if:** You need comprehensive information about all servers.

**Contains:**
- Overview of all 5 servers
- Detailed configuration instructions
- Setup procedures
- Usage guidelines
- Complete troubleshooting guide
- Security considerations

**Time to read:** 15-20 minutes

---

### ARCHITECTURE.md
**Read this if:** You want to understand how the system works internally.

**Contains:**
- System architecture diagrams
- Data flow illustrations
- MCP protocol details
- Multi-agent workflows
- Error handling
- Performance characteristics
- Security architecture
- Extension points

**Time to read:** 20-25 minutes

---

### EXAMPLES.md
**Read this if:** You want practical, copy-paste examples.

**Contains:**
- Examples for each server
- Common use cases
- Workflow examples
- Combined server usage
- Real-world scenarios
- Troubleshooting examples

**Time to read:** 15-20 minutes

---

### Custom Server README (mcp-koinon-dev/README.md)
**Read this if:** You need to understand or modify the Koinon dev server.

**Contains:**
- Custom server features
- Tool documentation
- Validation rules
- Anti-pattern detection
- Development guide
- Contributing guidelines

**Time to read:** 10-15 minutes

---

### MCP-SERVERS-REPORT.md
**Read this if:** You need a complete project status report.

**Contains:**
- Executive summary
- All installed servers
- Servers not available
- Complete configuration
- Testing results
- Success metrics
- Future enhancements

**Time to read:** 25-30 minutes

---

## Reading Paths

### Path 1: I Just Want It Working
1. [QUICK-START.md](QUICK-START.md) - Install everything
2. Run `./setup.sh` - Automated setup
3. Run `./test-servers.sh` - Verify it works
4. Done! Use [EXAMPLES.md](EXAMPLES.md) as needed

**Time:** 10 minutes

---

### Path 2: I Need to Understand Everything
1. [README.md](README.md) - Overview
2. [ARCHITECTURE.md](ARCHITECTURE.md) - How it works
3. [EXAMPLES.md](EXAMPLES.md) - How to use it
4. [../mcp-koinon-dev/README.md](../mcp-koinon-dev/README.md) - Custom server
5. [../../MCP-SERVERS-REPORT.md](../../MCP-SERVERS-REPORT.md) - Full report

**Time:** 60-90 minutes

---

### Path 3: I Want to Extend It
1. [ARCHITECTURE.md](ARCHITECTURE.md) - System design
2. [../mcp-koinon-dev/README.md](../mcp-koinon-dev/README.md) - Custom server
3. [../mcp-koinon-dev/src/index.ts](../mcp-koinon-dev/src/index.ts) - Source code
4. Extension Points section in ARCHITECTURE.md

**Time:** 45-60 minutes

---

### Path 4: I'm Troubleshooting an Issue
1. [QUICK-START.md](QUICK-START.md) - Troubleshooting section
2. Run `./test-servers.sh` - Identify the problem
3. [README.md](README.md) - Troubleshooting section
4. [EXAMPLES.md](EXAMPLES.md) - Find working examples

**Time:** 5-15 minutes

---

## File Structure

```
tools/
├── mcp-servers/
│   ├── INDEX.md                    ← You are here
│   ├── QUICK-START.md              ← 5-minute setup guide
│   ├── README.md                   ← Complete overview
│   ├── ARCHITECTURE.md             ← System design
│   ├── EXAMPLES.md                 ← Usage examples
│   ├── claude-code-config.json     ← Configuration file
│   ├── setup.sh                    ← Installation script
│   ├── test-servers.sh             ← Testing script
│   ├── package.json                ← npm dependencies
│   └── node_modules/               ← Installed packages
│
├── mcp-koinon-dev/
│   ├── README.md                   ← Custom server docs
│   ├── package.json                ← Server dependencies
│   ├── tsconfig.json               ← TypeScript config
│   ├── src/
│   │   └── index.ts                ← Server source code
│   ├── dist/
│   │   └── index.js                ← Compiled server
│   └── node_modules/               ← Server dependencies
│
└── ...

MCP-SERVERS-REPORT.md               ← Root-level report
```

---

## Quick Reference

### Installed Servers

| Server | Status | Package | Version |
|--------|--------|---------|---------|
| PostgreSQL | ✅ Installed | @modelcontextprotocol/server-postgres | 0.6.2 |
| Memory | ✅ Installed | @modelcontextprotocol/server-memory | 2025.11.25 |
| GitHub | ✅ Installed | @modelcontextprotocol/server-github | 2025.4.8 |
| Filesystem | ✅ Installed | @modelcontextprotocol/server-filesystem | 2025.11.25 |
| Koinon Dev | ✅ Custom | @koinon/mcp-dev-server | 1.0.0 |

### Missing Servers

| Server | Reason | Alternative |
|--------|--------|-------------|
| SQLite | Not available | Use PostgreSQL |
| HTTP/Fetch | Not available | Use bash curl |
| Docker | Not available | Use bash docker |
| Redis | Not available | Use bash redis-cli |
| .NET/NuGet | Not available | Use bash dotnet |
| npm Registry | Not available | Use bash npm |

---

## Common Tasks

### Setup
```bash
cd /home/mbrewer/projects/koinon-rms/tools/mcp-servers
./setup.sh
```

### Test
```bash
cd /home/mbrewer/projects/koinon-rms/tools/mcp-servers
./test-servers.sh
```

### Rebuild Custom Server
```bash
cd /home/mbrewer/projects/koinon-rms/tools/mcp-koinon-dev
npm run build
```

### Update Servers
```bash
cd /home/mbrewer/projects/koinon-rms/tools/mcp-servers
npm update
```

---

## Support Resources

### Documentation
- **Project Conventions:** `/home/mbrewer/projects/koinon-rms/CLAUDE.md`
- **Architecture Docs:** `/home/mbrewer/projects/koinon-rms/docs/architecture.md`

### External Links
- [MCP Specification](https://spec.modelcontextprotocol.io/)
- [MCP SDK](https://github.com/modelcontextprotocol/typescript-sdk)
- [Official Servers](https://github.com/modelcontextprotocol/servers)

### Scripts
- **Setup:** `./setup.sh`
- **Test:** `./test-servers.sh`

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-12-05 | Initial implementation with 5 servers |

---

## Contributing

To add documentation:
1. Create markdown file in appropriate location
2. Add entry to this index
3. Update relevant sections
4. Test all links

To modify servers:
1. Update source code
2. Rebuild: `npm run build`
3. Test: `./test-servers.sh`
4. Update documentation

---

**Last Updated:** December 5, 2025
**Status:** Complete
**Maintainer:** Koinon RMS Team
