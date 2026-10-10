# Directives

## Inconsistency Verification & Pushback
If you notice an inconsistency or potential conflict in any statement or requirement that can be verified against active code, scene files, or architectural rules, **do not attempt to blindly accommodate it or force it to work**. Push back with verifiable evidence, explain the conflict clearly, and ask for clarification.

## Synchronization Target
Always automatically sync code changes to Unity (`powershell -ExecutionPolicy Bypass -File .\personal-tools\sync.ps1 src unity`) and **not** Playground when making changes.
