# Nhịp boss nhanh hơn — 2026-10-21

Áp dụng cho Charizard, Blastoise, Venusaur, Gengar và Lucario theo yêu cầu tăng độ khó.
Thời gian vận chiêu giảm khoảng 25% (làm tròn tới 0.01 giây), hồi chiêu giảm 20%,
nghỉ sau đòn giảm khoảng 25%, tốc độ áp sát tăng 20%. Animation Charge chạy 4/3 tốc độ;
Meteor Mash tự co thời gian nhảy theo mốc tiếp đất mới. Các vòng đếm ngược dùng chung
mốc vận chiêu với logic sát thương.

| Boss | Vận chiêu 1 / 2 / 3 / ulti (giây) | Nghỉ sau đòn (giây) | Áp sát (đơn vị/giây) |
| --- | --- | --- | --- |
| Charizard | 0.30 / 0.60 / 0.49 / 0.83 | 0.49 | 1.92 |
| Blastoise | 0.34 / 0.27 / 0.34 / 0.83 | 0.68 | 1.56 |
| Venusaur | 0.42 / 0.60 / 0.49 / 1.24 | 0.60 | 1.20 |
| Gengar | 0.53 / 0.90 / 0.75 / 1.35 | 0.68 | 1.20 |
| Lucario | 0.64 / 0.60 / 0.79 / 1.24 | 0.49 / 0.79 / 0.79 / 1.15 | 2.64 → 3.24 khi thức tỉnh |

Aura Sphere phóng cầu sau thêm 0.10 giây đưa tay. Metal Claw vẫn có ba nhịp vuốt
để người chơi thoát giữa chuỗi. Lucario thức tỉnh giữ khoảng nghỉ ulti 1.15 giây;
ba chiêu thường tiếp tục giảm 20% thời gian nghỉ. Mở đầu trận và báo thức tỉnh
vẫn dành thời gian cho người chơi chuẩn bị.

Kiểm thử tái lập bằng Unity batch mode:

- `LucarioBuildCheck.Run`: toàn bộ kiểm thử chiến đấu của năm boss và Pikachu,
  va chạm năng lượng, thời điểm cảnh báo/sát thương, dọn trạng thái trận;
  mô phỏng AI Lucario 60 giây; `BossTempoCheck.Run`; build Windows.
- `BossTempoCheck.Run`: phản ứng sau 200 ms, dùng điều khiển di chuyển thật để
  đi bộ né ulti của cả năm boss; lướt và di chuyển né Hydro Pump. Không miễn sát thương.
- `LucarioPlayModeCheck.Run` và `WaterTemplePlayModeCheck.Run`: kiểm tra scene,
  chiến đấu, cảnh báo, né, chuyển màn, thắng/thua và chơi lại trong Play Mode.

Bot kiểm tra vòng chiêu Lucario chọn vị trí an toàn theo làn/vòng đã khóa, đồng thời
vào tầm cận chiến giữa các đòn để kiểm tra đủ bốn chiêu. Đây là kiểm tra lịch AI;
khả năng né bằng điều khiển thật được kiểm tra riêng trong `BossTempoCheck` và
các bài lướt/đi bộ trong `LucarioSetup`.

Bản chơi thử: `Builds/LucarioQA/PokemonBossArena.exe`.
Các thông số chi tiết nằm trong tài liệu riêng của từng boss.

Kết quả kiểm tra: 1.703 assertions hồi quy đạt; sáu tình huống né bằng di chuyển thật
đạt; Play Mode Lucario và Blastoise đạt; Windows x64 build thành công. Ảnh từ bản
Windows ở 1280×720 và 960×540 đã kiểm tra; vòng Meteor và làn Hydro Cannon hiển thị rõ.
Log: `Docs/Previews/BossTempoCheck.log`, `BossTempoLucarioPlayMode.log`,
`BossTempoWaterPlayMode.log`, `BossTempoPlayer.log` và `LucarioPlayerCapture.result.txt`.
Kết quả tự động xác nhận hành vi và khả năng né trong các tình huống thử;
cảm nhận độ khó thực tế vẫn cần phản hồi khi chơi.
