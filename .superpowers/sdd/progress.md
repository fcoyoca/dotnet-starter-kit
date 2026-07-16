# Claims module — SDD progress ledger

Plan: docs/superpowers/plans/2026-07-15-claims-billing-module.md
Branch: clinic-app  (base for review-package: record each task's pre-dispatch HEAD)

- Task 1: complete (commits 88a066ff..724db470, review clean) — Scaffold Contracts project (marker, permissions, status)
- Task 2: complete (commits 724db470..feccc9a6, review clean) — Claim aggregate + lifecycle (TDD)
- Task 3: complete (commits feccc9a6..7f3ce968, review clean; ArchTests 51/51) — Data layer + 4-place registration + migration
- Task 4: complete (commits 7f3ce968..555771c7, review clean) — Contracts DTOs/commands/queries/submitter iface
- Task 5: complete (commits 83ea6ebc..6c6efbb7, incl. fix round; review clean; 13/13 tests) — event handler + freeze + tenant-isolation test
- Task 6: complete (commits 6c6efbb7..324d590a, review clean; 14/14 tests) — Stub submitter + DI
- Task 7: pending — Transition commands + validators + endpoints
- Task 8: pending — Query handlers (GetClaims + GetClaimById)
- Task 9: pending — Frontend api/claims.ts
- Task 10: pending — Worklist page + nav + route
- Task 11: pending — Claim detail page
- Task 12: pending — Playwright e2e
- Task 13: DEFERRED — docs+changelog (separate fullstackhero/docs repo)

## Minor findings roll-up (for final review)
(none yet)

### Minor findings roll-up (updated)
- [Task 2] ClaimLine.DiagnosticIds returns naked `_diagnosticIds` (=> field) instead of `.AsReadOnly()` like SuperBillProcedure — castable back to List<Guid> at runtime. Trivial fix. (ClaimLine.cs)
- [Task 2] No test for MarkDenied() transition, and no test for double-Void() idempotent branch. Low risk; consider adding.

### Cross-task interface notes
- ClaimLine.DiagnosticIds is `IReadOnlyList<Guid>` backed by private field `_diagnosticIds` (NOT a public List<Guid> as the plan text showed). Task 3 EF `PrimitiveCollection` mapping must map the backing field — verify the migration emits a uuid[] column for DiagnosticIds; add `.HasField("_diagnosticIds")` if EF doesn't auto-detect.

### CRITICAL carry-forward (Task 3 → Tasks 4/5)
- Mediator MSG0007: the two `o.Assemblies` marker lines (ClaimsContractsMarker + ClaimsModule) were DEFERRED in BOTH src/Host/FSH.Starter.Api/Program.cs and src/Host/FSH.Starter.DbMigrator/Program.cs (they don't compile until the assembly has a Mediator type). `moduleAssemblies` entry IS present.
  * Task 4 (adds Contracts ICommand/IQuery): re-add `typeof(FSH.Modules.Claims.Contracts.ClaimsContractsMarker)` to o.Assemblies in BOTH Program.cs; verify `dotnet build src/FSH.Starter.slnx`.
  * Task 5 (adds first handler in runtime asm): re-add `typeof(FSH.Modules.Claims.ClaimsModule)` to o.Assemblies in BOTH Program.cs; verify build. Without these markers, handlers are SILENTLY undiscovered at runtime.
- Task 6 still owns adding the IClaimSubmitter DI line (not added in Task 3).
- Unique index is compound (TenantId, SuperBillId) ux_claims_superbill — tenant-scoped, intended.

## ============================================================
## RESUME HERE (paused 2026-07-15 — user out of session budget)
## ============================================================
State: Tasks 1–3 complete & reviewed clean. Task 4 IMPLEMENTED + committed (555771c7)
but its TASK REVIEW has NOT run yet. Working tree clean; all code committed on branch clinic-app.

NEXT STEP on resume — review Task 4, then continue the SDD loop:
1. Re-read this ledger + `git log --oneline -8` to confirm state (trust ledger+git over memory).
2. Generate Task 4 review package:
   bash <SKILL>/scripts/review-package 7f3ce9688b0c0e729ca2ca884f83baa207bbac7d HEAD
   (<SKILL> = C:/Users/fcoyo/.claude/plugins/cache/claude-plugins-official/superpowers/d884ae04edeb/skills/subagent-driven-development)
   Brief: .superpowers/sdd/task-4-brief.md ; Report: .superpowers/sdd/task-4-report.md
   Reviewer focus: 5 DTOs/5 commands/2 queries/IClaimSubmitter match brief; ClaimsContractsMarker
   re-added to o.Assemblies in BOTH Program.cs (Api + DbMigrator); ClaimsModule runtime marker
   still correctly deferred; build 0 warnings.
3. If clean → mark Task 4 complete in ledger, proceed to Task 5.

REMAINING TASKS: 5 (SuperBillSaved event handler — TDD), 6 (stub submitter+DI),
7 (transition commands+validators+endpoints), 8 (query handlers), 9 (frontend api),
10 (worklist page+nav+route), 11 (claim detail page), 12 (Playwright e2e),
then FINAL whole-branch review + superpowers:finishing-a-development-branch.
Task 13 (docs+changelog) is DEFERRED — separate github.com/fullstackhero/docs repo, not in this workspace.

STILL-OPEN CRITICAL CARRY-FORWARD:
- Task 5 MUST re-add `typeof(FSH.Modules.Claims.ClaimsModule),` to o.Assemblies in BOTH
  Program.cs (Api + DbMigrator) — it adds the first runtime Mediator handler; without this
  marker, handlers are SILENTLY undiscovered at runtime. (Contracts marker already re-added in Task 4.)
- Task 6 owns adding the IClaimSubmitter DI line in ClaimsModule.ConfigureServices.
- ClaimLine.DiagnosticIds is IReadOnlyList<Guid> backed by _diagnosticIds (mapped uuid[] via HasField).

SDD process reminders: fresh implementer subagent per task (model: sonnet), review after each
(model: sonnet), record base HEAD BEFORE dispatching each implementer, use scripts/task-brief for
each task's brief, update this ledger when each review comes back clean. Env: dev Postgres/Redis/MinIO
up in Docker; dotnet 10, node 24. Spec: docs/superpowers/specs/2026-07-15-claims-billing-module-design.md
Plan: docs/superpowers/plans/2026-07-15-claims-billing-module.md

### Minor findings roll-up (updated after Task 5)
- [Task 5] Handler <remarks>/null-guard comment says it "mirrors Billing's TenantSubscribedIntegrationEventHandler" — partial analogy (Billing's DbContext is NOT tenant-isolated). Cosmetic.
- [Task 5 / DOCS follow-up, Golden Rule #10] `.agents/rules/eventing.md` line ~37 says background handlers "must restore Finbuckle context first via IMultiTenantContextSetter" — now MISLEADING; the central FinbuckleEventTenantScope (opened in InMemoryEventBus before handler scope) satisfies this for all handlers. Update the rule to cite FinbuckleEventTenantScope / Billing's handler as the current pattern. (Out of scope for Task 5; do in docs pass / final.)
- Task 5 handler correctly relies on central tenant scope + fail-fast on null TenantId. Do NOT add WebhookFanoutHandler-style self-set (inert on an injected DbContext).
- [Task 6] StubClaimSubmitter control number uses 8 hex chars of Guid — stub-only, collision risk if ever promoted; determinism not test-asserted. No action.
