import argparse
import csv
import os
import re
from dataclasses import dataclass, field
from pathlib import Path
from typing import Iterable, List, Optional, Sequence, Set, Tuple


CS_PRIMITIVES: Set[str] = {
    "bool",
    "byte",
    "sbyte",
    "short",
    "ushort",
    "int",
    "uint",
    "long",
    "ulong",
    "float",
    "double",
    "decimal",
    "char",
    "string",
    "object",
    "void",
}


CS_KEYWORDS: Set[str] = {
    # common C# keywords/modifiers that can appear in member declarations
    "abstract",
    "add",
    "async",
    "await",
    "base",
    "break",
    "case",
    "catch",
    "checked",
    "class",
    "const",
    "continue",
    "default",
    "delegate",
    "do",
    "else",
    "enum",
    "event",
    "explicit",
    "extern",
    "false",
    "finally",
    "fixed",
    "for",
    "foreach",
    "get",
    "goto",
    "if",
    "implicit",
    "in",
    "interface",
    "internal",
    "is",
    "lock",
    "namespace",
    "new",
    "null",
    "operator",
    "out",
    "override",
    "params",
    "private",
    "protected",
    "public",
    "readonly",
    "ref",
    "remove",
    "return",
    "sealed",
    "set",
    "static",
    "struct",
    "switch",
    "this",
    "throw",
    "true",
    "try",
    "typeof",
    "unchecked",
    "unsafe",
    "using",
    "value",
    "var",
    "virtual",
    "void",
    "volatile",
    "where",
    "while",
    "yield",
}

GENERIC_CONTAINERS: Set[str] = {
    "List",
    "IList",
    "IReadOnlyList",
    "ICollection",
    "IReadOnlyCollection",
    "IEnumerable",
    "HashSet",
    "Dictionary",
    "IDictionary",
    "Queue",
    "Stack",
    "LinkedList",
    "SortedDictionary",
    "SortedList",
    "SortedSet",
    "ConcurrentDictionary",
}


@dataclass
class TypeDecl:
    kind: str  # class/struct/interface/enum etc in CSV
    name: str
    base_class: Optional[str] = None
    interfaces: List[str] = field(default_factory=list)
    composition: Set[str] = field(default_factory=set)
    aggregation: Set[str] = field(default_factory=set)
    association: Set[str] = field(default_factory=set)
    dependency: Set[str] = field(default_factory=set)


def strip_comments_and_strings(code: str) -> str:
    """Remove comments and string/char literals to make brace counting & regex safer."""
    out: List[str] = []
    i = 0
    n = len(code)
    state = "code"  # code | line_comment | block_comment | string | verbatim_string | char

    while i < n:
        c = code[i]
        nxt = code[i + 1] if i + 1 < n else ""

        if state == "code":
            if c == "/" and nxt == "/":
                state = "line_comment"
                out.append(" ")
                i += 2
                continue
            if c == "/" and nxt == "*":
                state = "block_comment"
                out.append(" ")
                i += 2
                continue
            if c == '"':
                # verbatim string starts with @"
                if i > 0 and code[i - 1] == "@":
                    state = "verbatim_string"
                else:
                    state = "string"
                out.append(" ")
                i += 1
                continue
            if c == "'":
                state = "char"
                out.append(" ")
                i += 1
                continue
            out.append(c)
            i += 1
            continue

        if state == "line_comment":
            if c == "\n":
                state = "code"
                out.append("\n")
            else:
                out.append(" ")
            i += 1
            continue

        if state == "block_comment":
            if c == "*" and nxt == "/":
                state = "code"
                out.append("  ")
                i += 2
            else:
                out.append("\n" if c == "\n" else " ")
                i += 1
            continue

        if state == "string":
            if c == "\\":
                # escape + next char
                out.append(" ")
                if i + 1 < n:
                    out.append(" ")
                    i += 2
                else:
                    i += 1
                continue
            if c == '"':
                state = "code"
                out.append(" ")
                i += 1
                continue
            out.append("\n" if c == "\n" else " ")
            i += 1
            continue

        if state == "verbatim_string":
            # "" escapes inside verbatim strings
            if c == '"' and nxt == '"':
                out.append("  ")
                i += 2
                continue
            if c == '"':
                state = "code"
                out.append(" ")
                i += 1
                continue
            out.append("\n" if c == "\n" else " ")
            i += 1
            continue

        if state == "char":
            if c == "\\":
                out.append(" ")
                if i + 1 < n:
                    out.append(" ")
                    i += 2
                else:
                    i += 1
                continue
            if c == "'":
                state = "code"
                out.append(" ")
                i += 1
                continue
            out.append("\n" if c == "\n" else " ")
            i += 1
            continue

    return "".join(out)


