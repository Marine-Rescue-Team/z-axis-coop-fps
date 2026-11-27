#!/usr/bin/env python3
"""Planning/milestones.yml -> GitHub 마일스톤 (한 방향 반영).

- 제목이 "<id>."로 시작하는 마일스톤을 찾아 제목, 마감일, 설명을 덮어씁니다. 없으면 만듭니다.
- 마일스톤마다 종료 티켓(라벨: milestone-end)이 없으면 하나 만듭니다. 있는 티켓은 건드리지 않습니다.
- DRY_RUN=1 이면 GitHub에 요청하지 않고 보낼 내용만 출력합니다.
"""
import json, os, sys, urllib.request, urllib.error
import yaml

SRC = "Planning/milestones.yml"
LABEL = "milestone-end"
DRY = os.environ.get("DRY_RUN") == "1"
REPO = os.environ.get("GITHUB_REPOSITORY", "")
TOKEN = os.environ.get("GH_TOKEN", "")
API = f"https://api.github.com/repos/{REPO}"
FOOTER = f"---\n이 설명은 `{SRC}`에서 자동으로 반영됩니다. 여기서 직접 고치지 마세요."


def api(method, path, body=None):
    req = urllib.request.Request(API + path, method=method,
        data=json.dumps(body).encode() if body is not None else None,
        headers={"Authorization": f"Bearer {TOKEN}", "Accept": "application/vnd.github+json",
                 "X-GitHub-Api-Version": "2022-11-28", "Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(req) as r:
            return json.loads(r.read() or "null")
    except urllib.error.HTTPError as e:
        if e.code == 422 and method == "POST" and path == "/labels":
            return None  # 라벨이 이미 있음
        sys.exit(f"{method} {path} 실패: {e.code} {e.read().decode()[:300]}")


def description(m):
    lines = [f"기간: {m['start']} ~ {m['due']}", ""]
    if m.get("note"):
        lines += [str(m["note"]), ""]
    lines += ["통과 기준"] + [f"- {c}" for c in m["criteria"]] + ["", FOOTER]
    return "\n".join(lines)


def end_issue_body(m):
    return "\n".join([
        f"{m['id']} 마일스톤을 끝내는 티켓입니다. 통과 기준은 이 티켓에 연결된 마일스톤의 설명에 있습니다.",
        "",
        "- [ ] 마일스톤의 통과 기준을 모두 충족했는지 dev에서 확인",
        "- [ ] 이 마일스톤의 다른 티켓이 모두 닫혔거나 다음 마일스톤으로 옮겨짐",
        "- [ ] `dev` → `main` PR 병합",
        f"- [ ] `main`에 태그 `{m['id'].lower()}` 생성",
        "",
        "위 항목이 끝나면 이 티켓과 마일스톤을 닫습니다.",
    ])


def main():
    data = yaml.safe_load(open(SRC, encoding="utf-8"))
    items = data["milestones"]
    ids = [m["id"] for m in items]
    assert len(ids) == len(set(ids)), "id가 중복됩니다"
    for m in items:
        for k in ("id", "name", "start", "due", "criteria"):
            assert m.get(k), f"{m.get('id')}: {k} 항목이 비어 있습니다"
        assert str(m["start"]) <= str(m["due"]), f"{m['id']}: start가 due보다 늦습니다"

    existing = [] if DRY else api("GET", "/milestones?state=all&per_page=100")
    if not DRY:
        api("POST", "/labels", {"name": LABEL, "color": "5ed6c6", "description": "마일스톤 종료 티켓"})
    for m in items:
        payload = {"title": f"{m['id']}. {m['name']}", "description": description(m),
                   "due_on": f"{m['due']}T12:00:00Z"}
        if DRY:
            print(json.dumps(payload, ensure_ascii=False, indent=1)); print(end_issue_body(m)); print("=" * 40)
            continue
        found = next((e for e in existing if e["title"].startswith(m["id"] + ".")), None)
        if found:
            ms = api("PATCH", f"/milestones/{found['number']}", payload); print("갱신:", ms["title"])
        else:
            ms = api("POST", "/milestones", payload); print("생성:", ms["title"])
        tickets = api("GET", f"/issues?state=all&labels={LABEL}&milestone={ms['number']}&per_page=5")
        if not tickets:
            t = api("POST", "/issues", {"title": f"[{m['id']}] 마일스톤 종료", "body": end_issue_body(m),
                                        "milestone": ms["number"], "labels": [LABEL]})
            print("  종료 티켓 생성: #%d" % t["number"])


if __name__ == "__main__":
    main()
