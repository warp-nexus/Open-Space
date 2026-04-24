# GitHub Copilot Instructions

Follow `AGENTS.md` as the primary repository instruction file for this SS14 fork.

Before editing:

- Read `.agents/rules/ss14-skill-preflight-and-refresh.md`
- Read `.agents/rules/ss14-interaction-flow.md`
- Read `.agents/rules/ss14-engine-boundaries.md`
- Read `.agents/rules/ss14-localization-required.md`
- Read `.agents/rules/ss14-csharp-style.md`
- Read `.agents/rules/ss14-testing-and-validation.md`
- Read every relevant skill under `.agents/skills/`
- Apply matching `.github/instructions/*.instructions.md` files when the touched path has a narrower guide

Always load these skills:

- `ss14-naming-conventions`
- `ss14-ecs-prototypes`
- `ss14-upstream-maintenance`

For C# gameplay work also load:

- `ss14-ecs-components`
- `ss14-ecs-entities`
- `ss14-ecs-systems`
- `ss14-events`
- `ss14-prediction`

For player-facing text also load:

- `ss14-localization-strings`
- `ss14-localization-code` when C# text or `LocId` fields change

For hot paths or `Update()` also load:

- `ss14-standard-optimizations`

For XAML or BUI work also load:

- `ss14-ui-bui`

For network events, `NetEntity`, or replicated state also load:

- `ss14-netcode`

For `Appearance` and `GenericVisualizer` work also load:

- `ss14-graphics-generic-visualizer-appearance`

For test authoring or coverage selection also load:

- `ss14-tests-authoring`

Repository reminders:

- Keep components data-only and put behavior in systems.
- Use `On... -> Try... -> Can... -> Do...` interaction flow.
- Prefer `Entity<T?>`, `ProtoId<T>`, and localized strings.
- Avoid `RobustToolbox/` edits unless engine work is explicitly required.
- Run the smallest meaningful validation for the touched files.
