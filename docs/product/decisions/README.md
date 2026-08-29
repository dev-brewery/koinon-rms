# Product and refinement decisions

Durable product-management decisions are part of the standards corpus. They are indexed into the existing `koinon-standards` Qdrant collection with `doc_type=product-decision`; they do **not** get a separate collection.

Use this directory for decisions that constrain future feature implementation but are not code-style rules:

- agreed product behavior;
- UX/refinement decisions;
- feature-scope boundaries;
- owner-approved tradeoffs;
- structural product decisions that implementation agents must not rediscover or reverse.

Do **not** use this directory for transient notes, task progress, raw meeting logs, or bug lessons. Those go to GitHub issues or `lesson_add` depending on whether they are work tracking or experiential knowledge.

## Deterministic write/update pathway

1. Create or edit one markdown file named `PD-####-short-title.md` using `template.md`.
2. Set frontmatter:
   - `id`: stable `PD-####`;
   - `status`: `proposed`, `accepted`, `superseded`, or `rejected`;
   - `decision_type`: `product`, `refinement`, `structural`, or `ux`;
   - `applies_to`: bracket list of feature/component tags;
   - `date`: ISO date.
3. The owner or chief architect must explicitly agree before `status: accepted`.
4. If changing an accepted decision, either amend the same file with a dated update section or create a new decision with `supersedes: PD-####`; never silently replace history.
5. Run `npm run rag:index:standards` after accepted decision changes so `koinon-standards` is refreshed.
6. PRs that change this directory are protected standards changes and require a signed architecture review artifact.

## Required sections

Each decision must include:

- Context
- Decision
- Rationale
- Implementation implications
- Regression risks
- Related standards / ADRs
