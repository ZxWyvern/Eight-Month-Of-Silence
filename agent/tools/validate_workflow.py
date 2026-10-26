#!/usr/bin/env python3
"""Read-only EOS workflow checks. Standard library only; never runs Unity or MCP."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import sys


PHASES = [
    "INTAKE", "CLARIFY", "PLAN", "ARCHITECTURE_CHECK", "IMPLEMENT",
    "SELF_REVIEW", "TEST", "DOCUMENT", "HANDOFF",
]
CHECK_STATES = {"PASS", "FAIL", "BLOCKED", "NOT_RUN", "NOT_APPLICABLE"}


def under(root, relative):
    """Only inspect named files inside an explicit root, including symlink targets."""
    path = Path(relative)
    if path.is_absolute() or ".." in path.parts:
        raise ValueError("Unsafe relative path: " + str(relative))
    result = (root / path).resolve()
    if result != root and root not in result.parents:
        raise ValueError("Path escapes root: " + str(relative))
    return result


def read_json(root, relative):
    return json.loads(under(root, relative).read_text(encoding="utf-8-sig"))


def string_list(value):
    return isinstance(value, list) and bool(value) and all(
        isinstance(item, str) and item.strip() for item in value
    )


def frontmatter(path):
    """Parse the flat scalar frontmatter used by this bundle, not arbitrary YAML."""
    text = path.read_text(encoding="utf-8-sig")
    parts = text.split("---", 2)
    if len(parts) != 3 or parts[0].strip():
        raise ValueError("Missing frontmatter: " + str(path))
    data = {}
    for line in parts[1].splitlines():
        if not line.strip():
            continue
        key, separator, value = line.partition(":")
        if not separator or key.strip() in data:
            raise ValueError("Invalid/duplicate flat frontmatter: " + str(path))
        data[key.strip()] = value.strip().strip("\"'")
    return data, parts[2]


def structure(root, report):
    errors, warnings = report["errors"], report["warnings"]
    workflow = read_json(root, "agent/workflow.json")
    rules = read_json(root, "agent/rules.json")
    baseline = read_json(root, "agent/source-baseline.json")
    if workflow.get("format") != "eos-agent-workflow":
        errors.append("Invalid workflow format.")
    if workflow.get("version") != "2.0.0" or rules.get("version") != "2.0.0":
        errors.append("Workflow/rule version mismatch.")
    if workflow.get("entry_point") != "AGENTS.md":
        errors.append("The project contract must remain the entry point.")
    phases = workflow.get("phases", [])
    ids = [phase.get("id") for phase in phases]
    if ids != PHASES:
        errors.append("All nine phases must appear once in the required order.")
    rule_list = rules.get("rules", [])
    rule_ids = [rule.get("id") for rule in rule_list]
    if not rule_ids or len(set(rule_ids)) != len(rule_ids):
        errors.append("Rule IDs must exist and be unique.")
    for rule in rule_list:
        if not isinstance(rule.get("id"), str) or not re.fullmatch(
            r"[A-Z]+-\d{3}", rule.get("id", "")
        ):
            errors.append("Invalid rule ID: " + str(rule.get("id")))
        if rule.get("severity") not in {"blocker", "major", "minor"}:
            errors.append("Invalid severity: " + str(rule.get("id")))
        if not rule.get("description") or not rule.get("verification"):
            errors.append("Missing rule description/verification: " + str(rule.get("id")))
        if "Assets/_Project" in str(rule.get("scope", "")):
            errors.append("Stale source scope: " + str(rule.get("id")))
        if "match_regex" in rule:
            re.compile(rule["match_regex"])
    for i, phase in enumerate(phases):
        for field in ("entry_criteria", "actions", "exit_criteria", "rule_ids"):
            if not string_list(phase.get(field)):
                errors.append(str(phase.get("id")) + ": invalid " + field)
        for rule_id in phase.get("rule_ids", []):
            if rule_id not in rule_ids:
                errors.append(str(phase.get("id")) + ": unknown rule " + rule_id)
        if i < len(PHASES) - 1:
            if phase.get("next") != PHASES[i + 1] or phase.get("terminal"):
                errors.append(str(phase.get("id")) + ": wrong next phase.")
        elif phase.get("terminal") is not True or "next" in phase:
            errors.append("HANDOFF must be terminal.")
        if "on_failure" in phase and phase["on_failure"] not in PHASES:
            errors.append("Unknown failure transition.")
        if phase.get("on_blocked", {}).get("task_state") != "BLOCKED":
            errors.append(str(phase.get("id")) + ": missing blocked behavior.")
    if set(workflow.get("check_states", [])) != CHECK_STATES:
        errors.append("Evidence states are incomplete.")
    if "DONE" in workflow.get("task_states", []):
        errors.append("DONE must remain human-only.")
    if workflow.get("human_only_task_states") != ["DONE"]:
        errors.append("Human acceptance ownership is missing.")
    expected_reading = [
        "AGENTS.md", "docs/GDD.md", "docs/ARCHITECTURE.md",
        "docs/CODING_STANDARDS.md", "docs/DEFINITION_OF_DONE.md",
        "agent/workflow.json", "agent/rules.json", "docs/DECISIONS.md",
    ]
    if workflow.get("required_reading", [])[:8] != expected_reading:
        errors.append("Required read-order prefix does not match AGENTS.md.")
    required_files = ["AGENTS.md", "README.md", "agent/rules.json"]
    required_files += list(workflow.get("artifacts", {}).values())
    required_files += [workflow.get("enforcement", {}).get("validator", "")]
    required_files += ["agent/legacy-v1/" + name for name in (
        "workflow.json", "rules.json", "task-template.md"
    )]
    for relative in required_files:
        if not relative or not under(root, relative).is_file():
            errors.append("Missing bundle file: " + relative)
    agents_path = under(root, "AGENTS.md")
    if agents_path.is_file() and hashlib.sha256(agents_path.read_bytes()).hexdigest() != baseline.get("agents_sha256"):
        warnings.append("AGENTS.md differs from the reviewed contract; reread and reconcile current policy.")
    adapter, _ = frontmatter(under(root, ".opencode/agents/eos-workflow.md"))
    command, body = frontmatter(under(root, ".opencode/commands/eos-work.md"))
    if adapter.get("mode") != "primary" or not adapter.get("description"):
        errors.append("Invalid OpenCode agent frontmatter.")
    if set(adapter) - {"description", "mode"}:
        errors.append("Unexpected adapter override; review model/permission changes.")
    if command.get("agent") != "eos-workflow" or not command.get("description") or "$ARGUMENTS" not in body:
        errors.append("Invalid command-to-agent linkage or missing arguments.")
    report["workflow"] = {"phases": len(phases), "rules": len(rule_ids), "bundle_files_checked": len(required_files)}
    return baseline


def source_checks(project, baseline, report):
    errors, warnings = report["errors"], report["warnings"]
    candidates = [under(project, p) for p in baseline["source_root_candidates"]]
    roots = [p for p in candidates if p.is_dir()]
    if len(roots) != 1:
        report["source_check_status"] = "BLOCKED"
        report["blockers"].append("Expected exactly one source root; found " + str(len(roots)) + ". Use --project for the intended project or --structure-only for bundle-only checks.")
        return
    source = roots[0]
    report["source_root"] = str(source)
    missing, changed = [], []
    for anchor in baseline["source_anchors"]:
        path = under(source, anchor["path"])
        if not path.is_file():
            missing.append(anchor["path"])
        elif hashlib.sha256(path.read_bytes()).hexdigest() != anchor["sha256"]:
            changed.append(anchor["path"])
    if missing:
        warnings.append("Source anchors missing/renamed; refresh affected code-map claims.")
    if changed:
        warnings.append("Source changed since review; previous observations need rechecking.")
    report["source_freshness"] = {"reviewed_anchors": len(baseline["source_anchors"]), "missing": missing, "changed": changed}
    missing_context = [p for p in baseline["required_context"] if not under(project, p).is_file()]
    missing_markers = [p for p in baseline["project_markers"] if not under(project, p).is_file()]
    report["missing_context"] = missing_context
    report["missing_unity_project_markers"] = missing_markers
    if missing_context:
        warnings.append("Required context files are absent; resolve before a dependent project task.")
    if missing_markers:
        warnings.append("Partial Unity snapshot: editor/package/build facts and runtime verification remain unavailable.")
    assemblies, guids = {}, {}
    for path in sorted(source.rglob("*.asmdef")):
        data = json.loads(path.read_text(encoding="utf-8-sig"))
        name = data["name"]
        if name in assemblies:
            errors.append("Duplicate assembly name: " + name)
        assemblies[name] = data
        meta = path.with_name(path.name + ".meta")
        if meta.is_file():
            match = re.search(r"^guid:\s*(\S+)", meta.read_text(), re.M)
            if match:
                guids[match.group(1)] = name
    graph = {}
    for name, data in assemblies.items():
        refs = data.get("references", [])
        if not isinstance(refs, list) or not all(isinstance(r, str) for r in refs):
            errors.append("Invalid assembly references: " + name)
            continue
        normalized = [guids.get(r[5:], r) if r.startswith("GUID:") else r for r in refs]
        graph[name] = [r for r in normalized if r in assemblies]
        unresolved = [r for r in normalized if r.startswith("GUID:")]
        if unresolved:
            warnings.append(name + ": unresolved GUID references; full project resolution required.")
        if name in ("Yumiwa.Domain", "Yumiwa.Application"):
            if data.get("noEngineReferences") is not True:
                errors.append(name + " must remain engine-free.")
            allowed = set() if name == "Yumiwa.Domain" else {"Yumiwa.Domain"}
            if set(normalized) - allowed:
                errors.append(name + ": unexpected assembly boundary change.")
    for required in ("Yumiwa.Domain", "Yumiwa.Application", "Yumiwa.Infrastructure", "Yumiwa.Presentation"):
        if required not in assemblies:
            errors.append("Missing expected runtime assembly: " + required)
    visiting, visited = set(), set()

    def visit(name):
        if name in visiting:
            errors.append("Assembly cycle detected at " + name)
            return
        if name in visited:
            return
        visiting.add(name)
        for dependency in graph.get(name, []):
            visit(dependency)
        visiting.remove(name)
        visited.add(name)

    for name in graph:
        visit(name)
    report["assembly_graph"] = graph
    report["source_check_status"] = "FAIL" if errors else "PASS"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", default=".", help="Workflow bundle or installed project root")
    parser.add_argument("--project", help="Optional separate project/export root to inspect")
    parser.add_argument("--structure-only", action="store_true", help="Explicitly omit all source/project checks")
    args = parser.parse_args()
    root = Path(args.root).resolve()
    report = {
        "static_status": "NOT_RUN", "source_check_status": "NOT_RUN",
        "errors": [], "warnings": [], "blockers": [],
        "not_verified": [
            "C# compilation and package/API compatibility",
            "Unity EditMode/PlayMode tests, actual mechanics, builds and performance",
            "OpenCode runtime loading, skill invocation, MCP connectivity and permissions",
            "Every semantic/manual rule, full C# dependency analysis and game completion",
        ],
    }
    try:
        baseline = structure(root, report)
        if not args.structure_only:
            project = Path(args.project).resolve() if args.project else root
            source_checks(project, baseline, report)
    except (OSError, ValueError, KeyError, TypeError, AttributeError, re.error) as exc:
        report["errors"].append(type(exc).__name__ + ": " + str(exc))
    report["static_status"] = "FAIL" if report["errors"] else "BLOCKED" if report["blockers"] else "PASS"
    print(json.dumps(report, ensure_ascii=False, indent=2))
    return 1 if report["errors"] else 2 if report["blockers"] else 0


if __name__ == "__main__":
    sys.exit(main())
