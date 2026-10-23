# CLAUDE.md — Make2D1

Game 2D mobile/PC phong cách **Poinpy**: nhân vật kéo-thả để bắn lên cao, ăn quả (coin), giẫm quái, leo độ cao.
Nguồn kế hoạch: `Tom_Tat_Ke_Hoach_Update_Game.txt` (thư mục gốc).

## Môi trường
- Unity **2022.3.62f3**, URP 2D (`com.unity.render-pipelines.universal` 14.0.12)
- Package chính: Cinemachine 2.10.7, TextMeshPro 3.0.7, 2D Feature Set, Visual Scripting
- Input hiện tại: `UnityEngine.Input` (legacy, chuột). Chưa có Input System / Photon / AdMob.
- Scene duy nhất đang dùng: `Assets/Scenes/SampleScene.unity`

## Cấu trúc thư mục
```
Assets/
  Scenes/SampleScene.unity      # scene chơi chính
  Scripts/                      # toàn bộ code C#
  Prefabs/                      # Coin, AirEnemy, GroundEnemy
  TilePalette/map.prefab        # tilemap/map
  Settings/                     # URP / scene template
```

## Kiến trúc hiện tại (1 người chơi, endless leo cao)
- **Điều khiển**: giữ chuột để kéo → slow-motion (`timeScale = 0.2`), vẽ quỹ đạo + vòng giới hạn kéo; thả chuột để bắn (impulse). Mặc định 2 lần nhảy (`maxJumps`), chạm đất reset. `S` / mũi tên xuống / chuột phải = Ground Slam.
- **Chống kẹt**: coroutine kiểm tra overlap với layer ground, đẩy player ra ngoài.
- **Spawn**: `ObjectSpawner` sinh object theo từng mốc độ cao (`spawnIntervalY`) phía trên player, theo tỉ lệ % (coin 70 / air enemy 15 / ground enemy 15), kiểm tra va chạm ground bằng raycast/overlap trước khi `Instantiate`.
- **Chiến đấu**: va chạm tag `Enemy` — giẫm từ trên (`normal.y > 0.5`) thì nảy lên + diệt quái; còn lại mất 1 máu. HP mặc định 3, hết máu → `Die()` → reload scene sau 2s.
- **Coin**: trigger tag `Coin`; cộng điểm qua `ScoreManager` rồi `Destroy` khi Player chạm.

