# 골목고양이 생존기 — Codex 작업 가이드

## 프로젝트 개요
Unity 2D 횡스크롤 생존 로그라이트. 고양이가 된 사람 주인공이 골목에서 살아남는 게임.  
전체 설계는 `GDD.md` 참조.

## 기술 스택
- **엔진**: Unity (URP, 2D)
- **입력**: Unity Input System (새 입력 시스템)
- **저장**: PlayerPrefs → 추후 JSON
- **언어**: C#
- **타겟 해상도**: 1080×1920 (세로 모바일)
- **프레임**: 60fps 목표

## 폴더 구조
```
Assets/
  Scripts/
    Core/          GameManager, StageManager, SaveManager, SaveData
    Player/        PlayerController, PlayerAnimationController, PlayerStats
    Systems/       HealthSystem, HungerSystem, ExpManager, ScoreManager, FeverManager
    Enemies/       EnemyBase, PersonEnemy, CatEnemy, DogEnemy, EnemySpawner
    Items/         ItemBase, FoodItem, MoneyItem, ItemSpawner
    Events/        EventManager, GameEvent
    UI/            UIManager, HUDController
    Camera/        CameraFollow
    Map/           StageData
  Imported/
    Cat_player/    고양이 스프라이트 시트 (32×32)
    alley_cat_tilemap_pack/  골목 타일셋
  Scenes/
    SampleScene    (메인 개발 씬)
```

## 스프라이트 경로
`Assets/Imported/Cat_player/Cat_player/Cat_sheets/`

| 파일명 | 용도 |
|--------|------|
| Cat_idle_1.png | 기본 대기 |
| Cat_walk_1.png | 걷기 |
| Cat_run_1.png | 달리기 |
| Cat_jump_1/2.png | 점프 |
| Cat_fall_1.png | 낙하 |
| Cat_landding_1.png | 착지 |
| Cat_ducking_1.png | 숨기 시작 |
| Cat_ducking_idle_1.png | 숨기 유지 |
| Cat_ducking_move_1.png | 낮은 자세 이동 |
| Cat_attack_1.png | 공격 |
| Cat_hit_1.png | 피격 |
| Cat_dead_1.png | 사망 |
| Cat_asleep_1.png | 휴식 |
| Cat_ladder_1.png | 사다리 오르기 |
| Cat_against_wall.png | 벽에 붙기 |
| Cat_spining_1.png | 피버/대시 |
| Cat_win_cheer_1.png | 클리어 |

타일셋: `Assets/Imported/alley_cat_tilemap_pack/alley_cat_tileset_32px_8x8.png`

## 개발 규칙

### 코드
- 싱글턴은 `GameManager`, `StageManager`, `SaveManager`만 사용
- 시스템 간 통신은 C# event/Action 사용 (직접 참조 최소화)
- MonoBehaviour 없이도 동작하는 로직은 일반 C# 클래스로 분리
- 애니메이션 상태는 `PlayerAnimationController`에서만 관리
- 물리: Rigidbody2D (Dynamic), 충돌 레이어 명시적 설정

### 레이어 설계
```
Player      = 6
Ground      = 7
Enemy       = 8
Item        = 9
Interactable = 10
```

### 태그
```
Player / Enemy / Item / Ground / Interactable / NPC
```

### 애니메이터 파라미터 네이밍
- bool: `isWalking`, `isRunning`, `isDucking`, `isOnLadder`, `isHurt`, `isDead`
- trigger: `Jump`, `Land`, `Attack`, `Win`, `Fever`, `Dash`

## 현재 개발 단계
**1차 MVP 진행 중**  
목표: 시골 골목 1맵에서 걷기·점프·숨기·사람 회피·경험치 100% 달성까지

### 완료된 것
- [ ] 프로젝트 초기 설정 (URP, Input System)
- [ ] 스프라이트 임포트
- [ ] 타일맵 임포트

### 다음 작업 순서
1. Scripts 폴더 구조 생성 및 뼈대 스크립트 작성
2. PlayerController — 좌우 이동, 점프
3. PlayerAnimationController — 기본 상태머신
4. CameraFollow — 횡스크롤 카메라
5. HealthSystem / HungerSystem
6. ExpManager / ScoreManager
7. ItemBase, FoodItem, MoneyItem + ItemSpawner
8. PersonEnemy AI
9. GameManager 상태 흐름
10. UIManager / HUDController
11. SaveManager
12. 씬 조립 및 테스트

## 주의사항
- 모바일 우선이므로 Update() 내 GC 발생 최소화
- 스프라이트 라이선스: 상업용 사용 시 크레딧 필수, AI 학습 사용 금지
- PlayerPrefs 키는 상수로 관리 (문자열 하드코딩 금지)