TYPE_DECL_RE = re.compile(
    r"(?m)^\s*(?P<mods>(?:public|private|protected|internal|static|abstract|sealed|partial|new|unsafe)\s+)*"
    r"(?P<kind>class|struct|interface|enum)\s+"
    r"(?P<name>[A-Za-z_]\w*)"
    r"(?:\s*:\s*(?P<bases>[^\{\n]+))?\s*\{"
)


def normalize_kind(kind: str, mods: str) -> str:
    mods_set = set((mods or "").split())
    if kind == "class":
        if "abstract" in mods_set:
            return "abstract class"
        if "static" in mods_set:
            return "static class"
        if "sealed" in mods_set:
            return "sealed class"
        return "class"
    return kind


def split_base_list(bases: Optional[str]) -> List[str]:
    if not bases:
        return []
    # Remove generic constraints tail if any (rare here)
    s = bases.strip()
    # Split by commas at top-level generic depth
    parts: List[str] = []
    cur: List[str] = []
    depth = 0
    for ch in s:
        if ch == '<':
            depth += 1
        elif ch == '>':
            depth = max(0, depth - 1)
        elif ch == ',' and depth == 0:
            part = "".join(cur).strip()
            if part:
                parts.append(part)
            cur = []
            continue
        cur.append(ch)
    last = "".join(cur).strip()
    if last:
        parts.append(last)

    # Strip common modifiers like "global::" and namespaces
    return [p.strip() for p in parts if p.strip()]


def find_matching_brace(code: str, open_brace_index: int) -> int:
    depth = 0
    for i in range(open_brace_index, len(code)):
        ch = code[i]
        if ch == '{':
            depth += 1
        elif ch == '}':
            depth -= 1
            if depth == 0:
                return i
    return -1


def extract_simple_type_names(type_expr: str) -> Set[str]:
    """Extract identifiers from a type expression (handles generics/arrays/namespaces)."""
    type_expr = type_expr.replace("global::", "")
    # Remove nullable suffix ? and pointer/ref symbols
    type_expr = re.sub(r"[\?\*\&]", " ", type_expr)
    # Keep identifiers only
    names = set(re.findall(r"[A-Za-z_]\w*", type_expr))
    # Drop container names, primitives, and keywords
    names = {
        n
        for n in names
        if n not in CS_PRIMITIVES
        and n not in GENERIC_CONTAINERS
        and n not in CS_KEYWORDS
        and (n[0].isupper() or n[0] == "_")
    }
    return names


def is_collection_type(type_expr: str) -> bool:
    if "[]" in type_expr:
        return True
    # Quick check for common generic containers
    for c in GENERIC_CONTAINERS:
        if re.search(rf"\b{re.escape(c)}\s*<", type_expr):
            return True
    return False


VISIBILITY_MODS_RE = re.compile(r"\b(public|private|protected|internal)\b")
MODIFIERS_RE = re.compile(
    r"\b(static|readonly|const|volatile|sealed|abstract|virtual|override|async|extern|new|unsafe|partial|event)\b"
)


def _first_space_outside_generics(s: str) -> int:
    depth = 0
    for idx, ch in enumerate(s):
        if ch == '<':
            depth += 1
        elif ch == '>':
            depth = max(0, depth - 1)
        elif ch.isspace() and depth == 0:
            return idx
    return -1


