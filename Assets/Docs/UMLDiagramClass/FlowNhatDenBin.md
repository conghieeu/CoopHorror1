Ok, tao vẽ **ASCII flow đúng lethal-style**, chỉ cho **nhặt ĐÈN PIN**, không lẫn thứ thừa.

---

```
CLIENT A (local)          SERVER                  CLIENT B
──────────────────────────────────────────────────────────

World Flashlight
(isHeld = false)
(rendered on ground)

A nhấn E
    |
    v
RequestGrab(itemId)  ───────────────►

                        if (!isHeld):
                           isHeld = true
                           holder = A

◄────────────── NetworkVariables Updated ────────────────

Client A reacts:                    Client B reacts:

[WORLD ITEM HIDE]                   [WORLD ITEM HIDE]

[SPAWN FP FLASHLIGHT]               [SPAWN TP FLASHLIGHT]
(local-only)                        (networked / visible)

(camera follow)                     (attach to player hand bone)

```

---

### **Khi THẢ đèn pin**

```
CLIENT A (LOCAL)                     SERVER                     CLIENT B
──────────────────────────────────────────────────────────────────────────

[nhấn G / Drop]
        |
        v
PlayerInventory (Client A)
        |
        |  Send ServerRpc: RequestDrop(itemId, dropPos)
        v
──────────────────────────────►  ServerRpc
                                |
                                |  item.isHeld = false
                                |  item.heldBy = 0
                                |  item.transform = dropPos
                                |
                                v
                          NetworkVariable Update
──────────────────────────────◄───────────────────────────────────────────

        |                                              |
        v                                              v
Client A OnValueChanged                         Client B OnValueChanged
(isHeld = false)                                (isHeld = false)
        |                                              |
[DESTROY FP FLASHLIGHT]                    (KHÔNG CÓ FP ITEM)
        |                                              |
[SHOW WORLD ITEM]                          [SHOW WORLD ITEM]
(renderer + collider ON)                   (renderer + collider ON)
```

---

### **CÂU CHỐT (NHỚ KỸ)**

```
WORLD FLASHLIGHT  ≠  FP FLASHLIGHT
```

* World flashlight: **networked, bật/tắt**
* FP flashlight: **local-only, spawn/despawn**

Nếu mày muốn, tao có thể:

* Vẽ tiếp flow **dùng đèn pin (bật/tắt)**
* Hoặc vẽ **sơ đồ class đúng lethal**

Chọn 1.
