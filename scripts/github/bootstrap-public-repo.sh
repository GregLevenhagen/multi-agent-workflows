#!/usr/bin/env bash

set -euo pipefail

OWNER="${OWNER:-greglevenhagen}"
REPO="${REPO:-multi-agent-workflows}"
DESCRIPTION="${DESCRIPTION:-Open-source C# reference project for multi-agent workflows on Azure AI Foundry.}"
HOMEPAGE="${HOMEPAGE:-}"
SOURCE_DIR="${SOURCE_DIR:-/tmp/multi-agent-workflows-public}"
VISIBILITY="${VISIBILITY:-public}"
DEFAULT_BRANCH="${DEFAULT_BRANCH:-main}"
TOPICS="${TOPICS:-dotnet,csharp,azure,ai-foundry,multi-agent,opentelemetry,bicep}"
API_VERSION="${API_VERSION:-2026-03-10}"

required_tools=(gh git)
for tool in "${required_tools[@]}"; do
  if ! command -v "$tool" >/dev/null 2>&1; then
    echo "Missing required tool: $tool" >&2
    exit 1
  fi
done

if [[ ! -d "$SOURCE_DIR/.git" ]]; then
  echo "SOURCE_DIR must point to the fresh-history publish repo. Missing: $SOURCE_DIR/.git" >&2
  exit 1
fi

if ! gh auth status >/dev/null 2>&1; then
  echo "GitHub CLI is not authenticated. Run 'gh auth login -h github.com' and rerun this script." >&2
  exit 1
fi

if gh repo view "$OWNER/$REPO" >/dev/null 2>&1; then
  echo "Repository $OWNER/$REPO already exists." >&2
else
  gh repo create "$OWNER/$REPO" \
    --"$VISIBILITY" \
    --source "$SOURCE_DIR" \
    --remote origin \
    --push \
    --description "$DESCRIPTION"
fi

if [[ -n "$HOMEPAGE" ]]; then
  homepage_flag=(--field "homepage=$HOMEPAGE")
else
  homepage_flag=()
fi

IFS=',' read -r -a topic_array <<<"$TOPICS"
topic_json=""
for topic in "${topic_array[@]}"; do
  if [[ -n "$topic_json" ]]; then
    topic_json+=", "
  fi
  topic_json+="\"$topic\""
done

gh api \
  --method PATCH \
  -H "Accept: application/vnd.github+json" \
  -H "X-GitHub-Api-Version: $API_VERSION" \
  "repos/$OWNER/$REPO" \
  --field "name=$REPO" \
  --field "description=$DESCRIPTION" \
  "${homepage_flag[@]}" \
  --field "has_issues=true" \
  --field "has_projects=false" \
  --field "has_wiki=false" \
  --field "allow_squash_merge=true" \
  --field "allow_merge_commit=false" \
  --field "allow_rebase_merge=false" \
  --field "delete_branch_on_merge=true" \
  --field "allow_auto_merge=true" \
  --field "web_commit_signoff_required=false" \
  --raw-field "security_and_analysis[secret_scanning][status]=enabled" \
  --raw-field "security_and_analysis[secret_scanning_push_protection][status]=enabled"

gh api \
  --method PUT \
  -H "Accept: application/vnd.github+json" \
  -H "X-GitHub-Api-Version: $API_VERSION" \
  "repos/$OWNER/$REPO/vulnerability-alerts" >/dev/null

gh api \
  --method PUT \
  -H "Accept: application/vnd.github+json" \
  -H "X-GitHub-Api-Version: $API_VERSION" \
  "repos/$OWNER/$REPO/automated-security-fixes" >/dev/null

gh api \
  --method PUT \
  -H "Accept: application/vnd.github+json" \
  -H "X-GitHub-Api-Version: $API_VERSION" \
  "repos/$OWNER/$REPO/topics" \
  --input - <<EOF
{
  "names": [ $topic_json ]
}
EOF

ruleset_payload="$(mktemp)"
cat >"$ruleset_payload" <<EOF
{
  "name": "Protect ${DEFAULT_BRANCH}",
  "target": "branch",
  "enforcement": "active",
  "bypass_actors": [
    {
      "actor_id": 1,
      "actor_type": "RepositoryRole",
      "bypass_mode": "always"
    }
  ],
  "conditions": {
    "ref_name": {
      "include": ["refs/heads/${DEFAULT_BRANCH}"],
      "exclude": []
    }
  },
  "rules": [
    { "type": "deletion" },
    { "type": "non_fast_forward" },
    { "type": "required_linear_history" },
    {
      "type": "pull_request",
      "parameters": {
        "allowed_merge_methods": ["squash"],
        "dismiss_stale_reviews_on_push": true,
        "require_code_owner_review": false,
        "require_last_push_approval": false,
        "required_approving_review_count": 0,
        "required_review_thread_resolution": true
      }
    },
    {
      "type": "required_status_checks",
      "parameters": {
        "strict_required_status_checks_policy": true,
        "do_not_enforce_on_create": false,
        "required_status_checks": [
          { "context": "CI / build-test" },
          { "context": "CodeQL / analyze (csharp)" }
        ]
      }
    }
  ]
}
EOF

existing_ruleset_id="$(
  gh api \
    -H "Accept: application/vnd.github+json" \
    -H "X-GitHub-Api-Version: $API_VERSION" \
    "repos/$OWNER/$REPO/rulesets" \
    --jq '.[] | select(.name == "Protect '"${DEFAULT_BRANCH}"'") | .id' || true
)"

if [[ -n "$existing_ruleset_id" ]]; then
  gh api \
    --method PUT \
    -H "Accept: application/vnd.github+json" \
    -H "X-GitHub-Api-Version: $API_VERSION" \
    "repos/$OWNER/$REPO/rulesets/$existing_ruleset_id" \
    --input "$ruleset_payload" >/dev/null
else
  gh api \
    --method POST \
    -H "Accept: application/vnd.github+json" \
    -H "X-GitHub-Api-Version: $API_VERSION" \
    "repos/$OWNER/$REPO/rulesets" \
    --input "$ruleset_payload" >/dev/null
fi

rm -f "$ruleset_payload"

git -C "$SOURCE_DIR" remote get-url origin >/dev/null 2>&1 || git -C "$SOURCE_DIR" remote add origin "https://github.com/$OWNER/$REPO.git"
git -C "$SOURCE_DIR" push -u origin "$DEFAULT_BRANCH"

echo "Repository configured: https://github.com/$OWNER/$REPO"
