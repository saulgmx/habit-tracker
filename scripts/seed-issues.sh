#!/usr/bin/env bash
# Uso: ./scripts/seed-issues.sh <usuario/repo> [nombre-del-proyecto]
# Crea los labels y un issue por cada historia de docs/01-requisitos/historias-usuario.md.
# Es idempotente: omite las historias cuyo issue ya existe (mismo título).
# Si pasas el nombre de un GitHub Project, los issues se agregan a él (requiere: gh auth refresh -s project).
set -euo pipefail

REPO="${1:?Falta usuario/repo de GitHub (por ejemplo saul/habit-tracker)}"
PROJECT="${2:-}"
FILE="docs/01-requisitos/historias-usuario.md"

gh auth status >/dev/null 2>&1 || { echo "Necesitas gh autenticado (gh auth login)."; exit 1; }

label() { gh label create "$1" --repo "$REPO" --color "$2" --description "$3" --force >/dev/null; }
label historia 0e8a16 "Historia de usuario"
label "epica:gestion" 1d76db "Épica 1: Gestión de hábitos"
label "epica:seguimiento" 1d76db "Épica 2: Seguimiento diario"
label "epica:rachas" 1d76db "Épica 3: Rachas"
label "epica:pwa" 1d76db "Épica 4: PWA"
label "epica:evolucion" 5319e7 "Épica 5: Evolución técnica"
label "epica:infraestructura" 5319e7 "Épica 6: Infraestructura"
label "dia-2" fbca04 "Se hace en el día 2"

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

# Divide el archivo en un cuerpo por historia: $TMP/HU-01.md con la épica en la primera línea.
python3 - "$FILE" "$TMP" <<'PY'
import re, sys, pathlib
text = pathlib.Path(sys.argv[1]).read_text(encoding="utf-8")
out = pathlib.Path(sys.argv[2])
epic = ""
current = None
buf = []
def flush():
    if current:
        (out / f"{current[0]}.md").write_text(f"{epic_of[current[0]]}\n{current[1]}\n" + "".join(buf), encoding="utf-8")
epic_of = {}
for line in text.splitlines(keepends=True):
    if line.startswith("## Épica"):
        flush(); current = None; buf = []
        epic = re.match(r"## Épica (\d+)", line).group(1)
    elif line.startswith("### HU-"):
        flush(); buf = []
        m = re.match(r"### (HU-\d+) (.+)", line)
        current = (m.group(1), m.group(2).strip())
        epic_of[current[0]] = epic
    elif current and line.strip() != "---":
        buf.append(line)
flush()
PY

declare -a EPIC_LABEL=("" "epica:gestion" "epica:seguimiento" "epica:rachas" "epica:pwa" "epica:evolucion" "epica:infraestructura")

for f in "$TMP"/HU-*.md; do
  ID="$(basename "$f" .md)"
  EPIC="$(sed -n 1p "$f")"
  TITLE="$ID $(sed -n 2p "$f")"
  BODY="$(sed -n '3,$p' "$f")"
  if [ -n "$(gh issue list --repo "$REPO" --state all --search "\"$TITLE\" in:title" --json title --jq ".[] | select(.title==\"$TITLE\") | .title")" ]; then
    echo "Ya existe: $TITLE"; continue
  fi
  LABELS="historia,${EPIC_LABEL[$EPIC]}"
  [ "$EPIC" = "5" ] && LABELS="$LABELS,dia-2"
  ARGS=(--repo "$REPO" --title "$TITLE" --body "$BODY" --label "$LABELS")
  [ -n "$PROJECT" ] && ARGS+=(--project "$PROJECT")
  gh issue create "${ARGS[@]}"
done