def try_parse_field_or_expression_property(line: str) -> Optional[Tuple[str, str]]:
    """Parse a field-like member or expression-bodied property at depth 0.

    Returns (type_expr, initializer_text).
    """
    s = line.strip()
    if not s.endswith(";"):
        return None

    # remove attributes on the same line (best-effort)
    s = re.sub(r"^\s*(\[[^\]]*\]\s*)+", "", s)
    # drop trailing ';'
    s = s[:-1].strip()

    init = ""
    if "=>" in s:
        left, right = s.split("=>", 1)
        s = left.strip()
        init = right.strip()
    elif "=" in s:
        left, right = s.split("=", 1)
        s = left.strip()
        init = right.strip()

    # If this looks like a method signature, don't treat it as a field/property.
    if "(" in s:
        return None

    # remove common modifiers
    s = VISIBILITY_MODS_RE.sub(" ", s)
    s = MODIFIERS_RE.sub(" ", s)
    s = re.sub(r"\s+", " ", s).strip()

    # For multi-variable declarations (Type a, b, c) we only need Type.
    split_idx = _first_space_outside_generics(s)
    if split_idx == -1:
        return None

    type_expr = s[:split_idx].strip()
    # ensure we have something that looks like a type
    if not type_expr or type_expr in CS_KEYWORDS:
        return None

    return type_expr, init


def try_parse_auto_property(line: str) -> Optional[str]:
    """Parse an auto-property signature line at depth 0.

    Example: public Foo Bar { get; set; }
    """
    if "{" not in line:
        return None
    s = line.strip()
    s = re.sub(r"^\s*(\[[^\]]*\]\s*)+", "", s)

    # take the part before '{'
    s = s.split("{", 1)[0].strip()

    # Methods also contain '{' but include parentheses; ignore those.
    if "(" in s:
        return None
    s = VISIBILITY_MODS_RE.sub(" ", s)
    s = MODIFIERS_RE.sub(" ", s)
    s = re.sub(r"\s+", " ", s).strip()

    # last token is property name; type is the rest
    tokens = s.split(" ")
    if len(tokens) < 2:
        return None
    type_expr = " ".join(tokens[:-1]).strip()
    if not type_expr or type_expr in CS_KEYWORDS:
        return None
    return type_expr

METHOD_RE = re.compile(
    r"^\s*(?:\[[^\]]*\]\s*)*"
    r"(?:(?:public|private|protected|internal)\s+)?"
    r"(?:static\s+)?(?:virtual\s+|override\s+|abstract\s+|async\s+|sealed\s+)*"
    r"(?P<ret>[A-Za-z_][\w\.<>,\[\]]*)\s+"
    r"(?P<name>[A-Za-z_]\w*)\s*\((?P<params>[^\)]*)\)"
)

EXPLICIT_INTERFACE_METHOD_RE = re.compile(
    r"^\s*(?:\[[^\]]*\]\s*)*"
    r"(?:(?:public|private|protected|internal)\s+)?"
    r"(?:static\s+)?(?:virtual\s+|override\s+|abstract\s+|async\s+|sealed\s+)*"
    r"(?P<ret>[A-Za-z_][\w\.<>,\[\]]*)\s+"
    r"(?P<iface>[A-Za-z_]\w*)\.(?P<name>[A-Za-z_]\w*)\s*\((?P<params>[^\)]*)\)"
)

CTOR_RE = re.compile(
    r"^\s*(?:\[[^\]]*\]\s*)*"
    r"(?:(?:public|private|protected|internal)\s+)?"
    r"(?P<name>[A-Za-z_]\w*)\s*\((?P<params>[^\)]*)\)"
)


