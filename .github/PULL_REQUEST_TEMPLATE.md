## Description

Please include a summary of the changes and related context.

## Type of Change

- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to change)
- [ ] Documentation update

## Changes

- [ ] Code changes
- [ ] Tests added or updated
- [ ] Documentation updated
- [ ] Updates graph baseline (if applicable)

## Graph Baseline Update

When code structure changes (new entities, DTOs, endpoints, or component reorganization), the API graph baseline must be updated:

```bash
npm run graph:update
git add tools/graph/graph-baseline.json
```

See `tools/graph/README.md` for detailed guidance on when updates are needed.

## Related Issues

Closes #

## Checklist

- [ ] My code follows the project's style guidelines
- [ ] I have performed a self-review of my own code
- [ ] I have commented my code, particularly in hard-to-understand areas
- [ ] I have made corresponding changes to the documentation
- [ ] My changes generate no new warnings
- [ ] I have added tests that prove my fix is effective or that my feature works
- [ ] New and existing unit tests passed locally with my changes
