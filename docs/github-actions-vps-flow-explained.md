# CodeBadger: GitHub Actions → GHCR → VPS

Tài liệu này giải thích luồng deploy tự động: từ push commit vào main cho tới
khi các container chạy trên VPS.

## 1. Vấn đề và kiến trúc

Build trực tiếp trên VPS khiến server phải có source và build toolchain, version
khó truy nguyên và rollback không rõ. Luồng mới build một lần trên GitHub runner,
lưu image theo full commit SHA trong GHCR, rồi VPS chỉ pull và chạy artifact đó.

~~~text
git push main
  → Actions checkout commit
  → build MCP + Joern (linux/amd64)
  → push GHCR với tag full commit SHA
  → SSH/rsync Compose + scripts tới /opt/codebadger
  → VPS cập nhật IMAGE_TAG
  → docker compose pull && up -d --no-build
  → /health && smoke test
~~~

## 2. Trigger và concurrency

~~~yaml
# .github/workflows/deploy-vps.yml:3-12
on:
  push:
    branches: [main]
  workflow_dispatch:

concurrency:
  group: codebadger-production
  cancel-in-progress: false
~~~

Push vào main chạy tự động; workflow_dispatch chạy thủ công. Concurrency group
bảo đảm hai release không đồng thời thay đổi production.

## 3. Job build-and-push

### Checkout, version và quyền GHCR

~~~yaml
# .github/workflows/deploy-vps.yml:20-41
permissions:
  contents: read
  packages: write

- name: Check out the release commit
  uses: actions/checkout@v6

- name: Set image tag
  id: image
  run: echo "tag=${GITHUB_SHA}" >> "$GITHUB_OUTPUT"

- name: Log in to GitHub Container Registry
  uses: docker/login-action@v4
  with:
    registry: ghcr.io
    username: ${{ github.actor }}
    password: ${{ secrets.GITHUB_TOKEN }}
~~~

GITHUB_SHA là commit chính xác đã kích hoạt workflow. contents: read phục vụ
checkout; packages: write cho phép GITHUB_TOKEN push image, không cần hard-code PAT.

### Build hai image

~~~yaml
# .github/workflows/deploy-vps.yml:43-66
- name: Set up Docker Buildx
  uses: docker/setup-buildx-action@v4

- name: Build and publish MCP image
  uses: docker/build-push-action@v7
  with:
    context: .
    file: Dockerfile.mcp
    platforms: linux/amd64
    push: true
    tags: |
      ghcr.io/nguyenthanhhungdev140503/codebadger-mcp:<SHA>
      ghcr.io/nguyenthanhhungdev140503/codebadger-mcp:latest
~~~

Joern image dùng cùng Buildx action nhưng file Dockerfile và tên
codebadger-joern-server. Mỗi image có tag SHA (canonical production) và latest
(convenience, không dùng để xác định release production).

MCP image cài Python dependencies, copy main.py/src và Docker CLI client
(Dockerfile.mcp:25-39). Joern image cài Java 21, Joern 4.0.594 và Rust
(Dockerfile:7,15-16,32-43).

Job deploy nhận tag qua output image_tag và needs build-and-push, nên chỉ chạy sau
khi cả hai image push thành công.

## 4. Job deploy

### SSH secrets

~~~yaml
# .github/workflows/deploy-vps.yml:79-87
install -m 700 -d ~/.ssh
printf '%s\n' "$VPS_SSH_PRIVATE_KEY" > ~/.ssh/id_ed25519
chmod 600 ~/.ssh/id_ed25519
printf '%s\n' "$VPS_KNOWN_HOSTS" > ~/.ssh/known_hosts
~~~

| Secret | Giá trị |
|---|---|
| VPS_HOST | root@160.250.4.40 |
| VPS_SSH_PRIVATE_KEY | Private key khớp authorized_keys trên VPS |
| VPS_KNOWN_HOSTS | Host key đã xác minh của VPS |

Private key chỉ nằm trong filesystem tạm của runner, không được commit vào repo,
image hoặc log.

### Sync file, bảo toàn state

~~~yaml
# .github/workflows/deploy-vps.yml:89-99
rsync -az --info=progress2 \
  --exclude='.env' --exclude='playground/' --exclude='pgdata/' --exclude='logs/' \
  -e 'ssh -i ~/.ssh/id_ed25519 -o IdentitiesOnly=yes' \
  docker-compose.yml .env.defaults scripts \
  "$VPS_HOST:$VPS_APP_DIR/"
~~~

Được sync: docker-compose.yml, .env.defaults, scripts. Không sync: .env,
playground, pgdata, logs. Workflow không dùng rsync --delete.

| VPS path | Vai trò | Giữ qua deploy? |
|---|---|---:|
| /opt/codebadger/playground | Source và CPG cache | Có |
| /opt/codebadger/pgdata | Postgres catalog/jobs/findings | Có |
| /opt/codebadger/logs | Runtime logs | Có |
| /opt/codebadger/.env | Host configuration/secrets | Có |

### Cập nhật .env và lưu rollback tag

~~~bash
# .github/workflows/deploy-vps.yml:111-129
if [[ ! -f .env ]]; then
  cat > .env <<'ENV'
