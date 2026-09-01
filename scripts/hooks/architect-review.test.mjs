import test from 'node:test';
import assert from 'node:assert/strict';
import { buildUserContent, retrievalQuery } from './architect-review.mjs';

test('mandates are excluded from the standards retrieval query', () => {
  const query = retrievalQuery('focused diagnosis', 'focused proposal');
  assert.equal(query, 'focused diagnosis\nfocused proposal');
  assert.doesNotMatch(query, /mandate/i);
});

test('compact mandates appear once in architect context', () => {
  const content = buildUserContent({
    fileHashes: [{ path: 'example.py', hash: 'abc' }],
    deduced: 'diagnosis',
    proposed: 'proposal',
    mandates: 'UNIQUE_CANONICAL_MANDATE',
    criteria: 'none',
    standards: [{ score: 0.9, payload: { path: 'docs/reference/x.md', section: 'Rule', content: 'standard' } }],
    lessons: [],
  });
  assert.equal(content.match(/UNIQUE_CANONICAL_MANDATE/g)?.length, 1);
  assert.match(content, /## Dev-cycle mandates/);
});
