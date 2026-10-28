# Chơi LAN 1v1 trên hai máy

## Phiên bản hiện tại

Mở **MainMenu → KẾT NỐI LAN / 1v1**. Luồng gồm: kết nối phòng → cả hai chọn Pokémon và sẵn sàng → chủ phòng chọn map → cả hai sẵn sàng lần nữa → đấu.

- 2 người chơi, một máy **Host / P1**, một máy **Guest / P2**.
- Kết nối trực tiếp bằng IPv4, TCP cổng **7777**. Không cần cài package mạng hoặc đăng nhập tài khoản.
- 6 lựa chọn: Pikachu, Charizard, Blastoise, Venusaur, Gengar, Lucario. Hai người được chọn cùng Pokémon.
- 5 chủ đề: Lửa, Đền nước, Vườn cổ, Phế tích, Võ đường. Đây là sân PvP riêng vẽ bằng IMGUI, cùng kích thước và luật, khác bảng màu; không tải các scene boss hay chướng ngại vật của chế độ solo.
- Pokémon dùng sprite sẵn có, quay theo hướng di chuyển. Bản đầu dùng chung chỉ số, bắn đạn và lướt; chưa chuyển toàn bộ kỹ năng boss/solo sang PvP, chưa có hoạt ảnh đi/chạy riêng trong PvP.
- Chế độ đánh boss trong menu vẫn hoạt động độc lập.

## 1. Chuẩn bị game

1. Mở dự án bằng Unity **6000.5.7f1** và đợi biên dịch hoàn tất.
2. Mở `Assets/Scenes/MainMenu.unity`.
3. Trong **File → Build Profiles**, chọn Windows và bảo đảm danh sách scene có MainMenu cùng các scene đấu trường hiện có. Đặt MainMenu đầu tiên để game khởi động tại menu.
4. Build ra một thư mục mới, ví dụ `Builds/Lan/PokemonArena.exe`.
5. Chép **toàn bộ thư mục build**, bao gồm `.exe`, thư mục `_Data`, `UnityPlayer.dll` và các file đi kèm, sang máy thứ hai. Hai máy dùng cùng bản build.
6. Kết nối hai máy vào cùng mạng nội bộ (Wi-Fi hoặc Ethernet). Không dùng mạng khách có chế độ cô lập thiết bị.

Có thể thử một bên bằng Unity Play Mode và bên còn lại bằng bản build. Game tiếp tục cập nhật khi cửa sổ mất focus.

## 2. Máy A tạo phòng

1. Chạy game → **KẾT NỐI LAN / 1v1**.
2. Xem **IP máy này** trong cột trái. Nếu có nhiều IP, chọn IPv4 của card Wi-Fi/Ethernet đang dùng; không chọn card VPN/ảo nếu cả hai đang dùng LAN thông thường.
3. Có thể mở PowerShell trên máy A và chạy `ipconfig`, tìm **IPv4 Address** của card mạng đang kết nối. Ví dụ: `192.168.1.10`.
4. Bấm **TẠO PHÒNG**. Trạng thái chuyển sang đang chờ máy thứ hai.
5. Nếu Windows hỏi quyền mạng, người dùng cho phép đúng ứng dụng game trên mạng **Private**. Nếu bị chặn, cho phép ứng dụng hoặc TCP inbound 7777 trên mạng Private; không cần tắt toàn bộ firewall.
6. Gửi IP đó cho người chơi máy B.

Phòng nhận đúng một Guest. Muốn tạo lại phòng, bấm **NGẮT / THỬ LẠI**, rồi **TẠO PHÒNG**.

## 3. Máy B vào phòng

1. Chạy cùng bản game → **KẾT NỐI LAN / 1v1**.
2. Nhập IPv4 của **máy A** vào ô **IP MÁY CHỦ**. Không nhập IP của máy B, URL, hoặc thêm `:7777`.
3. Bấm **KẾT NỐI**. Chờ trạng thái đã kết nối, góc dưới hiển thị **GUEST / P2**.
4. Nếu kết nối chưa xong sau 8 giây, game báo hết thời gian. Kiểm tra phần xử lý lỗi rồi thử lại.

## 4. Chọn Pokémon, map và vào trận

1. Mỗi người bấm một thẻ Pokémon, rồi **SẴN SÀNG**. Bấm lại nút để hủy sẵn sàng và đổi lựa chọn.
2. Máy A chỉ bấm được **CHỌN MAP** khi cả hai đã sẵn sàng.
3. Máy A chọn một trong 5 thẻ map. Máy B nhìn thấy lựa chọn được đồng bộ.
4. Cả hai bấm **SẴN SÀNG** lần nữa. Đổi map sẽ xóa xác nhận của cả hai.
5. Máy A bấm **BẮT ĐẦU TRẬN**.
6. Sau khi kết thúc, Host bấm **CHƠI LẠI / CHỌN POKÉMON** để trở về bước chọn, giữ nguyên kết nối. Cả hai xác nhận lại trước trận mới.
7. **RỜI PHÒNG** đóng socket và quay lại menu solo. Nếu một bên rời hoặc mất kết nối, bên kia thoát trận về màn chọn; không tự tiếp tục trận cũ.

## 5. Điều khiển và luật

| Thao tác | Điều khiển |
| --- | --- |
| Di chuyển | W, A, S, D |
| Ngắm | Vị trí con trỏ chuột |
| Bắn liên tục | Giữ chuột trái trong vùng đấu trường |
| Lướt | Space + hướng di chuyển; giữ để lướt lại khi hồi xong |
| Rời phòng | Nút góc trên bên phải |