## Danh sách script C# (`Assets/Scripts/`)
| Script | Vai trò | Ghi chú |
|---|---|---|
| `PlayerController.cs` | Kéo-thả bắn, slow-mo (qua `GameManager.SetSlowMotion`), quỹ đạo, ground check, ground slam, unstuck. Nguồn duy nhất quản lý số lần nhảy (`ResetJumps`, `AddExtraJump`) | Gọi `ScoreManager.RegisterJump()` khi bắn; `ResetJumps()` cũng gọi `ResetCombo()`. Dừng xử lý khi game không ở `Playing`. Có ô `debugJumps` (log nơi gọi `ResetJumps`) |
| `PlayerCollision.cs` | HP, va chạm quái (giẫm: nảy + hồi 1 jump + cộng `enemyKillPoints`; còn lại mất máu), chết → `GameManager.GameOver()` rồi reload scene, HP UI (TMP, tùy chọn) | `TakeDamage(int)` và `Kill()` public; `CurrentHealth`/`MaxHealth` và event `OnHealthChanged(current, max)` |
| `ObjectSpawner.cs` | Sinh coin / quái bay / quái đất / bẫy theo độ cao. Đọc `RunManager.CurrentLevel`: tỉ lệ coin/enemy/trap từ `LevelConfig`, chỉ spawn trong khoảng StageStartY → TargetY (dừng cách Portal `stopSpawnMargin`), bẫy di động ở ải Survival/Boss. Spawn `fixedTraps` của ải ngay khi vào ải. Sinh quả từ `fruitPrefabs` (ưu tiên loại Fruit Challenge đang cần, `neededFruitBias`%). Spawn `bossPrefab` nếu ải có. Xóa vật thể cũ khi `OnStageChanged` | Cần gán `trapPrefab` (prefab có `Trap`), nếu trống thì bỏ qua bẫy. Không có RunManager thì dùng tỉ lệ serialized. Hàm `SqawnCoin` sai chính tả |
| `coin.cs` | Cộng `points` (mặc định 10) qua `ScoreManager.AddPoints`, báo `FruitChallengeManager.OnFruitCollected(fruitType)` rồi hủy khi Player chạm. Có `fruitType` (enum `FruitType`) | Tên file/class viết thường (giữ nguyên để không mất tham chiếu prefab). Mỗi prefab quả cần 1 `fruitType` khác nhau |
| `GameManager.cs` | Singleton (DontDestroyOnLoad), state `Menu/Playing/Paused/GameOver/Victory`, event `OnStateChanged`, quản lý `Time.timeScale` (pause + `SetSlowMotion`) | Mới thêm (Bước 1), cần gắn vào scene |
| `ScoreManager.cs` | Singleton theo scene: điểm hành động × combo + điểm độ cao, `FruitCoins` (100 điểm = 1 coin), events `OnScoreChanged`/`OnComboChanged` | Mới thêm (Bước 1). `BeginStage(startY)` chốt điểm độ cao ải cũ khi sang ải. Cần gắn `playerTransform`; đã hook vào coin/quái/jump |
| `LevelConfig.cs` | ScriptableObject cấu hình 1 ải: `stageIndex` (1-4), `stageType` (enum `StageType`: SpeedRush/Survival/Hybrid/Boss), `targetHeight`, `spawnCoinRate`, `spawnEnemyRate`, `spawnTrapRate` (bẫy random vị trí), `fruitChallengeEnabled`/`fruitRoundTime`/`fruitTypesPerRound`/`fruitMaxCountPerType`/`fruitRoundsToClear`/`fruitFailDamage` (cơ chế ải Speed Rush), `timeLimit` (giây, 0 = không giới hạn), `bossPrefab`, `fixedTraps` (mảng `FixedTrapSpawn`: prefab + offset; x = tọa độ thế giới, y = độ cao từ điểm xuất phát ải) | Bước 2. Menu: Create → Make2D1 → Level Config |
| `BiomeData.cs` | ScriptableObject biome: `biomeName`, `backgroundColor`, mảng `levels` (đúng 4 LevelConfig, `GetLevel(index)` 0-based) | Bước 2. Menu: Create → Make2D1 → Biome Data. `OnValidate` cảnh báo nếu ≠ 4 ải |
| `RunManager.cs` | Singleton (DontDestroyOnLoad) quản lý 1 Run: `CurrentBiome`, `NextBiome`, `CurrentStageIndex` (0-based), `CurrentLevel`, `StageStartY`/`TargetY`. `StartRun()`, `StartNewRun()` (nút chơi tiếp sau Victory), `LoadNextStage()`; events `OnRunStarted`, `OnStageChanged`, `OnVictory` | **Room-to-Room**: sang ải thì teleport player về điểm xuất phát (vị trí ban đầu trong scene), warp Cinemachine, reset jumps. **Mở khóa biome tuần tự**: `PlayerPrefs["Biome_UnlockedCount"]` (mặc định 1); thắng biome mới nhất → mở biome kế; random chỉ trong biome đã mở (ContextMenu "Reset Biome Unlocks"). Victory → `GameManager.Victory()`. `GameOver` → random biome, về ải 1 khi reload. `autoAdvanceByHeight` mặc định tắt. **Giới hạn thời gian ải**: `StageTimeRemaining`/`HasTimeLimit` (trừ theo `Time.deltaTime` có scale, chỉ khi Playing), hết giờ → `PlayerCollision.Kill()`. **Thưởng cuối run** (`AwardRunCoins`, 1 lần/run): Victory = `FruitCoins ×2`, thua = ×1, cộng vào `CurrencyManager`; `LastRunReward` |
| `Portal.cs` | Trigger2D ở đỉnh ải; Player chạm → `RunManager.LoadNextStage()`. Tự đặt `y = TargetY` khi ải đổi (`placeAtTargetHeight`) | Bước 2. Cần Collider2D (tự bật `isTrigger` khi Reset) |
| `Trap.cs` | Bẫy gây sát thương (`PlayerCollision.TakeDamage`) khi Player chạm, có `hitCooldown`; tùy chọn di chuyển ngang qua lại (`SetMoving`) | Bước 2b. Prefab cần Collider2D, KHÔNG gắn tag `Enemy` |
| `RollingBoulder.cs` | Bẫy vị trí cố định kiểu đá lăn: đứng yên (Kinematic) tới khi Player vào `activateRadius` thì chuyển Dynamic, rơi/lăn xuống với vận tốc ngang ban đầu, tự hủy sau `lifeTime` | Prefab cần Rigidbody2D + CircleCollider2D + `Trap`. Đặt vào `LevelConfig.fixedTraps` |
| `VictoryUI.cs` | Hiện panel Victory khi `OnVictory` (biome vừa thắng, điểm, coin thưởng ×2, biome kế, biome mới mở khóa); nút Play Again → `RunManager.StartNewRun()` | Bước 2b. Gắn vào object luôn active (Canvas), `panel` là con bị ẩn. Coin thưởng thật sự được cộng vào ví bởi RunManager |
| `BossController.cs` | Boss ải 4: bám góc trên viewport camera (`viewportAnchor`), tung skill luân phiên mỗi `skillInterval`: Aimed Shot (loạt đạn nhắm Player) và Rain (mưa đạn rơi từ mép trên). Không bị tiêu diệt, ải kết thúc ở Portal | Prefab cần sprite + script này + gán `projectilePrefab`. Gán vào `LevelConfig.bossPrefab` của ải 4 |
| `Projectile.cs` | Đạn của boss: bay theo hướng `Launch(dir)`, trúng Player thì `TakeDamage` rồi hủy, tự hủy sau `lifeTime` | Prefab cần Collider2D `isTrigger` |
| `CurrencyManager.cs` | Singleton (DontDestroyOnLoad), ví Fruit Coin lưu `PlayerPrefs["FruitCoins"]`: `Balance`, `AddCoins`, `TrySpend`, event `OnBalanceChanged` | Cần gắn vào scene. ContextMenu "Reset Wallet" |
| `GameHUD.cs` | HUD: điểm, combo, ví coin, đồng hồ ải, tên biome/ải, yêu cầu Fruit Challenge (TMP, ô nào trống thì bỏ qua) | Gắn vào Canvas |
| `FruitChallengeManager.cs` | Cơ chế ải Speed Rush (enum `FruitType`: Apple/Banana/Cherry/Grape/Orange): mỗi vòng random N loại quả × 1..M quả phải ăn trong `fruitRoundTime` giây; đủ thì +điểm thưởng và vòng mới, đủ `fruitRoundsToClear` vòng thì `RunManager.LoadNextStage()`; hết giờ thì `TakeDamage(fruitFailDamage)` và vòng mới | Gắn vào 1 object trong scene; chỉ chạy khi `LevelConfig.fruitChallengeEnabled`. Timer dùng `Time.deltaTime` có scale |
| `HeartsUI.cs` | HUD máu bằng trái tim: tạo `MaxHealth` Image từ `heartTemplate`, mất 1 máu = đổi sang sprite `emptyHeart` (hoặc ẩn nếu không gán) qua `PlayerCollision.OnHealthChanged` | Gắn vào container (HorizontalLayoutGroup); template là con của container |
| `RoundTimerUI.cs` | Đồng hồ quay đếm ngược vòng Fruit Challenge: Image Filled Radial360 thu dần theo chiều kim đồng hồ, kim `clockHand` (tùy chọn) quay theo, số giây, đổi màu đỏ khi ≤ `warningSeconds`; tự ẩn khi ải không có challenge | `root` phải là object khác object chứa script |
| `KillZone.cs` | Vực/vùng nguy hiểm: trigger, Player chạm → `PlayerCollision.Kill()` | Prefab `PH_Hazard`; kích thước chỉnh bằng `SpriteRenderer.size` + `BoxCollider2D.size` |
| `Editor/PlaceholderGenerator.cs` | Menu **Make2D1 → Placeholders**: sinh sprite `PH_*`, Animator Controller + clip, prefab P0 (Player P1/P2, Enemy, 5 Fruit, FruitCoin, Checkpoint, Hazard) vào `Assets/Art/Placeholders/{Sprites,Animators,Prefabs}`; ghi `Assets/Art/PLACEHOLDERS.md` | Bản thường KHÔNG ghi đè asset đã có (giữ art đã swap); bản "GHI ĐÈ" tạo lại hết. Sprite bake sẵn màu + viền, Visual không tint |