PLAYGROUND_HOST_PATH=/opt/codebadger/playground
POSTGRES_DATA_PATH=/opt/codebadger/pgdata
DOCKER_HOST=unix:///var/run/docker.sock
DOCKER_SOCK=/var/run/docker.sock
MCP_PUBLISH_HOST=127.0.0.1
ENV
  chmod 600 .env
fi

current_tag="$(sed -n 's/^IMAGE_TAG=//p' .env | tail -1)"
if [[ -n "$current_tag" ]]; then
  printf '%s\n' "$current_tag" > .last-deploy
fi
sed -i '/^IMAGE_REGISTRY=/d; /^IMAGE_TAG=/d' .env
printf 'IMAGE_REGISTRY=%s/\nIMAGE_TAG=%s\n' "$IMAGE_PREFIX" "$IMAGE_TAG" >> .env
~~~

Lần đầu workflow tạo baseline .env. Các setting host khác vẫn thuộc .env VPS.
Tag cũ được lưu vào .last-deploy trước khi đổi tag mới. Nếu health check hoặc
smoke test thất bại, `trap ERR` của workflow dùng tag này để tự động restore
`.env`, pull lại image SHA cũ, re-tag hai local alias `:latest`, rồi recreate
container. Run vẫn kết thúc **failed** để không che giấu deploy lỗi. Lần deploy
đầu không có tag cũ nên workflow chỉ báo lỗi, không rollback được.

### Compose resolve image

~~~yaml
# docker-compose.yml:3,27,61
image: ${IMAGE_REGISTRY:-}codebadger-joern-server:${IMAGE_TAG:-latest}
image: ${IMAGE_REGISTRY:-}codebadger-mcp:${IMAGE_TAG:-latest}
JOERN_WORKER_IMAGE: ${IMAGE_REGISTRY:-}codebadger-joern-server:${IMAGE_TAG:-latest}
~~~

Với IMAGE_REGISTRY=ghcr.io/nguyenthanhhungdev140503/ và IMAGE_TAG=<SHA>:

| Thành phần | Image |
|---|---|
| MCP | ghcr.io/nguyenthanhhungdev140503/codebadger-mcp:<SHA> |
| Joern build service | ghcr.io/nguyenthanhhungdev140503/codebadger-joern-server:<SHA> |
| Per-CPG worker | Cùng Joern <SHA> |

JOERN_WORKER_IMAGE phải trùng tag đã pull; nếu để latest worker có thể chạy
image cũ hoặc gặp ImageNotFound.

### Pull, recreate và health gate

~~~bash
# .github/workflows/deploy-vps.yml:131-142
docker compose config --quiet
docker compose pull
docker compose up -d --no-build

for _ in $(seq 1 30); do
  if curl -fsS http://127.0.0.1:4242/health | grep -q '"status"'; then
    break
  fi
  sleep 2
done
curl -fsS http://127.0.0.1:4242/health
bash scripts/smoke-test.sh
~~~

config --quiet bắt lỗi Compose; pull tải artifact; up -d --no-build recreate
service mà không build trên VPS. Health được poll 30 lần × 2 giây. Smoke test
đọc /health và thử POST /tools/call với list_tools.

## 5. Runtime Compose

~~~mermaid
flowchart TB
    Start((Deploy bắt đầu)) --> Pull[Pull image SHA]
    Pull --> Up[Compose up -d]
    Up --> MCP[codebadger-mcp]
    Up --> Joern[codebadger-joern-server]
    Up --> PG[(Postgres 16)]
    Up --> Redis[(Redis 7)]
    MCP --> Socket[[/var/run/docker.sock]]
    MCP --> Workers[[Tạo Joern worker containers]]
    MCP --> PG
    MCP --> Redis
    MCP --> Playground[( /opt/codebadger/playground )]
    Joern --> Playground
    Workers --> Playground
~~~

Compose định nghĩa bốn service tại docker-compose.yml:1-149. MCP mount Docker
socket để điều khiển Joern/worker; Postgres nằm ngoài playground để Joern không
đọc database files. Cổng host mặc định loopback: MCP 127.0.0.1:4242, Joern
127.0.0.1:13371-13870, Postgres 127.0.0.1:55432, Redis 127.0.0.1:56379.

Docker socket cho MCP quyền gần tương đương root trên host; VPS nên dedicated và
MCP không nên public trực tiếp ra Internet.

## 6. Health và smoke test

~~~python
# src/health.py:15-30
def aggregate_status(dependencies: dict) -> str:
    statuses = list(dependencies.values())
    if any(status == "down" for status in statuses):
        return "down"
    if any(status == "partial" for status in statuses):
        return "partial"
    return "up"
~~~

up nghĩa mọi dependency cần thiết hoạt động; partial nghĩa MCP còn phản hồi
nhưng dependency degraded; down nghĩa dependency bắt buộc mất. /health HTTP 503
làm workflow fail. Smoke test chấp nhận up/partial và thử tools endpoint
(scripts/smoke-test.sh:28-57).

## 7. Rollback

