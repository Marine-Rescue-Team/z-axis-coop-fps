#!/usr/bin/env python3
"""PR 규칙 검사.

1. main에는 dev만 합친다.
2. dev로 가는 PR: 브랜치 이름 feature/<번호>-<설명>, 제목에 [#번호], 본문에 Refs #번호, 닫기 키워드 금지.
3. 폴더 담당: 자기 담당 폴더는 승인 없이, 남의 폴더는 그 폴더 담당 중 한 명의 승인이 있어야 한다.
   담당은 대상 브랜치의 .github/CODEOWNERS에서 읽는다.
"""
import base64, json, os, re, sys, urllib.request, urllib.error

BRANCH = re.compile(r"^feature/(\d+)-[a-z0-9][a-z0-9-]*$")
CLOSING = re.compile(r"\b(close[sd]?|fix(e[sd])?|resolve[sd]?)\b:?\s+#\d+", re.I)


def parse_owners(text):
    """[(접두 경로, {아이디}), ...] 뒤에 나온 줄이 우선."""
    rules = []
    for line in text.splitlines():
        line = line.split("#", 1)[0].strip()
        if not line:
            continue
        parts = line.split()
        owners = {p[1:].lower() for p in parts[1:] if p.startswith("@")}
        rules.append((parts[0].lstrip("/"), owners))
    return rules


def owners_of(path, rules):
    found = set()
    for prefix, owners in rules:
        if prefix in ("*", "") or path == prefix.rstrip("/") or path.startswith(prefix if prefix.endswith("/") else prefix + "/"):
            found = owners
    return found


def evaluate(pr, files, approvers, owners_text):
    """문제 목록을 돌려준다. 비어 있으면 통과."""
    errs = []
    head, base = pr["head"], pr["base"]
    author = pr["author"].lower()
    title, body = pr.get("title") or "", pr.get("body") or ""
    if base == "main":
        if head != "dev":
            errs.append("main에는 dev만 합칩니다. 작업 브랜치는 dev를 대상으로 PR을 올리세요.")
    elif base == "dev":
        m = BRANCH.match(head)
        if not m:
            errs.append(f"브랜치 이름이 규칙과 다릅니다: `{head}`. `feature/<티켓번호>-<영문-설명>` 형식으로 만드세요. 예: feature/12-player-move")
        else:
            n = m.group(1)
            if f"[#{n}]" not in title:
                errs.append(f"PR 제목에 `[#{n}]`을 넣으세요. 예: [#{n}] 플레이어 이동")
            if not re.search(rf"\bRefs\s+#{n}\b", body, re.I):
                errs.append(f"PR 본문에 `Refs #{n}`을 적어 티켓과 연결하세요.")
        if CLOSING.search(body) or CLOSING.search(title):
            errs.append("닫기 키워드(Closes, Fixes, Resolves)는 쓰지 않습니다. `Refs #번호`로 바꾸세요. 티켓은 dev에서 확인한 뒤 직접 닫습니다.")
    else:
        return errs
    rules = parse_owners(owners_text)
    need = {}
    for f in files:
        owners = owners_of(f, rules)
        if not owners or author in owners or owners & approvers:
            continue
        top = f.split("/", 1)[0] + "/"
        need[top] = owners
    for top, owners in sorted(need.items()):
        names = ", ".join("@" + o for o in sorted(owners))
        errs.append(f"`{top}`는 담당 폴더가 아닙니다. 담당({names}) 중 한 명의 승인이 필요합니다.")
    return errs


def api(path, raw=False):
    req = urllib.request.Request(
        f"https://api.github.com/repos/{os.environ['GITHUB_REPOSITORY']}{path}",
        headers={"Authorization": f"Bearer {os.environ['GH_TOKEN']}", "Accept": "application/vnd.github+json",
                 "X-GitHub-Api-Version": "2022-11-28"})
    try:
        with urllib.request.urlopen(req) as r:
            return json.loads(r.read())
    except urllib.error.HTTPError as e:
        if e.code == 404:
            return None
        raise


def pages(path):
    out, page = [], 1
    while True:
        chunk = api(f"{path}{'&' if '?' in path else '?'}per_page=100&page={page}") or []
        out += chunk
        if len(chunk) < 100:
            return out
        page += 1


def main():
    event = json.load(open(os.environ["GITHUB_EVENT_PATH"], encoding="utf-8"))
    number = event["pull_request"]["number"]
    p = api(f"/pulls/{number}")
    pr = {"head": p["head"]["ref"], "base": p["base"]["ref"], "author": p["user"]["login"],
          "title": p["title"], "body": p["body"]}
    files = [f["filename"] for f in pages(f"/pulls/{number}/files")]
    latest = {}
    for r in pages(f"/pulls/{number}/reviews"):
        if r["state"] in ("APPROVED", "CHANGES_REQUESTED", "DISMISSED") and r.get("user"):
            latest[r["user"]["login"].lower()] = r["state"]
    approvers = {u for u, s in latest.items() if s == "APPROVED"}
    co = api(f"/contents/.github/CODEOWNERS?ref={pr['base']}")
    owners_text = base64.b64decode(co["content"]).decode("utf-8") if co else ""
    errs = evaluate(pr, files, approvers, owners_text)
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if errs:
        msg = "PR 규칙 검사 실패\n\n" + "\n".join(f"- {e}" for e in errs) + "\n\n자세한 규칙: AGENTS.md, Planning/workflow/index.html"
        print(msg)
        if summary:
            open(summary, "a", encoding="utf-8").write(msg + "\n")
        sys.exit(1)
    print("PR 규칙 검사 통과")
    if summary:
        open(summary, "a", encoding="utf-8").write("PR 규칙 검사 통과\n")


if __name__ == "__main__":
    main()