### Vấn đề kỹ thuật cần lưu ý khi mở rộng
- Đã có GameManager / ScoreManager / RunManager nhưng chưa có UI hiển thị điểm, chưa có CurrencyManager.
- Ground là nền **one-way** (`PlatformEffector2D`): `CheckGround` chỉ tính chạm đất khi `velocity.y <= 0.05`, tiếp xúc ngang, `separation > -0.05`; chống kẹt không hồi nhảy với nền one-way.
- Boss chưa có HP/không bị tiêu diệt; chưa có prefab Trap/Boss/Projectile (phải tự tạo trong Unity). Các asset `LevelConfig` hiện đều `stageType: 0` — cần đặt đúng Survival/Hybrid/Boss và `timeLimit` cho ải 1.
- Camera cần `Clear Flags = Solid Color` thì màu nền biome mới hiện.
- Chỉ `GameManager` được đổi `Time.timeScale`; script khác dùng `SetSlowMotion`.
- Spawner chỉ endless theo độ cao, chưa gắn với ải/biome (xem mục `ObjectSpawner` bên trên).
- Chưa có lưu dữ liệu (PlayerPrefs / JSON).
- Input gắn cứng vào chuột → cần tách lớp input để hỗ trợ touch, joystick, 2P.

## Lộ trình phát triển (theo `Tom_Tat_Ke_Hoach_Update_Game.txt`)