def parse_param_types(param_list: str) -> Set[str]:
    if not param_list.strip():
        return set()
    parts: List[str] = []
    cur: List[str] = []
    depth = 0
    for ch in param_list:
        if ch == '<':
            depth += 1
        elif ch == '>':
            depth = max(0, depth - 1)
        elif ch == ',' and depth == 0:
            parts.append("".join(cur).strip())
            cur = []
            continue
        cur.append(ch)
    last = "".join(cur).strip()
    if last:
        parts.append(last)

    out: Set[str] = set()
    for p in parts:
        p = p.strip()
        if not p:
            continue
        # remove default value
        p = p.split("=")[0].strip()
        # remove modifiers
        p = re.sub(r"\b(ref|out|in|params|this)\b", " ", p).strip()
        # parameter name is the last identifier; type is the rest
        tokens = p.split()
        if len(tokens) <= 1:
            # could be something like "CancellationToken" (no name) - ignore
            continue
        type_expr = " ".join(tokens[:-1])
        out |= extract_simple_type_names(type_expr)
    return out


def collect_depth0_lines(body: str) -> Iterable[str]:
    depth = 0
    for raw_line in body.splitlines():
        line = raw_line
        if depth == 0:
            yield line
        # update depth after yielding
        depth += line.count("{") - line.count("}")


def analyze_file(path: Path, known_type_kinds: dict[str, str]) -> List[TypeDecl]:
    code = path.read_text(encoding="utf-8", errors="ignore")
    cleaned = strip_comments_and_strings(code)

    decls: List[TypeDecl] = []
    for m in TYPE_DECL_RE.finditer(cleaned):
        mods = m.group("mods") or ""
        kind_raw = m.group("kind")
        name = m.group("name")
        bases = split_base_list(m.group("bases"))
        kind = normalize_kind(kind_raw, mods)

        base_class: Optional[str] = None
        interfaces: List[str] = []

        if kind_raw == "class":
            if bases:
                base_class = bases[0].strip()
                interfaces = [b.strip() for b in bases[1:]]
        elif kind_raw == "interface":
            # interface extends interfaces
            interfaces = [b.strip() for b in bases]
        else:
            # struct/enum: ignore bases
            pass

        # body extraction
        open_brace_index = m.end() - 1
        close_brace_index = find_matching_brace(cleaned, open_brace_index)
        body = ""
        if close_brace_index != -1:
            body = cleaned[open_brace_index + 1 : close_brace_index]

        td = TypeDecl(kind=kind, name=name, base_class=base_class, interfaces=interfaces)

        # Scan only member lines at depth 0 inside the type
        for line in collect_depth0_lines(body):
            line_stripped = line.strip()
            if not line_stripped:
                continue

            # methods (skip constructors)
            mm = METHOD_RE.match(line)
            if mm:
                ret = mm.group("ret")
                td.dependency |= extract_simple_type_names(ret)
                td.dependency |= parse_param_types(mm.group("params"))
                continue

            em = EXPLICIT_INTERFACE_METHOD_RE.match(line)
            if em:
                ret = em.group("ret")
                td.dependency |= extract_simple_type_names(ret)
                td.dependency |= parse_param_types(em.group("params"))
                continue

            # constructor: dependencies from params
            cm = CTOR_RE.match(line)
            if cm and cm.group("name") == name:
                td.dependency |= parse_param_types(cm.group("params"))
                continue

            # auto-properties
            type_expr_prop = try_parse_auto_property(line)
            if type_expr_prop:
                type_names = extract_simple_type_names(type_expr_prop)
                if type_names:
                    if is_collection_type(type_expr_prop):
                        td.aggregation |= type_names
                    else:
                        td.association |= type_names
                continue

            # fields and expression-bodied properties
            parsed = try_parse_field_or_expression_property(line)
            if parsed:
                type_expr, init = parsed
                type_names = extract_simple_type_names(type_expr)
                if not type_names:
                    continue

                if is_collection_type(type_expr):
                    td.aggregation |= type_names
                else:
                    if init and re.search(r"\bnew\b", init):
                        td.composition |= type_names
                    else:
                        td.association |= type_names
                continue

        # Filter out self refs and obvious noise
        td.composition.discard(td.name)
        td.aggregation.discard(td.name)
        td.association.discard(td.name)
        td.dependency.discard(td.name)

        # Keep external base_class/interfaces as-is; but for relationship sets, prefer to keep useful ones.
        decls.append(td)

    return decls