Workflow ghi tag cũ tại /opt/codebadger/.last-deploy. Rollback dùng
scripts/rollback.sh:19-56:

~~~bash
PREV_TAG=$(ssh codebadger "cat /opt/codebadger/.last-deploy")
ssh codebadger "cd /opt/codebadger && sed -i 's/^IMAGE_TAG=.*/IMAGE_TAG=$PREV_TAG/' .env"
ssh codebadger "cd /opt/codebadger && docker compose pull && docker compose up -d --no-build"
~~~

Rollback chỉ đổi image reference và recreate container; không xóa playground,
pgdata hay logs. Cả automatic rollback và `scripts/rollback.sh` đều re-tag local
`codebadger-mcp:latest` và `codebadger-joern-server:latest` về đúng SHA đang
rollback, không pull registry `:latest` có thể đã trỏ sang release mới hơn.

## 8. Flowchart và call graph

~~~mermaid
flowchart LR
    Start((git push main)) --> Build[build-and-push]
    Build --> MCP[Build MCP image]
    Build --> Joern[Build Joern image]
    MCP --> GHCR[(GHCR SHA tags)]
    Joern --> GHCR
    GHCR --> Deploy[[deploy job]]
    Deploy --> SSH[SSH + rsync]
    SSH --> Env[Update VPS .env]
    Env --> Pull[docker compose pull]
    Pull --> Up[docker compose up -d --no-build]
    Up --> Health{Health OK?}
    Health -->|No| Fail((Action failed))
    Health -->|Yes| Smoke[smoke-test.sh]
    Smoke --> Done((Deployment complete))
~~~

~~~mermaid
graph TD
    Push[git push main] --> Trigger[Actions trigger]
    Trigger --> Build[build-and-push job]
    Build --> Checkout[checkout]
    Build --> Login[login GHCR]
    Build --> MCPBuild[build Dockerfile.mcp]
    Build --> JoernBuild[build Dockerfile]
    MCPBuild --> GHCRM[(GHCR MCP SHA)]
    JoernBuild --> GHCRJ[(GHCR Joern SHA)]
    GHCRM --> Deploy[deploy job]
    GHCRJ --> Deploy
    Deploy --> Sync[rsync Compose/scripts]
    Sync --> Remote[remote bash]
    Remote --> Compose[docker compose]
    Compose --> Health[GET /health]
    Health --> Smoke[smoke-test.sh]
~~~

## 9. Analogy: kho hàng và xe giao hàng

| Thành phần | Hình dung |
|---|---|
| Git commit | Mã đơn hàng duy nhất |
| GitHub runner | Nhà máy đóng gói |
| Dockerfile | Công thức đóng gói MCP/Joern |
| GHCR | Kho trung chuyển có nhãn SHA |
| SSH key | Chìa khóa cửa VPS |
| compose pull | VPS nhận đúng kiện hàng |
| IMAGE_TAG | Nhãn version |
| /health | Kiểm tra máy đã khởi động |
| .last-deploy | Biên lai kiện trước để đổi trả |
| playground/pgdata | Đồ cố định, không thay khi giao kiện |

## 10. Failure points và trace

| Giai đoạn | Dấu hiệu | Kiểm tra |
|---|---|---|
| Checkout | Checkout fail | Commit/branch, contents: read |
| GHCR | unauthorized/denied | packages: write, package visibility |
| Build MCP | pip/Dockerfile fail | Dockerfile.mcp, requirements, Buildx log |
| Build Joern | Download fail | Dockerfile, network, Joern release |
| SSH | timeout/host key error | VPS_HOST, key, VPS_KNOWN_HOSTS, firewall |
| rsync | permission/path error | /opt/codebadger và quyền root |
| Pull | image not found | VPS docker login ghcr.io và tag SHA |
| Compose | interpolation/volume error | .env và absolute playground path |
| Health | timeout/503 | docker compose ps và MCP logs |

~~~bash
cd /opt/codebadger
docker compose ps
docker compose logs --tail=100 codebadger-mcp
curl -fsS http://127.0.0.1:4242/health
~~~

## 11. Source map

| File | Vai trò |
|---|---|
| .github/workflows/deploy-vps.yml:3-17 | Trigger, concurrency, registry, VPS path |
| .github/workflows/deploy-vps.yml:20-66 | Build/push hai image GHCR |
| .github/workflows/deploy-vps.yml:68-99 | SSH setup và sync files |
| .github/workflows/deploy-vps.yml:101-142 | .env, pull, recreate, health, smoke |
| Dockerfile.mcp:8-43 | MCP Python image và app source |
| Dockerfile:7-50 | Joern, Java, Rust và entrypoint |
| docker-compose.yml:2-18 | Joern image/mount |
| docker-compose.yml:26-109 | MCP, Docker socket, worker image, dependencies |
| docker-compose.yml:113-149 | Postgres/Redis, healthcheck, volumes |
| src/health.py:15-30 | Rollup up/partial/down |
| scripts/smoke-test.sh:28-57 | Health và tool endpoint smoke test |
| scripts/rollback.sh:19-56 | Đọc tag cũ và rollback |
| docs/deployment.md:61-153 | Production reference và secrets |