> Chế độ 1 người chơi: giống Poinpy, chỉ bắn kéo-thả, **không có dây**.

### Phần 1 — Chế độ 2 người chơi (Co-op)
Cơ chế trọng tâm: **Rope Physics** — 2 nhân vật nối bằng dây đàn hồi, tạo lực kéo/bắn đa chiều.
- **Local Co-op (1 thiết bị)**
  - Màn hình ngang (Landscape).
  - 2 Virtual Joystick độc lập ở hai góc đáy màn hình.
  - Camera tự động zoom-out tỉ lệ thuận với khoảng cách dãn dây (Cinemachine target group).
- **Online / LAN**
  - Photon PUN 2.
  - Host tính toán vật lý dây nối.
  - Client đồng bộ vị trí, vận tốc, trạng thái qua nội suy (Interpolation) để giảm trễ.
- Việc cần làm trong code: tách input khỏi `PlayerController`, tạo `RopeController` (DistanceJoint2D/SpringJoint2D + LineRenderer), `CameraController2P`.

### Phần 2 — Cấu trúc màn chơi & hệ thống Biome
Chuyển cảnh Room-to-Room qua **Cổng dịch chuyển (Portal)**.
- **FTUE**: Intro Stage chỉ hiện ở phiên đầu; lưu cờ `FTUE_Completed = true` trên thiết bị để bỏ qua các phiên sau.
- **1 Run = 4 ải độc lập**:
  | Ải | Loại | Mô tả | Độ cao |
  |---|---|---|---|
  | 1 | Speed Rush | Chạy đua thời gian ăn quả tích điểm (như Poinpy gốc) | 150 Units |
  | 2 | Survival | Né bẫy liên tục và chướng ngại vật di động | 250 Units |
  | 3 | Hybrid | 50% thu thập quả + 50% né cạm bẫy | 300 Units |
  | 4 | Boss | Như ải 3 + boss lớn ở góc trên tung skill liên tục | 300 Units |
