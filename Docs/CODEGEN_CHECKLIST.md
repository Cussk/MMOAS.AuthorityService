# Code Generation Checklist

Before generating code for a phase:

1. Read AGENTS.md
2. Read current phase document
3. Explain:
  - what is being built
  - which layer owns it
  - what state it reads/writes
  - any threading or timing implications
4. Confirm no architecture boundary violations
5. Then generate compile-ready code

---

## Generation standards
- compile-ready only
- avoid placeholder pseudocode
- use DI-friendly construction
- use interfaces for state stores
- prefer explicit DTOs/contracts
- hosted services must be cancellation-safe
- use thread-safe state containers
- add meaningful educational comments where useful
- keep scope strictly inside current phase