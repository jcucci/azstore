# Init Issue Command

Initialize work on a GitHub issue by marking it as in progress and creating a feature branch.

## Usage

When a user says `/init-issue <issue-number> [branch-name]`, Claude should:

1. **Mark issue as in progress**: Use `gh issue edit <issue-number> --repo jcucci/azstore --add-label "in progress"`
2. **Read issue into context**: Use `gh issue view <issue-number> --repo jcucci/azstore` to get full issue details
3. **Create feature branch**: 
   - If branch name provided: use it
   - If not provided: generate from issue title using format `azs-{number}-{sanitized-title}`
   - Checkout main, pull latest, create new branch

## Branch Naming Convention

- Remove "Phase X:" prefixes from issue titles
- Convert to lowercase
- Replace spaces/special chars with hyphens
- Limit to 50 characters
- Examples:
  - "Phase 1: Initialize solution structure" → `azs-1-initialize-solution-structure`
  - "Implement Azure CLI authentication" → `azs-6-implement-azure-cli-authentication`

## Commands to Execute

```bash
# 1. Mark as in progress. The gh specific fields have been 
gh project item-edit \
  --id $(gh project item-list 1 --owner jcucci --format json | jq -r '.items[] | select(.content.number == {issue-number}) | .id') \
  --field-id $(gh project field-list 1 --owner jcucci --format json | jq -r '.fields[] | select(.name == "Status") | .id') \
  --single-select-option-id $(gh project field-list 1 --owner jcucci --format json | jq -r '.fields[] | select(.name == "Status") | .options[] | select(.name == "In Progress") | .id') \
  --project-id $(gh project list --owner jcucci --format json | jq -r '.projects[] | select(.title == "azstore") | .id')

# 2. Read issue details
gh issue view {issue-number} --repo jcucci/azstore

# 3. Create branch
git checkout main
git pull origin main  
git checkout -b {branch-name}
```

## Example Usage

```
User: /init-issue 1
Claude: I'll initialize work on issue #1...
[executes commands and shows issue details]

User: /init-issue 5 feature/auth-service
Claude: I'll initialize work on issue #5 with custom branch name...
[executes commands with custom branch name]
```