- **Biome**: mở khóa tuần tự — Biome 1 (Khu Vườn), Biome 2 (Băng Tuyết), Biome 3 (Dung Nham), …
- 1 vòng (4 ải = 1 biome) kết thúc (thắng/thua) → lượt kế tiếp **random biome** để tạo vòng 4 ải mới.
- Việc cần làm: `LevelConfig` (ScriptableObject: loại ải, độ cao, spawn rate, biome), `RunManager`, `Portal`, `BiomeDatabase`, mở rộng `ObjectSpawner` theo config (thêm bẫy, chướng ngại di động, boss).

### Phần 3 — Hệ thống tính điểm động
- **Solo (1P)**: `Điểm = (Điểm ăn quả / diệt quái) × Hệ số Combo + Điểm độ cao`.
- **Combo** = số lần nhảy liên tiếp không chạm đất: 5 lần → ×1.5, tăng dần đến 10 lần → ×2.0, trên 10 vẫn ×2.0.
- **Co-op (2P)** thêm:
  - Rescue Bonus **+200**: cứu đồng đội rơi khỏi rìa màn hình trong vòng 3 giây.
  - Synchronized Bounce **+50**: 2 nhân vật cùng lúc nảy tường hoặc giẫm quái.
- Việc cần làm: `ScoreManager`, bộ đếm combo trong `PlayerController` (reset khi `isGrounded`), điểm độ cao theo `transform.position.y`, hook vào `coin` và `PlayerCollision`.

### Phần 4 — Kinh tế, Gacha & Skin Buff
- Quy đổi: **100 điểm gameplay = 1 Fruit Coin**.
- **Gacha**: 500 Coins/lượt đơn, 4.500 Coins/lượt ×10; **Pity**: đảm bảo Legendary ở lượt 50; **trùng lặp**: hoàn 50% Fruit Coins.
- **Bảng Rarity**:
  | Bậc | Tỉ lệ | Buff |
  |---|---|---|
  | Common | 70% | Không |
  | Rare | 20% | +5% coin |
  | Epic | 9% | +5% lực bắn, +5% coin |
  | Legendary | 1% | +1 lần jump, +10% coin, 1 lần khiên bảo vệ |
- Việc cần làm: `CurrencyManager`, `SkinDatabase` (ScriptableObject), `GachaSystem` (pity counter, hoàn trùng), `SaveSystem`, áp buff skin vào `PlayerController` (`launchForce`, `maxJumps`), khiên vào `PlayerCollision`.

### Phần 5 — Cơ chế bổ trợ & vòng lặp kết thúc
- **Buff Roguelike giữa các ải**: xong mỗi ải, chọn ngẫu nhiên 1 trong 3 buff (mở rộng phạm vi dây, trường hút trái cây, thêm mạng dự phòng).
- **Quảng cáo hồi sinh (AdMob Rewarded)**: đếm ngược 5 giây khi ngã; xem quảng cáo → hồi sinh tại Checkpoint gần nhất + 2 giây bất tử.
- **Phần thưởng cuối trận**:
  - Phá đảo 4 ải: **×2 Fruit Coins** toàn trận + Huy hiệu vinh danh (nguyên liệu chế tạo skin).
  - Thất bại giữa chừng: giữ **100%** Fruit Coins đã nhặt.
- Việc cần làm: `BuffSelectionUI` + `BuffDefinition`, `CheckpointManager`, `AdsManager` (AdMob/Google Mobile Ads), `RunResultCalculator`, thay `ReloadScene` trong `PlayerCollision.Die()` bằng luồng Game Over / Revive.

