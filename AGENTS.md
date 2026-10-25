# AGENTS.md

## Project
Virtual Dark Store Work-Study & Simulation Platform.

## Core objective
Build a virtual/VR dark-store environment for automated work-study and work-sampling.
This is not an AI/ML prediction product.

## Non-negotiable scope
- Do NOT add AI/ML unless explicitly requested.
- Do NOT create a training dataset pipeline.
- Do NOT invent real-world data.
- Do NOT add Python unless explicitly justified and approved.
- Desktop mode must work before VR.
- Desktop and VR must share the same simulation and WorkStudy core.
- Do not claim eye-gaze, fatigue or physiological measurement without actual instrumentation.

## No-hallucination
- Inspect the repository before changing it.
- Inspect packages/project settings before assuming they exist.
- Prefer official Unity documentation for Unity-specific claims.
- If uncertain, say NOT VERIFIED.
- Never fabricate file paths, APIs, package versions, test results or screenshots.
- Never claim a resource is integrated unless files actually exist.
- Never claim a test/build passed unless it was actually run.

## Change discipline
1. Inspect.
2. Plan.
3. Implement the smallest coherent change.
4. Verify.
5. Update documentation.
6. Report changed files and evidence.

## Documentation
After every phase update:
- README.md
- docs/PHASE_STATUS.md
- docs/DEVELOPMENT_LOG.md
- docs/ARCHITECTURE.md if changed
- docs/DECISIONS.md if changed
- docs/TESTING.md if changed
- CHANGELOG.md at milestone

## Time/version tracking
Every log entry must include:
Date, Phase, Goal, Changes, Files changed, Commands/tools, Verification, Result, Known issues, Next step.
Use actual environment date/time; never invent it.

## Stop conditions
Stop and ask if requirements conflict, destructive changes are proposed, a license/dependency is unclear, an API cannot be verified, or scope would materially expand.

## Git
Use small logical commits. Do not force-push or rewrite history unless explicitly instructed.

