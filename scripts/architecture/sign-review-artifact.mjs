#!/usr/bin/env node
// Sign an already-formed architecture review artifact. The private key must live
// outside the repository. This script only appends/replaces agentSignature.

import { createHash, createPrivateKey, createPublicKey, sign } from 'node:crypto';
import { readFileSync, writeFileSync } from 'node:fs';
import { canonicalJson, publicKeyFingerprint, sha256Hex } from './verify-review-artifact.mjs';

function parseArgs(argv) {
  const opts = {};
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--artifact') opts.artifact = argv[++i];
    else if (a === '--key') opts.key = argv[++i];
    else if (a === '--key-id') opts.keyId = argv[++i];
    else throw new Error(`Unknown argument: ${a}`);
  }
  if (!opts.artifact || !opts.key || !opts.keyId) {
    throw new Error('Usage: sign-review-artifact.mjs --artifact <json> --key <private.pem> --key-id <id>');
  }
  return opts;
}

const opts = parseArgs(process.argv.slice(2));
const artifact = JSON.parse(readFileSync(opts.artifact, 'utf8'));
delete artifact.agentSignature;
const privateKey = createPrivateKey(readFileSync(opts.key, 'utf8'));
const publicPem = createPublicKey(privateKey).export({ type: 'spki', format: 'pem' });
const payload = canonicalJson(artifact);
artifact.agentSignature = {
  keyId: opts.keyId,
  algorithm: 'ed25519',
  publicKeySha256: publicKeyFingerprint(publicPem),
  signedPayloadSha256: sha256Hex(Buffer.from(payload)),
  signature: sign(null, Buffer.from(payload), privateKey).toString('base64'),
};
writeFileSync(opts.artifact, JSON.stringify(artifact, null, 2) + '\n');
console.log(`signed ${opts.artifact}`);
console.log(`payload ${artifact.agentSignature.signedPayloadSha256}`);