Mỗi Pokémon có **100 HP**, tốc độ **3,6 đơn vị/giây**. Đạn gây **10 sát thương**, hồi **0,45 giây**, bay **9 đơn vị/giây**, tồn tại tối đa **1,6 giây**. Lướt đi thêm **1,6 đơn vị**, hồi **2,5 giây**, không có miễn nhiễm sát thương. Người hết HP thua; nếu cả hai hết HP trong cùng nhịp mô phỏng thì hòa. Đạn của đối thủ có màu theo Pokémon.

## 6. Thử nhanh trên một máy

Chạy Editor và một bản build, hoặc hai tiến trình bản build. Chỉ một cửa sổ tạo phòng. Cửa sổ còn lại nhập **127.0.0.1**. Địa chỉ này chỉ dùng khi hai game chạy trên cùng máy, không dùng giữa hai máy khác nhau.

Kiểm tra thủ công:

- Chọn hai Pokémon khác nhau, xác nhận tên đối thủ xuất hiện đúng.
- Host không thể chuyển bước trước khi Guest sẵn sàng.
- Host đổi map làm mất sẵn sàng ở cả hai bên.
- Di chuyển, bắn trúng đối thủ: vị trí và HP giống nhau ở hai cửa sổ.
- Kết thúc trận, chơi lại: HP trở về 100, không còn đạn cũ.
- Đóng một cửa sổ: bên còn lại báo mất kết nối, có thể tạo/vào lại phòng.
- Cuối cùng chạy lại quy trình trên **hai máy vật lý** để kiểm tra firewall và mạng thực.

Kiểm tra tự động trong Editor: **Pokemon → Check LAN Duel**. Kiểm tra luật di chuyển, sát thương, hồi chiêu, biên lướt, thắng/thua, đầu vào không hợp lệ, kết nối TCP loopback và ngắt kết nối. Đóng phòng đang chạy trước khi kiểm tra vì bộ kiểm tra sử dụng cổng 7777. Thành công ghi `LAN_DUEL_CHECK_OK` trong Console.

## 7. Xử lý lỗi

| Hiện tượng | Cách kiểm tra |
| --- | --- |
| Không kết nối được | Host đã tạo phòng chưa? IP có đúng card mạng không? Hai máy cùng LAN chưa? |
| Hết thời gian sau 8 giây | Kiểm tra firewall của Host, mạng khách/AP isolation, VPN và IP đã thay đổi. |
| Cổng đang được sử dụng | Chỉ giữ một Host trên máy. Rời phòng ở phiên game cũ, rồi tạo lại. |
| Mất phản hồi | Đợi tối đa khoảng 12 giây để phát hiện mạng đứt; ngắt/thử lại. Ngắt kết nối có trật tự thường được phát hiện ngay. |
| Nút chọn map/bắt đầu bị khóa | Cần kết nối đủ 2 người và cả hai sẵn sàng; chỉ Host chuyển bước. |
| Bản game cũ không có nút LAN | Build lại từ mã mới và chép lại toàn bộ thư mục build. |
| Có IP nhưng không rõ IP nào | Trên Host chạy `ipconfig`, lấy IPv4 của adapter Wi-Fi/Ethernet đang dùng. |

Có thể chạy từ PowerShell máy B, trong lúc Host đang chờ: `Test-NetConnection 192.168.1.10 -Port 7777` (thay IP bằng IP thật). **Lưu ý:** phép thử này mở kết nối TCP và có thể chiếm chỗ Guest rồi ngắt; sau khi kiểm tra, Host bấm **NGẮT / THỬ LẠI → TẠO PHÒNG** trước khi máy B vào bằng game.

## 8. Cấu trúc và giới hạn kỹ thuật

- `Assets/Scripts/LanTransport.cs`: TCP không chặn vòng lặp Unity, kết nối có timeout, hàng đợi có giới hạn, đóng socket khi rời.
- `Assets/Scripts/LanDuelRules.cs`: mô phỏng và dữ liệu mạng. Host xử lý 50 nhịp/giây, sát thương do Host quyết định.
- `Assets/Scripts/LanDuelMenu.cs`: giao diện 1280×720 tự co theo cửa sổ, chọn nhân vật/map, gửi trạng thái hoặc ý định điều khiển khoảng 20 lần/giây.
- `Assets/Editor/LanDuelCheck.cs`: kiểm tra luật và transport.

Đây là bản LAN prototype: không có relay, tài khoản, phòng công khai, mã phòng, mã hóa hay xác thực người chơi; chỉ dùng với người chơi tin cậy trong mạng nội bộ. Chưa hỗ trợ WebGL, host migration, nối lại trận đang chơi, nội suy/dự đoán chuyển động hoặc bộ chiêu riêng từng Pokémon. Độ trễ cao có thể khiến hình ảnh di chuyển giật. Chưa tích hợp Internet matchmaking; không cần cấu hình port forwarding để chơi cùng LAN.

## Kết quả kiểm tra triển khai (28/10/2026)

- Unity Editor đã biên dịch thành công; `Pokemon → Check LAN Duel` ghi `LAN_DUEL_CHECK_OK`.
- Đã kiểm tra trực tiếp giao diện trong Play Mode: danh sách Pokémon, chọn map, sẵn sàng, đấu và chơi lại.
- Đã dùng một tiến trình khách TCP riêng qua localhost để xác minh lựa chọn Gengar, map Vườn cổ, đạn, HP về 0 và kết quả thắng được gửi sang khách.
- Chưa kiểm tra với hai máy vật lý, chưa xác minh bản build Windows cuối cùng. Cần thực hiện các bước ở trên để kiểm tra firewall, địa chỉ IP và độ trễ trong mạng sử dụng thực tế.
