Dưới đây là sơ đồ luồng dữ liệu (Flowchart) được vẽ bằng ASCII để bạn hình dung toàn bộ quá trình từ lúc **Nhặt (Pickup)** cho đến lúc **Hiển thị (Rendering)**.

Tôi chia làm 3 giai đoạn để bạn dễ nhìn:

1. **Giai đoạn Nhặt (Networking):** Xin phép Server.
2. **Giai đoạn Hiển thị (Visuals):** Quyết định gắn vào đâu (Camera hay Tay).
3. **Giai đoạn Dùng đồ (Action):** Bật/Tắt đèn.

---

### 1. QUY TRÌNH NHẶT (PICKUP FLOW)

*Mục tiêu: Chuyển quyền sở hữu và thông báo cho cả làng biết.*

```text
[CLIENT A (Bạn)]                     [SERVER (Host)]                     [CLIENT B (Người khác)]
       |                                    |                                       |
1. Bấm phím E (Raycast trúng)               |                                       |
       |                                    |                                       |
2. Gọi GrabObjectServerRpc() -------------> |                                       |
       |                                    |                                       |
       |                            3. Validate (Còn chỗ trống?)                    |
       |                                    |                                       |
       |                            4. XỬ LÝ LOGIC:                                 |
       |                               + ChangeOwnership(ClientA)                   |
       |                               + NetworkObject.TrySetParent(Player)         |
       |                               + Update Inventory Data                      |
       |                                    |                                       |
       |                            5. Gọi GrabObjectClientRpc() -----------------> |
       |<-----------------------------------|                                       |
       |                                    |                                       |
6. Chạy hàm OnGrabbed()              (Không làm gì)                      6. Chạy hàm OnGrabbed()
   (Logic hiển thị riêng)                                                   (Logic hiển thị riêng)
       |                                                                            |
       v                                                                            v
(Xem Sơ đồ 2 bên dưới)                                                   (Xem Sơ đồ 2 bên dưới)

```

---

### 2. QUY TRÌNH HIỂN THỊ (VISUAL LOGIC)

*Mục tiêu: "Ảo thuật" để máy mình thấy đẹp (ở mắt), máy bạn thấy đúng (ở tay).*
*Đây là những gì diễn ra bên trong hàm `OnGrabbed()` ở Bước 6 trên.*

```text
                                [Hàm OnGrabbed chạy]
                                          |
                                          v
                              [Kiểm tra: IsOwner?]
                                    /           \
                                  /               \
                       [CÓ (Là Máy Mình)]      [KHÔNG (Là Máy Người Khác)]
                              |                         |
                              |                         |
            +-----------------+                         +--------------------+
            |                                           |
1. GẮN VÀO CAMERA (LocalHand)               1. GẮN VÀO TAY NHÂN VẬT (ServerBone)
   (item.SetParent(MainCamera))                (item.SetParent(HandBone))
            |                                           |
2. ĐỔI LAYER "FirstPerson"                  2. ĐỔI LAYER "Default"
   (Để không bị Camera cắt hình)               (Để hiển thị như 3D thường)
            |                                           |
3. RESET VỊ TRÍ                             3. RESET VỊ TRÍ
   (Pos = 0,0,0 / Rot = 0,0,0)                 (Pos = 0,0,0 / Rot = 0,0,0)
            |                                           |
4. TẮT NET TRANSFORM                        4. TẮT NET TRANSFORM
   (Để Server không giật đồ về)                (Client tự quản lý Animation)

```

---

### 3. QUY TRÌNH DÙNG ĐỒ (ITEM USAGE)

*Mục tiêu: Bấm chuột trái -> Đèn sáng ở cả 2 máy.*

```text
[CLIENT A]                             [SERVER]                             [CLIENT B]
    |                                     |                                     |
1. Click Chuột Trái                       |                                     |
    |                                     |                                     |
2. Gọi UseItemServerRpc(true) ----------> |                                     |
    |                                     |                                     |
    |                             3. Gọi UseItemClientRpc(true) --------------> |
    |<------------------------------------|                                     |
    |                                     |                                     |
4. Chạy item.ItemActivate(true)           |                          4. Chạy item.ItemActivate(true)
    |                                     |                                     |
    v                                     |                                     v
+ Đèn sáng (Light.enabled = true)         |                     + Đèn sáng (Light.enabled = true)
+ Vật liệu phát sáng (Emission)           |                     + Vật liệu phát sáng (Emission)
+ Âm thanh "Tách" (AudioSource)           |                     + Âm thanh "Tách" (AudioSource)
    |                                     |                                     |
(Ngay trước mặt Camera A)                 |                     (Trên tay mô hình nhân vật A)

```

---

### TỔNG KẾT: CÁC ĐIỂM QUAN TRỌNG CẦN CHECK CODE

Dựa trên sơ đồ này, bạn hãy kiểm tra lại code xem đã có đủ các chốt chặn này chưa:

1. **Tại ServerRpc (Bước Nhặt):** Phải có dòng `ChangeOwnership`. Nếu thiếu dòng này, Client A sẽ không bấm chuột trái được (vì code check `if (!IsOwner) return`).
2. **Tại ClientRpc (Bước Hiển thị):** Phải có đoạn `if (IsOwner)` để tách parent.
* Nếu thiếu: Cả 2 máy đều gắn vào tay -> Máy mình nhìn thấy tay trống trơn (hoặc đèn pin bị body che mất).


3. **Tại Script GrabbableObject:** Phải có hàm `ItemActivate` (hàm ảo) để các item con (Đèn, Súng) ghi đè lên.

Bạn nhìn sơ đồ này có thấy đoạn nào "lấn cấn" hay khó hiểu không?