def main() -> int:
    ap = argparse.ArgumentParser(description="Generate UML ClassRelationship.csv from C# scripts")
    ap.add_argument(
        "--source",
        default=str(Path("Assets/Scripts/FusionImpostor")),
        help="Folder containing .cs files to analyze",
    )
    ap.add_argument(
        "--output",
        default=str(Path("Assets/Docs/UMLDiagramClass/ClassRelationship.csv")),
        help="CSV output path",
    )
    ap.add_argument(
        "--internal-only",
        action="store_true",
        help="Only include types declared under the scanned source folder in relationship columns.",
    )
    args = ap.parse_args()

    source_dir = Path(args.source)
    output_path = Path(args.output)

    if not source_dir.exists():
        raise SystemExit(f"Source folder not found: {source_dir}")

    cs_files = sorted(source_dir.rglob("*.cs"))
    if not cs_files:
        raise SystemExit(f"No .cs files found under: {source_dir}")

    # First pass: collect known types (names + kinds)
    known_type_kinds: dict[str, str] = {}
    for f in cs_files:
        code = f.read_text(encoding="utf-8", errors="ignore")
        cleaned = strip_comments_and_strings(code)
        for m in TYPE_DECL_RE.finditer(cleaned):
            mods = m.group("mods") or ""
            kind_raw = m.group("kind")
            name = m.group("name")
            known_type_kinds[name] = normalize_kind(kind_raw, mods)

    # Second pass: analyze per file
    all_decls: List[TypeDecl] = []
    for f in cs_files:
        all_decls.extend(analyze_file(f, known_type_kinds))

    # Merge by type name in case multiple files/partials
    merged: dict[str, TypeDecl] = {}
    for d in all_decls:
        if d.name not in merged:
            merged[d.name] = d
            continue
        existing = merged[d.name]
        # Prefer a more specific kind label if available
        existing.kind = d.kind or existing.kind
        existing.base_class = d.base_class or existing.base_class
        existing.interfaces = sorted(set(existing.interfaces) | set(d.interfaces))
        existing.composition |= d.composition
        existing.aggregation |= d.aggregation
        existing.association |= d.association
        existing.dependency |= d.dependency

    known_type_names = set(known_type_kinds.keys())

    def maybe_filter_to_known(items: Set[str]) -> Set[str]:
        if not args.internal_only:
            return items
        return {i for i in items if i in known_type_names}

    # Normalize relationship sets: drop primitives/containers again; optionally drop unknown symbols like "get" (rare)
    def clean_set(items: Set[str]) -> List[str]:
        cleaned = [
            i
            for i in items
            if i not in CS_PRIMITIVES and i not in GENERIC_CONTAINERS and i not in CS_KEYWORDS
        ]
        return sorted(set(cleaned))

    header = [
        "Kind",
        "Class Name",
        "Generalization",
        "Realization/Implement",
        "Composition",
        "Dependency",
        "Assocation",
        "Inheritance",
        "Aggregation",
    ]

    output_path.parent.mkdir(parents=True, exist_ok=True)

    rows: List[List[str]] = []
    for name in sorted(merged.keys()):
        d = merged[name]
        kind = d.kind
        base = (d.base_class or "").strip()
        ifaces = "; ".join(sorted({i.strip() for i in d.interfaces if i.strip()}))
        comp = "; ".join(clean_set(maybe_filter_to_known(d.composition)))
        dep = "; ".join(clean_set(maybe_filter_to_known(d.dependency)))
        assoc = "; ".join(clean_set(maybe_filter_to_known(d.association)))
        aggr = "; ".join(clean_set(maybe_filter_to_known(d.aggregation)))

        # In this template, "Inheritance" is redundant with "Generalization"; keep same for convenience.
        inh = base

        rows.append([kind, name, base, ifaces, comp, dep, assoc, inh, aggr])

    with output_path.open("w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f)
        writer.writerow(header)
        writer.writerows(rows)

    print(f"Wrote {len(rows)} rows to: {output_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
