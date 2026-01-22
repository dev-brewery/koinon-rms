const { QdrantClient } = require('@qdrant/js-client-rest');
const fs = require('fs').promises;
const path = require('path');
const crypto = require('crypto');

const CONFIG = {
    qdrantUrl: 'http://localhost:6333',
    ollamaUrl: 'http://host.docker.internal:11434/api/embed',
    model: 'nomic-embed-text',
    collectionName: 'koinon-rms-code',
    rootPath: '/home/mbrewer/projects/koinon-rms',
    excludeDirs: ['.git', 'node_modules', '.claude', 'venv', 'bin', 'obj', 'dist'],
    chunkSize: 1000,
    chunkOverlap: 100
};

const qdrant = new QdrantClient({ url: CONFIG.qdrantUrl });
const toUUID = (h) => `${h.slice(0,8)}-${h.slice(8,12)}-${h.slice(12,16)}-${h.slice(16,20)}-${h.slice(20,32)}`;

async function getEmbeddings(textBatch) {
    const response = await fetch(CONFIG.ollamaUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ 
            model: CONFIG.model, 
            input: textBatch.map(t => `search_document: ${t}`) 
        })
    });

    if (!response.ok) throw new Error(`Ollama Error: ${await response.text()}`);
    const data = await response.json();
    return data.embeddings;
}

async function run() {
    console.log(`🚀 Recreating collection with correct 768 dimensions...`);
    
    // Create with 768 to match nomic-embed-text
    await qdrant.createCollection(CONFIG.collectionName, { 
        vectors: { size: 768, distance: 'Cosine' } 
    }).catch(() => console.log("Collection already exists, proceeding..."));

    const walk = async (dir) => {
        const entries = await fs.readdir(dir, { withFileTypes: true });
        for (const entry of entries) {
            const fullPath = path.join(dir, entry.name);
            if (entry.isDirectory()) {
                if (!CONFIG.excludeDirs.includes(entry.name)) await walk(fullPath);
                continue;
            }

            const relPath = path.relative(CONFIG.rootPath, fullPath);
            try {
                const text = await fs.readFile(fullPath, 'utf-8');
                if (text.length < 20) continue;

                const chunks = [];
                for (let i = 0; i < text.length; i += (CONFIG.chunkSize - CONFIG.chunkOverlap)) {
                    chunks.push(text.slice(i, i + CONFIG.chunkSize));
                }

                console.log(`📄 [INDEXING] ${relPath} (${chunks.length} chunks)`);

                // Batch process chunks for speed on your GTX 1080
                const vectors = await getEmbeddings(chunks);
                const points = vectors.map((vector, i) => ({
                    id: toUUID(crypto.createHash('md5').update(`${relPath}-${i}`).digest('hex')),
                    vector,
                    payload: { path: relPath, text: chunks[i] }
                }));

                await qdrant.upsert(CONFIG.collectionName, { wait: true, points });
            } catch (err) {
                console.error(`❌ Fatal Error on ${relPath}: ${err.message}`);
                process.exit(1);
            }
        }
    };

    await walk(CONFIG.rootPath);
    console.log("✅ Done.");
}

run();