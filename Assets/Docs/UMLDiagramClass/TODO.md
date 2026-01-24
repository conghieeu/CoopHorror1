> "Hãy phân tích tất cả các file `.cs` trong thư mục FusionImposter và tự động điền thông tin vào file `ClassRelationship.csv` theo quy tắc ánh xạ sau:
> 1. **Kind**: Xác định loại (class, struct, enum, interface, abstract class).
> 2. **Class Name**: Tên của class/script.
> 3. **Generalization**: Tên lớp cha được kế thừa (ví dụ: MonoBehaviour).
> 4. **Realization/Implement**: Danh sách các Interface mà class thực thi.
> 5. **Composition**: Các field/biến được khởi tạo instance trực tiếp bên trong class (sở hữu mạnh).
> 6. **Aggregation**: Các field/biến dạng danh sách (List/Array) hoặc được gán reference từ bên ngoài (sở hữu yếu).
> 7. **Dependency**: Các class/type xuất hiện trong tham số hàm (parameters) hoặc kiểu trả về.
> 8. **Association**: Các field tham chiếu đơn giản đến class khác không thuộc hai loại trên.
