# Lucario — Võ đường trên núi

Màn 05, mở sẵn ở trang 2 của menu. Bấm **TIẾP →**, chọn **Lucario**,
hoặc tiếp tục từ bảng chiến thắng Gengar. Scene: `Assets/Scenes/LucarioArena.unity`.
Lucario có 850 HP; Pikachu giữ nguyên điều khiển, tài nguyên và quy tắc choáng.

Sân đá chữ nhật rộng 16.8 × 12.8 đơn vị di chuyển. Cột cờ, đèn đá, đình,
cầu thang và cây chỉ nằm ngoài ranh giới. Núi tuyết có nhiều lớp xa gần.
Cờ và gió chuyển động chỉ để trang trí, không gây sát thương hoặc đẩy người chơi.

| Chiêu | Báo trước | Sát thương | Hồi chiêu | Cách né |
| --- | --- | --- | --- | --- |
| Aura Sphere | 0.64 s vận khí + 0.10 s đưa tay phóng | 22 | 2.88 s | Đường đạn khóa khi bắt đầu; đổi hướng hoặc lướt ngang. Tốc độ 6 đơn vị/s, không bám lại mục tiêu. |
| Metal Claw | 0.6 s, cung cam phía trước | 10 × 3 | 4.16 s | Ba nhịp tại 0.06 / 0.36 / 0.66 s sau vận chiêu; lùi khỏi tầm 2.15 hoặc né sang bên/ra sau, cung rộng 110°. |
| Extreme Speed | 0.79 s, làn vàng có mũi tên | 28, tối đa một lần | 5.6 s | Rời làn bán kính 0.65; Lucario lao trong 0.28 s, không đổi hướng giữa đường. |
| Meteor Mash | 1.24 s, vòng vàng cố định bán kính 2.65 | 48, một lần lúc chạm đất | 12 s | Rời toàn bộ vòng vàng; vòng trắng thu nhỏ chỉ báo thời gian. Sóng va chạm không vượt vòng báo. |

Ranh giới trên mặt đất khớp điều kiện gây sát thương theo tâm người chơi;
không có bán kính sát thương ẩn bên ngoài làn/vòng. Điểm cuối đường lao được
chặn ở mép sân trước khi hiển thị, nên Lucario không bẻ cong đường lao ở góc.
Aura Sphere dùng hệ thống va chạm năng lượng hiện có: hai đạn triệt tiêu hoặc
đạn mạnh hơn tiếp tục với sát thương còn lại.

Khoảng nghỉ để phản công: 0.49 s sau Aura Sphere, 0.79 s sau Metal Claw /
Extreme Speed, 1.15 s sau Meteor Mash. Lucario đứng yên trong khoảng này.
Khi còn ≤50% HP, Lucario hoàn tất chiêu hiện tại rồi vận khí 1.4 s.
Giai đoạn hai tăng tốc áp sát 2.64 → 3.24 đơn vị/s và giảm 20% khoảng nghỉ của
ba chiêu thường. Thời gian báo chiêu, sát thương, kích thước vùng đánh và
khoảng nghỉ ulti giữ nguyên. Thanh boss và dòng hướng dẫn cho biết lúc có thể phản công.

Mọi chiêu, đạn và độ cao nhảy được xóa khi thắng, thua hoặc xem lại xuất hiện.
Đánh lại / rời luyện tập tạo trận mới, hồi HP và xóa trạng thái giai đoạn hai.
Các giá trị cân bằng là mặc định triển khai, chưa thay thế đánh giá độ khó bằng người chơi.

## Animation và âm thanh

