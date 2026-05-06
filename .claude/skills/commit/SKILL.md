---
name: commit
description: Create a conventional commit for staged and unstaged changes
user-invocable: true
---

# Commit Skill

## 1. Gather context
Run these in parallel:
- `git status` (never use `-uall`)
- `git diff` and `git diff --cached` to see all changes
- `git log --oneline -10` to match existing commit style

## 2. Stage files
- Stage relevant files by name — never use `git add -A` or `git add .`
- Never stage files that may contain secrets (`.env`, tokens, credentials)

## 3. Propose commits
- If changes span multiple logical units, propose splitting into separate commits
- Show the user a numbered list of proposed commits with: message, files, and rationale
- Wait for user approval before proceeding

## 4. Write a conventional commit message

Format: `<type>: <short description>`

**Types:**
- `feat` — new feature or capability
- `fix` — bug fix
- `refactor` — code change that neither fixes a bug nor adds a feature
- `docs` — documentation only
- `style` — formatting, whitespace, no code change
- `test` — adding or updating tests
- `chore` — build config, dependencies, tooling
- `perf` — performance improvement
- `ci` — CI/CD pipeline changes

**Rules:**
- Subject line ONLY — no body, no `Co-Authored-By` trailer, nothing else
- Subject line under 72 characters
- Imperative mood ("add", not "added" or "adds")
- No period at the end of the subject
- Scopes are not used in this repo — match the existing `git log --oneline` style

## 5. Commit
```bash
git commit -m "type: subject line here"
```

## 6. Verify
Run `git status` after committing to confirm success.

## Important
- NEVER amend unless explicitly asked — always create new commits
- NEVER push unless explicitly asked
- If a pre-commit hook fails, fix the issue and create a NEW commit (do not amend)
