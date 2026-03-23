---
name: TextView is fragile - don't change
description: TextView EndInit ordering is fragile - changes cause cascading test failures in non-parallel UnitTests. Avoid modifying TextView.
type: feedback
---

Do not change TextView's EndInit ordering or initialization flow. Moving `base.EndInit()` before `UpdateContentSize()`/`UpdateScrollBars()` fixes some tests but breaks others in the non-parallel UnitTests project.

**Why:** TextView has complex initialization dependencies and the non-parallel tests rely on specific ordering. The user explicitly said "TextView is fragile."

**How to apply:** If TextView ContentSize tests fail, note the root cause (UpdateContentSize runs before IsInitialized is set) but do NOT fix it by reordering EndInit. File a separate issue or let the user decide when to address it.