Nguồn: [PMDCollab / SpriteCollab, Lucario 0448](https://github.com/PMDCollab/SpriteCollab/tree/master/sprite/0448),
tải ngày 2026-10-21. Metadata và credits gốc giữ trong `Assets/Resources/LucarioBoss`.
Sprite gốc thuộc hệ Pokémon Mystery Dungeon; giữ tám hướng và neo theo shadow.

- Charge → RearUp cho Aura Sphere; thời điểm thả đạn khớp frame phát đòn.
- Attack chạy ba chu kỳ ngắn; frame đánh khớp ba nhịp Metal Claw.
- Charge → QuickStrike cho Extreme Speed, có năm ảnh dư và vệt gió.
- Charge → Hop → Attack cho Meteor Mash; đường bay nối liên tục tới điểm khóa,
  bóng ở mặt đất thu nhỏ theo độ cao. Độ nâng có sẵn trong sprite Hop được trừ
  theo từng frame / hướng trước khi áp dụng đường nhảy, tránh cộng độ cao hai lần
  và bị thanh máu che. Frame tiếp đất vào thẳng tư thế đánh.
- Faint dừng ở frame cuối, không lặp lại animation ngã.
- Âm vận khí, báo ulti, phóng cầu, vuốt, lao, va chạm và thức tỉnh được tổng hợp
  tại chỗ; theo thanh âm lượng menu. Không dùng nhạc hoặc file âm thanh từ game khác.

## Kiểm thử tái lập

Chạy Unity 6000.5.7f1 với `-batchmode -projectPath <project> -executeMethod <method> -logFile <log>`.

- `LucarioSetup.RunAll` (thêm `-quit`): tạo scene / đăng ký build, render ảnh và
  kiểm tra toàn bộ frame của chín clip ở tám hướng; khóa mục tiêu; từng nhịp đánh;
  ranh giới gây sát thương; lướt và đi bộ né thật; các hướng / góc sân; E và C;
  va chạm Aura Sphere; chênh lệch bước thời gian; chuyển giai đoạn; hủy chiêu;
  AI giao đấu 60 giây.
- `LucarioPlayModeCheck.Run` (không `-quit`, tự thoát): menu trang 2, scene thật,
  xuất hiện, E, ulti trúng, lướt né, thắng/thua/đánh lại, replay xuất hiện,
  luyện tập hai chiều, Gengar → Lucario và AI chạy với rendering.
- `LucarioBuildCheck.Run` (thêm `-quit`): chạy lại kiểm thử Lucario và các bộ hồi quy
  Pikachu, choáng, va chạm năng lượng, Charizard, Blastoise, Venusaur, Gengar;
  sau đó build Windows x64 tại `Builds/LucarioQA/PokemonBossArena.exe`.
  Đây là development build dùng kiểm tra; chạy bình thường vẫn vào menu.
- Tham số riêng của QA build: `-lucarioCapture <absolute-output-directory>`.
  Chế độ này tự chụp menu, bốn cảnh báo / đòn đánh và bảng thắng ở
  1280×720 / 960×540 rồi thoát. Không truyền tham số này khi chơi.
  Mã chụp chỉ được biên dịch khi có define `LUCARIO_CAPTURE`.

Ảnh và log nằm trong `Docs/Previews/Lucario*`.
Gợi ý né đặt ngay trên thanh kỹ năng; nhường chỗ cho thông báo thao tác khi cần.
Trạng thái choáng không hiển thị trên bảng kết thúc trận.

Ngày 2026-10-21: bộ Lucario đạt 1.444 assertions (bao gồm lấy mẫu các frame ở
tám hướng); bảy bộ hồi quy liên quan đạt thêm 259 assertions, tổng 1.703.
Play Mode đạt các luồng vào màn, giao chiến, thắng/thua, retry, luyện tập và
Gengar → Lucario. Windows x64 build thành công; kiểm tra ảnh thật ở 1280×720
và 960×540 đạt, gồm menu trang 2, bốn cảnh báo / đòn đánh và bảng thắng.
Harness kiểm tra ảnh có nội dung trước khi ghi kết quả đạt; không chấp nhận ảnh đen.
Kết quả cuối: `LucarioFinalBuild.log`, `LucarioPlayMode.result.txt`,
`LucarioPlayerCapture.result.txt`. Thống kê thời gian trong log batch Editor
không phải benchmark FPS của bản Windows.
Đánh giá cảm giác điều khiển / độ hấp dẫn vẫn cần chơi thử bởi người thật.