### Thứ tự triển khai đề xuất
1. Refactor nền tảng: GameManager, ScoreManager, tách Input, sửa trùng lặp jump (Phần 3 cơ bản trước).
2. Phần 2 (ải + biome + portal) với 1P.
3. Phần 3 (điểm & combo) + Phần 5 (buff, revive, thưởng).
4. Phần 4 (kinh tế, gacha, skin).
5. Phần 1 (2P local → online) — rủi ro cao nhất, làm sau khi lõi 1P ổn định.

## Quy trình Asset Placeholder (Swap-ready)
Nguồn danh sách asset: `Game.txt` (mục 3.1–3.7, ưu tiên P0/P1/P2). Với asset **P0/P1 chưa có file vẽ thật**, vẫn làm gameplay/UI bằng hình khối cơ bản (Square/Circle) nhưng cấu hình **như thể art thật đã có**, để khi có art chỉ cần **kéo-thả thay (Swap)** là chạy, không sửa code/prefab khác.

### Nguyên tắc bắt buộc
1. **Tách Logic khỏi Visual**: root prefab giữ script + Rigidbody2D + Collider2D; hình nằm ở child `Visual` (SpriteRenderer hoặc Image). Collider KHÔNG phụ thuộc kích thước sprite (chỉnh tay theo thiết kế, không auto-fit theo sprite).
2. **Placeholder là Sprite thật** (không dùng primitive/Mesh): đặt ở `Assets/Art/Placeholders/` với tiền tố `PH_` (`PH_Square`, `PH_Circle`, `PH_Capsule`…), Texture Type = Sprite (2D and UI), **cùng Pixels Per Unit với Art Bible** (chưa chốt → tạm 100; PNG 100×100 = 1 unit), Pivot đúng loại (nhân vật/quái: Bottom hoặc Center theo Art Bible, coin/quả: Center), Filter Mode/Compression đặt sẵn.
3. **Kích thước bằng world-unit**, không hard-code theo pixel; scale placeholder khi cần, KHÔNG scale root có collider.
4. **Sorting Layer / Order in Layer** đặt đúng ngay từ đầu (Background → Platform → Enemy/Item → Player → VFX → UI) để art thật không bị che.
5. **Animation**: mỗi thực thể có animation trong `Game.txt` phải có sẵn **Animator Controller** (`Assets/Art/Placeholders/Animators/<Tên>/<Tên>.controller`) với đủ state + parameter đặt tên chuẩn, clip placeholder 1 frame (có thể đổi màu/scale nhẹ để nhìn ra trạng thái). Script điều khiển Animator qua parameter (dùng `Animator.StringToHash`), **không** gọi `Play("tên clip")` hay đổi sprite bằng code. Skin dùng **Animator Override Controller** để thay clip.
6. **Điểm neo & socket** là Empty GameObject đặt tên cố định (`RopeAnchor`, `GroundCheck`, `Pivot_Hat`…), script tham chiếu theo Transform, không theo tọa độ cứng.
7. **UI**: dùng `Image` với sprite placeholder, `Image Type = Sliced` + Sprite Border cho khung/nút/popup (9-slice); nút 3 trạng thái dùng `Button` → `Sprite Swap` (Normal/Pressed/Disabled) hoặc Animator. Font dùng TMP Font Asset (swap font = đổi asset).
8. **Dây / nền / tile**: Rope = `LineRenderer` với `Texture Mode = Tile` + material nhận sprite; Platform/ground dùng `SpriteRenderer.Draw Mode = Sliced/Tiled` (hoặc Tilemap) để art ghép liền không cần sửa collider; Parallax mỗi lớp là 1 child có hệ số riêng.
9. **Prefab Variant cho biến thể** (Player1/Player2, quái, skin): Variant của prefab gốc, khác nhau chỉ ở màu/sprite → swap art không đụng logic.
10. Không đặt asset mới ngoài `Assets/Art/`, `Assets/Animations/`, `Assets/Prefabs/`; không đổi GUID/xóa file `.meta` của asset đã có.

