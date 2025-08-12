# Close Issue Command

Close a GitHub issue by committing all changes and marking the issue as Done in the project board.

## Usage

When a user says `/close-issue <issue-number> [custom-commit-message]`, Claude should:

1. **Check git status**: Show all uncommitted changes to the user
2. **Get issue details**: Read issue title and description for context
3. **Generate commit message**: Create a descriptive commit message based on issue
4. **Show commit preview**: Display what will be committed and the proposed message
5. **Get user confirmation**: Wait for user approval before proceeding
6. **Commit changes**: Stage all changes and create commit with generated message
7. **Mark issue as Done**: Update project board status to "Done"

## Commit Message Format

```
{issue-type}: {sanitized-issue-title}

- {bullet-point summary of key changes}
- {additional changes if applicable}

Closes #{issue-number}

🤖 Generated with Claude Code (https://claude.ai/code)

Co-Authored-By: Claude <noreply@anthropic.com>
```

## Commands to Execute

```bash
# 1. Check git status
git status

# 2. Show git diff of changes
git diff

# 3. Read issue details for context
gh issue view {issue-number} --repo jcucci/azstore

# 4. After user confirms, stage all changes
git add .

# 5. Create commit with generated message
git commit -m "$(cat <<'EOF'
{generated-commit-message}
EOF
)"

# 6. Mark issue as Done in project board
gh project item-edit \
  --id $(gh project item-list 1 --owner jcucci --format json | jq -r '.items[] | select(.content.number == {issue-number}) | .id') \
  --field-id $(gh project field-list 1 --owner jcucci --format json | jq -r '.fields[] | select(.name == "Status") | .id') \
  --single-select-option-id $(gh project field-list 1 --owner jcucci --format json | jq -r '.fields[] | select(.name == "Status") | .options[] | select(.name == "Done") | .id') \
  --project-id $(gh project list --owner jcucci --format json | jq -r '.projects[] | select(.title == "azstore") | .id')
```

## Issue Type Detection

Based on issue title/content, determine commit type:
- **feat**: New features or major functionality
- **init**: Initial setup, project structure, scaffolding
- **fix**: Bug fixes
- **refactor**: Code refactoring without functional changes
- **docs**: Documentation updates
- **test**: Adding or updating tests
- **chore**: Maintenance, dependency updates, build changes

## Example Flow

```
User: /close-issue 1
Claude: I'll prepare to close issue #1. Let me check the current changes...

[Shows git status and diff]
[Shows issue details]
[Shows proposed commit message]

Claude: Here's what will be committed:
- 12 files changed: 8 new project files, 4 configuration files
- Proposed commit message: "init: Initialize solution structure with projects..."

Do you want to proceed with this commit and mark issue #1 as Done? (y/n)

User: y
Claude: [Commits changes and updates project board]
Issue #1 has been successfully closed and marked as Done!
```

## Safety Features

- Always show changes before committing
- Require explicit user confirmation
- Generate descriptive commit messages
- Include issue reference for traceability
- Validate that there are actually changes to commit
- Handle cases where issue is already closed