# Danh sách Placeholder (cập nhật cột Trạng thái khi đã swap art thật)

Mọi sprite placeholder: trắng/viền tối đã bake màu, KHÔNG tint trong SpriteRenderer → swap sprite là xong.
Clip animator placeholder chỉ animate `Visual.localScale`; thay clip bằng animation thật khi có art.

| Prefab | Sprite placeholder | Vị trí swap | Yêu cầu art | Trạng thái |
|---|---|---|---|---|
| PH_Player_P1 | PH_Player_P1 (tròn xanh) | `PH_Player_P1/Visual` (SpriteRenderer.sprite) | 100x100px, PPU 100, pivot Center; điểm neo dây = `RopeAnchor` | Placeholder |
| PH_Player_P2 | PH_Player_P2 (tròn cam, Variant của P1) | `PH_Player_P2/Visual` | Khác P1 rõ về màu/viền/phụ kiện | Placeholder |
| PH_Enemy | PH_Enemy (vuông đỏ) | `PH_Enemy/Visual` (SpriteRenderer.sprite) | 100x100px, PPU 100, pivot Center | Placeholder |
| PH_Fruit_Apple | PH_Fruit_Apple | `PH_Fruit_Apple/Visual` (SpriteRenderer.sprite) | Dễ phân biệt ở kích thước nhỏ, PPU 100, pivot Center; `fruitType` = Apple | Placeholder |
| PH_Fruit_Banana | PH_Fruit_Banana | `PH_Fruit_Banana/Visual` (SpriteRenderer.sprite) | Dễ phân biệt ở kích thước nhỏ, PPU 100, pivot Center; `fruitType` = Banana | Placeholder |
| PH_Fruit_Cherry | PH_Fruit_Cherry | `PH_Fruit_Cherry/Visual` (SpriteRenderer.sprite) | Dễ phân biệt ở kích thước nhỏ, PPU 100, pivot Center; `fruitType` = Cherry | Placeholder |
| PH_Fruit_Grape | PH_Fruit_Grape | `PH_Fruit_Grape/Visual` (SpriteRenderer.sprite) | Dễ phân biệt ở kích thước nhỏ, PPU 100, pivot Center; `fruitType` = Grape | Placeholder |
| PH_Fruit_Orange | PH_Fruit_Orange | `PH_Fruit_Orange/Visual` (SpriteRenderer.sprite) | Dễ phân biệt ở kích thước nhỏ, PPU 100, pivot Center; `fruitType` = Orange | Placeholder |
| PH_FruitCoin | PH_FruitCoin (tròn vàng); icon HUD: PH_FruitCoin_Icon | `PH_FruitCoin/Visual` (SpriteRenderer.sprite) | 64x64px, PPU 100; icon HUD tách riêng. Chưa gắn script nhặt (tiền tệ do CurrencyManager) | Placeholder |
| PH_Checkpoint | PH_Checkpoint_Inactive / PH_Checkpoint_Active | `PH_Checkpoint/Visual` (SpriteRenderer.sprite) | 60x160px, PPU 100, pivot Bottom; thay sprite trong 2 clip `PH_Checkpoint_Inactive/Active.anim`. Animator Bool `Active` | Placeholder |
| PH_Hazard | PH_Hazard (đỏ trong suốt, 9-slice) | `PH_Hazard/Visual` (SpriteRenderer.sprite) | 64x64px, border 8px, Draw Mode Sliced/Tiled; đổi kích thước bằng SpriteRenderer.size + BoxCollider2D.size | Placeholder |