### Tên state/parameter Animator chuẩn
| Thực thể | State (Trigger/Bool cùng tên) |
|---|---|
| Player | `Idle`, `Aim`, `Shoot`, `Fly`, `Land`, `Pulled`, `Fall`, `Rescued`, `Collect`, `Win`, `Lose` + Int `Emotion` (0 bình thường, 1 vui, 2 sợ) |
| Quái / Boss | `Idle`, `Hit`, `Vanish` |
| Coin / Quả | `Idle` (xoay), `Collect` |
| Checkpoint | `Inactive`, `Active` |
| UI popup/nút | `Show`, `Hide`, `Pressed` |

### Bảng ánh xạ P0/P1 → placeholder
| Asset (Game.txt) | Placeholder | Cấu hình cần có sẵn |
|---|---|---|
| Player (+P1/P2 variant) | Circle/Capsule, P1 và P2 khác màu | Visual con, Animator đủ state, `RopeAnchor`, Prefab Variant |
| Skin 4 độ hiếm + khung | Circle đổi màu theo bậc (xám/xanh/tím/vàng) | Animator Override Controller mỗi skin, khung rarity = Image Sliced dùng chung |
| Dây & nút dây | LineRenderer trắng/xám | Texture Mode Tile, 2 đầu gắn `RopeAnchor` |
| Quái | Square đỏ | Animator `Idle/Hit/Vanish`, tag `Enemy`, collider riêng |
| Quả / Fruit Coin | Circle (mỗi loại 1 màu), Circle vàng | `fruitType` khác nhau, Animator `Idle/Collect`, icon HUD là sprite riêng |
| Nền parallax | Square lớn nhiều lớp màu nhạt | Mỗi lớp 1 child + hệ số parallax, Sorting Layer Background |
| Platform / tileset | Square xám | Draw Mode Sliced/Tiled, collider + PlatformEffector2D giữ nguyên |
| Vực / vùng nguy hiểm | Square đỏ trong suốt | Trigger kill-zone độc lập với sprite |
| Checkpoint | Square nhỏ xám→xanh khi kích hoạt | Animator `Inactive/Active` |
| UI kit, Top/Bottom Bar, popup, HUD, bảng hồi sinh, Game Over, joystick | Image Sliced màu phẳng | Sprite Border đã đặt, Button Sprite Swap, joystick: nền + núm là 2 Image tách |
| Rope Trail, VFX (P1) | Particle/Trail dùng `PH_Circle` | Material/texture là slot Inspector, không hard-code |

### Quy trình mỗi lần thêm asset placeholder
1. Tạo/dùng sprite `PH_*` → dựng prefab theo nguyên tắc trên → lưu ở `Assets/Prefabs/`.
2. Tạo Animator Controller + clip placeholder nếu asset nằm trong bảng animation.
3. Ghi một dòng vào `Assets/Art/PLACEHOLDERS.md`: `Tên prefab | Sprite placeholder | Vị trí swap (đường dẫn Visual/Animator) | Yêu cầu art (kích thước, PPU, pivot) | Trạng thái (Placeholder/Đã swap)`.
4. Kiểm tra swap thử: kéo 1 sprite bất kỳ khác vào `Visual` → game chạy bình thường, collider/animation không lệch.
5. Khi có art thật: swap sprite/clip/font, đổi trạng thái trong `PLACEHOLDERS.md`; **không** sửa script.

## Quy ước làm việc
- Asset chưa có art (P0/P1) → làm theo mục "Quy trình Asset Placeholder"; không chờ art mới làm gameplay.
- Comment/UI/tài liệu bằng tiếng Việt; tên class, biến, hàm bằng tiếng Anh (PascalCase cho class/method, camelCase cho field, `[SerializeField] private` cho field chỉnh trong Inspector).
- Không sửa file `.meta`, thư mục `Library/`, `Temp/`, `obj/`, `Logs/`, `UserSettings/`.
- Khi thêm script mới đặt vào `Assets/Scripts/`; prefab vào `Assets/Prefabs/`.
- Kiểm tra bằng cách mở Unity Editor và chạy `SampleScene` (không có test tự động).
- Cập nhật mục "Danh sách script" trong file này mỗi khi thêm/xóa script.
