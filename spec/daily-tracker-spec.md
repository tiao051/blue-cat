# Daily Tracker — Spec v3.2

Phần chính là để tra cứu lúc code. Lý do đằng sau mỗi quyết định nằm ở Phụ lục A.

**v3.2 chốt:** cắt scope v1 (tầng Tháng/Năm, màn Cài đặt, khối 10-tốt/10-tệ → v2; giữ một dòng mục tiêu năm read-only) · giấc ngủ thuộc về ngày nó ảnh hưởng · `dayType` mặc định theo lịch · ngày ốm chỉ hỏi phục hồi · dữ liệu sửa được tới khi đóng sổ · mã tuần ISO 8601.

---

## 1. App này là cái gì

Một công cụ cho một người dùng duy nhất, gồm hai nửa gắn vào nhau:

- **Lên plan** — mục tiêu năm chia xuống tháng, tháng chia xuống tuần, tuần chia xuống ngày.
- **Tự quan sát** — mỗi ngày ghi vài con số về giấc ngủ, tâm trạng, hiệu suất và việc đã làm.

Sau vài tháng, nhìn lại toàn bộ để tự nhận ra lịch trình nào làm mình sống tốt hơn. App không kết luận hộ; nó bày dữ liệu ra đủ dày và đủ trung thực để người dùng tự diễn giải.

**Không làm:**
- Task manager cho công việc công ty (việc công ty hiện ra để lên plan cho thực tế, nhưng không vào phân tích)
- Project management: dependency, sub-task, priority matrix
- Multi-user, share, social
- Tự động kết luận nhân quả

---

## 2. Một ngày dùng app trông như thế nào

**7h sáng.** Mở app trên điện thoại. Ba câu: đêm qua ngủ từ mấy giờ tới mấy giờ; hôm qua dùng điện thoại mấy tiếng (chưa nhớ thì bấm để sau); sáng nay trong người thấy thế nào. Xong là hôm qua đóng sổ, và app đổ ra màn Hôm nay.

**Màn Hôm nay** chia hai vùng. Trên là Cuộc sống: một lưới icon habit, rồi mấy việc vụn. Dưới là khối Công việc, nền xám, thu gọn được — nó ở đây chỉ để bạn biết hôm nay còn bao nhiêu chỗ trống, không tính vào bất cứ chỉ số nào.

**Trong ngày.** Chạm ô habit để tick hoặc nhập số giờ. Xong việc thì tick. Ticket đang làm dở thì bấm "hôm nay có làm".

**11h đêm.** Ba màn: ba thang điểm cho hiệu suất, tâm trạng, thể trạng; chọn tối đa hai mục cho câu tâm trí chủ yếu ở đâu; một ô text muốn ghi thì ghi. Ngày nghỉ thì màn đầu có thêm hai thang: có phục hồi không, và thời gian rảnh dùng có ý nghĩa không.

**Chủ nhật.** Mở tab Kế hoạch, tầng Tuần: đặt chỉ tiêu cho tuần tới, ném vào backlog những việc biết là phải làm nhưng chưa biết ngày nào.

**Cuối tháng, trên laptop.** Mở tab Phân tích. Một cái lưới: mỗi hàng một ngày, mỗi cột một chỉ số, mỗi ô tô màu theo giá trị. Cả tháng trên một màn hình. Nhìn một lúc thì bắt đầu thấy: mấy ngày màu tối thường đi liền sau mấy đêm ngủ muộn.

**Ngày quên mở app.** Không có gì xảy ra. Không thông báo, không streak bị đứt, không lời trách. Ngày đó đánh dấu khuyết và bị loại khỏi phân tích. Không có cách nào điền bù.

---

## 3. Yêu cầu gốc

Đóng băng — giữ nguyên văn. Đổi ý thì thêm dòng mới ghi rõ nó thay thế dòng nào, **không sửa dòng cũ**.

| # | |
|---|---|
| R1 | To-do list lên plan việc ngày/tuần/tháng, gồm việc muốn làm và việc phải làm |
| R2 | So sánh giữa các ngày: làm được những thứ này thì mood và thể trạng có tốt hơn không |
| R3 | Sáng lên plan · trong ngày tick dần · tối review + lên plan cho mai |
| R4 | Tự nhìn ra pattern. App chỉ nêu tương quan cụ thể, không cần cao siêu |
| R5 | Mục đích cuối: tìm lịch trình phù hợp cho từng thời điểm |
| R6 | Cuối ngày tự rate mood nhanh + ô text không bắt buộc |
| R7 | Lên plan 1 ngày dưới 10 phút. Không có trần thời gian nhập, nhưng phải tiện |
| R8 | Một người dùng. Web chính + mobile. Sync giữa laptop và điện thoại |
| R9 | Nhanh, nhưng đúng stack (.NET + Vue) để học luôn |
| R10 | Việc công ty là bắt buộc → không tính vào đo lường phát triển |
| R11 | Thay vào đó đo hiệu suất làm việc trong ngày |
| R12 | Việc lớn (du học) → mục tiêu năm, chỉ cần một dòng nhắc |
| R13 | Việc vụn tick xong rồi bỏ, nhưng ngày không làm hết thì cũng phải quan tâm |
| R14 | Trạng thái nền là thứ quan tâm nhất |
| R15 | Biến nền: giờ ngủ, giờ thức, screen time, attention dành cho gì |
| R16 | Habit: gym, đọc sách, học tech, học ngôn ngữ |
| R17 | Hiệu suất dùng thang điểm, tự estimate |
| R18 | Miss thì đánh miss, không cho nhập bù |
| R19 | ~~Attention chỉ cần mức nhẹ (chọn một)~~ → thay bởi R24 |
| R20 | Thống nhất thang 1–10 |
| R21 | Task kéo sang ngày sau được, ticket nhiều ngày là bình thường, có `in_progress` |
| R22 | Đọc/học đo theo tiếng; gym tick theo ngày + rate chất lượng buổi tập |
| R23 | Ngày nghỉ đo riêng: chất lượng phục hồi và thời gian rảnh có ý nghĩa không |
| R24 | Attention chọn được nhiều mục — **thay thế R19** |
| R25 | Màn hình mobile không được dày đặc thông tin |
| R26 | Việc cuộc sống và việc công ty phải tách khỏi nhau trên màn hình |
| R27 | Màn Hôm nay có nhìn lại hôm qua (read-only) và plan việc cho ngày mai — thêm ở v3.2 khi kéo phần task của M2 lên M1 |

---

## 4. Nguyên tắc

| # | | Hệ quả |
|---|---|---|
| 1 | Ma sát thấp quan trọng hơn nhanh | Mở ra là nhập được ngay; luồng chính không cần bàn phím |
| 2 | Không nhập bù cho đánh giá chủ quan | Dữ liệu khách quan được để sau trong hạn (mục 5) |
| 3 | Không mất dữ liệu đã ghi thật | Ngày thiếu trường → `partial`. Chạm nhầm không xoá dữ liệu |
| 4 | Điện thoại và laptop luôn thấy cùng dữ liệu | Server là nguồn sự thật duy nhất, không có DB local |
| 5 | Phải mở được trên điện thoại lúc 11h đêm | Backend chạy 24/7 từ M1 |
| 6 | Thêm/bớt/đổi biến theo dõi không cần sửa code | Config-driven (mục 5) |
| 7 | `no_data` khác `not_done` khác `0` | Không dùng boolean cho habit |
| 8 | **Không có gì tích luỹ theo chuỗi** | Cấm streak, số ngày liên tiếp, tổng cộng dồn, huy hiệu. **Cho phép** số đếm reset theo chu kỳ: còn 2 việc hôm nay, 2/3 buổi tuần này |
| 9 | Dữ liệu cũ luôn đọc được sau khi đổi config | Không sửa ý nghĩa của `key` đã có dữ liệu; chỉ tiêu có mốc hiệu lực |
| 10 | Mỗi màn mobile làm một việc | Nhập thì gập bớt thứ không dùng; nhìn thì nén lại thành lưới |

Nguyên tắc 8 phân biệt hai thứ hay bị lẫn. Cái tích luỹ theo chuỗi tạo áp lực **giữ chuỗi** — tick bừa để khỏi mất 47 ngày liên tiếp. Cái reset theo chu kỳ chỉ trả lời "còn bao nhiêu", và mỗi tuần lại về vạch xuất phát nên không có gì để mất.

---

## 5. Cơ chế lõi: config-driven

Không có form nào được viết tay. Định nghĩa các trường nằm trong DB; frontend đọc định nghĩa rồi tự dựng màn hình. Thêm một biến theo dõi mới = insert một document, không code, không migration, không deploy.

Một component `MetricField` duy nhất, switch theo `type` để chọn widget. Màn check-in dựng bằng cách: lấy definitions, lọc theo `phase` và `visibleWhen`, sắp theo `order`, lặp qua render.

### Định nghĩa một biến gồm những gì

| Trường | Ý nghĩa |
|---|---|
| `key` | định danh, không bao giờ đổi |
| `label` | tên hiển thị |
| `type` | quyết định widget — bảng dưới |
| `phase` | morning · evening · anytime |
| `order` | thứ tự trong màn |
| `visibleWhen` | điều kiện hiện, để trống là luôn hiện |
| `deferrable` | cho phép để sau bao nhiêu ngày |
| `polarity` | cao là tốt hay cao là xấu |
| `validation` | min, max, bắt buộc hay không |
| `options`, `maxSelect` | cho kiểu chọn |
| `active` | tắt biến, không xoá |

### `type` được hỗ trợ

| type | widget | ghi chú |
|---|---|---|
| `scale` | swipe-scale | dải 10 ô màu, vuốt ngang, số lớn phía trên |
| `number` | stepper | bước cấu hình được, 0.5 cho giờ |
| `time` | time picker | nhớ giá trị lần trước làm default |
| `enum` | chip chọn một | |
| `multi_enum` | chip chọn nhiều | bắt buộc có `maxSelect` |
| `text` | textarea | optional, không giới hạn độ dài |

### `polarity`

Nhận `higher_better` hoặc `higher_worse`. Quyết định chiều thang màu ở màn Phân tích. Không có nó thì cột screen time (cao là xấu) tô cùng chiều với cột tâm trạng (cao là tốt), và cả bảng đọc sai.

### `visibleWhen`

Chỉ là một phép khớp giá trị đơn giản: tên trường và danh sách giá trị chấp nhận được. Ví dụ `recovery` chỉ hiện khi `dayType` thuộc nhóm ngày nghỉ. **Không xây expression language** — làm DSL thì mất cả tháng cho parser thay vì thu thập dữ liệu.

### `deferrable`

Khai báo số ngày còn được ghi sau ngày mà giá trị thuộc về. Giá trị **thuộc về ngày X và được ghi vào document của ngày X**, nhưng ghi được tới hết ngày X+n.

Trong luồng check-in, field như vậy có nút "để sau", không chặn đi tiếp; nó rơi xuống danh sách màn Hôm nay kèm ngày mà nó thuộc về. Quá hạn thì `no_data`.

**Chỉ đặt cho dữ liệu khách quan** — con số đã tồn tại sẵn ở đâu đó, app chỉ chép lại. Mọi thang 1–10 không bao giờ `deferrable`.

`status` của một ngày chỉ do check-in sáng/tối quyết định; field `deferrable` chưa điền không giữ ngày ở trạng thái lửng lơ.

### Quy tắc versioning (bắt buộc)

Không bao giờ sửa ý nghĩa của một `key` đã có dữ liệu. Muốn đổi `productivity` từ thang 1–10 sang 1–5: tắt cái cũ, tạo `productivity_v2`. Đổi `label` thì thoải mái — chỉ ý nghĩa và thang đo là bất biến.

Vi phạm sẽ làm dữ liệu trước và sau không so sánh được, và **không có lỗi nào báo ra**.

---

## 6. Data model

Sáu collection.

### `metric_definitions`
Theo mục 5.

### `habits`

| Trường | Ghi chú |
|---|---|
| `label` | tên đầy đủ, dùng ở Cài đặt và Phân tích |
| `shortLabel` | tối đa ~8 ký tự, chỉ dùng trong ô lưới |
| `icon` | tên icon từ bộ có sẵn |
| `measure` | `binary` hoặc `duration` |
| `hasQuality` | có kèm chấm điểm 1–10 không |
| `qualityLabel` | câu hỏi khi chấm điểm |
| `active`, `order` | |

### `habit_targets`

| Trường | Ghi chú |
|---|---|
| `habitId` | |
| `period` | chỉ nhận `week` |
| `target`, `unit` | số buổi hoặc số giờ |
| `effectiveFrom` | tuần đầu tiên áp dụng chỉ tiêu này |

Đổi chỉ tiêu = tạo bản ghi mới với `effectiveFrom` là tuần tới, **không sửa bản ghi cũ**. Tuần cũ luôn được chấm theo chỉ tiêu đang có hiệu lực lúc đó (nguyên tắc 9).

Mã tuần và mã tháng theo ISO 8601, tuần bắt đầu thứ Hai — ví dụ `2026-W32`, `2026-08`. Dùng chung cho `effectiveFrom` và `scopeKey` của task.

Chỉ tiêu chỉ đặt ở tầng tuần. Đặt cả tuần lẫn tháng cho cùng một habit thì hai con số sẽ mâu thuẫn và người dùng phải tự đối chiếu.

### `daily_entries`
Một document một ngày, `date` là khoá duy nhất.

| Trường | Ghi chú |
|---|---|
| `date` | dạng năm-tháng-ngày theo giờ local |
| `status` | `open` · `closed` · `partial` · `missed` |
| `dayType` | `workday` · `weekend` · `dayoff` · `sick` — mặc định T7/CN là `weekend`, còn lại `workday`; đổi bằng chạm header màn Hôm nay |
| `values` | túi key-value, key lấy từ `metric_definitions` |
| `habits` | mỗi habit một mục: `state`, `hours`, `quality` |
| `quickPlanned` | mẫu số — chốt cứng, xem dưới |
| `quickDone` | tử số |
| `quickAddedLater` | việc thêm sau khi đã chốt mẫu số |
| `ongoingTouched` | số việc nhiều ngày có động vào |
| các mốc thời gian | check-in sáng, check-in tối, đóng sổ, cập nhật |

**`state` của habit** luôn có ba giá trị `done` · `not_done` · `no_data`, kể cả với habit đo giờ. `hours` bằng 0 là dữ liệu thật, khác hẳn `no_data`. `quality` chỉ tồn tại khi habit có chấm điểm và `state` là `done`.

**Quy tắc chốt mẫu số:** `quickPlanned` chốt tại thời điểm check-in sáng kết thúc, và không tăng nữa. Việc thêm vào sau đó đi vào `quickAddedLater`, làm xong thì tăng `quickDone` nhưng không tăng mẫu số. Tỉ lệ vì thế có thể vượt 1 — chủ ý, không phải lỗi.

Không chốt thì tối thêm một việc rồi làm luôn sẽ ra tỉ lệ 1/1 và ngày đó nhìn như hoàn hảo — trong khi R13 muốn bắt điều ngược lại: ngày plan nhiều mà không làm hết. Ngày không check-in sáng thì `quickPlanned` là `no_data`, ngày đó không vào phép so sánh tỉ lệ.

### `tasks`

| Trường | Ghi chú |
|---|---|
| `title` | |
| `category` | `personal` hoặc `work` — `work` không vào phân tích |
| `kind` | `quick` hoặc `ongoing` |
| `scope` | `day` · `week` — `month` để v2, cùng lúc với tầng Tháng |
| `scopeKey` | ngày cụ thể, hoặc mã tuần, hoặc mã tháng |
| `plannedDate` | chỉ có khi `scope` là `day` |
| `status` | `todo` · `in_progress` · `done` · `dropped` |
| `originalDate`, `carryCount`, `touchedDates`, `doneAt` | |

Việc ở backlog tuần có `scope` là `week` và chưa có `plannedDate`. Bấm "gán ngày" thì set `plannedDate` và chuyển `scope` thành `day`. Hết tuần mà chưa gán thì **không tự trôi sang tuần sau** — nó ở lại tuần cũ như việc chưa xong.

| | `quick` | `ongoing` |
|---|---|---|
| Ví dụ | photo tài liệu, giặt đồ | ticket kéo 5 ngày |
| Vào tử/mẫu? | có | không |
| Tín hiệu | done / not done | hôm nay có động vào không |

### `goals`

| Trường | Ghi chú |
|---|---|
| `title` | |
| `scope` | `year` hoặc `month` |
| `targetDate` | không bắt buộc |
| `parentId` | mục tiêu tháng treo dưới một mục tiêu năm |
| `status`, `active` | |

Mục tiêu năm không bắt buộc có mốc thời gian — có loại đo gián tiếp qua habit chứ không qua milestone.

**v1:** collection vẫn tạo ở M0 nhưng chỉ seed đúng một document (mục tiêu năm — du học), hiển thị read-only ở đầu tab Kế hoạch (R12: một dòng nhắc). CRUD và mục tiêu tháng để v2.

---

## 7. Vòng đời một ngày

Check-in sáng của ngày D làm hai việc: đóng sổ ngày D-1, và mở sổ ngày D. Trong ngày thì tick habit và task. Tối làm check-in tối. Sáng hôm sau lại đóng sổ.

| Tình huống | status |
|---|---|
| Có check-in tối + check-in sáng hôm sau | `closed` |
| Thiếu một phần | `partial` — giữ dữ liệu đã có, trường thiếu là `no_data` |
| Không mở app cả ngày | `missed` |

Ngày D đóng khi có check-in sáng của D+1, hoặc khi D+1 đã trôi qua. Không dùng mốc nửa đêm, vì thường ngủ sau đó.

**Sửa dữ liệu:** ngày chưa đóng thì mọi giá trị sửa được — mở lại check-in để sửa, chạm ô habit theo quy tắc mục 9.2. Ngày đã đóng thì khoá vĩnh viễn; ngoại lệ duy nhất là field `deferrable` còn trong hạn (mục 5). Sửa nhầm lẫn khác nhập bù: nhập bù là điền cho ngày đã trôi qua, sửa là chữa giá trị vừa ghi trong ngày.

**Không có cron đóng sổ:** `missed` và `partial` chốt lazy — tính lúc mở app lần kế hoặc lúc query. Ngày không có document nghĩa là `missed`.

**Phân tích:** mỗi phép so sánh chỉ dùng ngày có đủ trường nó cần, luôn hiển thị số ngày dùng được. Mặc định tách theo `dayType`, không bao giờ trộn ngày làm với ngày nghỉ.

---

## 8. Seed data

### Biến buổi sáng
| key | label | type | deferrable |
|---|---|---|---|
| `sleep_start` | Giờ đi ngủ đêm qua | time | — |
| `sleep_end` | Giờ thức | time | — |
| `screen_time` | Screen time (giờ) | number | 1 ngày |
| `mood_morning` | Tâm trạng sáng | scale | — |

Số giờ ngủ tính tự động từ hai mốc (xử lý qua nửa đêm: 23:30 → 07:00 là 7.5 giờ), không phải biến nhập tay. `screen_time` có polarity cao là xấu.

**Ngày sở hữu:** giấc ngủ đêm D-1 → sáng D ghi vào document ngày D — ngày nó ảnh hưởng, để phép so sánh "ngủ muộn → hôm sau tệ" đọc thẳng trên một hàng. `screen_time` thuộc về D-1 và ghi vào document D-1 (quy tắc `deferrable`, mục 5).

### Biến buổi tối
| key | label | type | chỉ hiện khi |
|---|---|---|---|
| `productivity` | Hiệu suất | scale | |
| `mood_evening` | Tâm trạng cuối ngày | scale | |
| `physical` | Thể trạng | scale | |
| `attention_main` | Tâm trí chủ yếu ở đâu | multi_enum, tối đa 2 | |
| `recovery` | Có thực sự phục hồi không | scale | `weekend` · `dayoff` · `sick` |
| `time_meaningful` | Thời gian rảnh dùng có ý nghĩa không | scale | `weekend` · `dayoff` |
| `note` | Ghi chú | text | |

Mọi thang đo là 1–10.

Ngày ốm hỏi phục hồi nhưng không hỏi "thời gian rảnh có ý nghĩa không" — nằm bẹp thì câu đó không công bằng, và điểm thấp của ngày ốm sẽ làm nhiễu dữ liệu ngày nghỉ thật.

Lựa chọn cho `attention_main`: công việc · học & phát triển · cày phone, giải trí · xã hội, người khác · trống rỗng

### Habit
| label | shortLabel | icon | measure | chấm điểm |
|---|---|---|---|---|
| Gym / vận động | gym | barbell | binary | ✅ "Buổi tập có tốt không?" |
| Đọc sách | đọc | book | duration | — |
| Học tech ngoài giờ làm | tech | code | duration | — |
| Luyện RP | RP | microphone | duration | — |
| Ra khỏi nhà / gặp người khác | ra ngoài | door-exit | binary | — |

Quy ước: "học tech" chỉ đếm thời gian ngoài giờ làm chính thức. `productivity` chấm cho cả ngày.

---

## 9. Màn hình

Nav bốn mục: **Hôm nay · Kế hoạch · Phân tích · Cài đặt**. Trên mobile, mục Phân tích vẫn hiện nhưng dẫn tới một thông báo ngắn gợi ý mở trên máy tính.

### 9.1 Check-in sáng — 3 bước
Full-screen, một câu hỏi một màn, tối ưu ngón cái, thanh tiến trình ba vạch ở đầu.

1. Giấc ngủ — hai time picker, hiện tổng số giờ tính tự động
2. Screen time hôm qua — stepper, có nút "để sau"
3. Tâm trạng sáng — swipe-scale

Xong là đổ ra màn Hôm nay, và `quickPlanned` chốt tại đây. **Không có bước lên plan riêng** — màn Hôm nay đã làm việc đó.

### 9.2 Hôm nay
Header: ngày và `dayType` ở góc phải, chạm để đổi. Mặc định T7/CN là `weekend`, còn lại `workday`.

**Vùng Cuộc sống**
- Lưới icon habit, 4 cột. Ô luôn hiện icon và `shortLabel`. Giá trị hiện ở góc trên phải dạng nhãn nhỏ, **không thay thế tên habit**.
- Bên dưới là việc cá nhân trong ngày, mỗi việc một dòng, kèm dòng "Thêm việc".
- Field để sau chưa điền hiện thành dòng thường, ghi rõ ngày nó thuộc về.

**Ba vùng thời gian (R27, thêm v3.2):** trên cùng là khối "Hôm qua" read-only (trạng thái sổ, việc đã/chưa làm, habit đã tick — không sửa được, R18 giữ nguyên); dưới cùng trước CTA tối là khối "Ngày mai" để plan trước việc (R3: tối lên plan cho mai). Việc thêm cho ngày mai vào mẫu số của ngày mai lúc check-in sáng hôm sau.

**Vùng Công việc**
- Khối riêng trên nền xám nhạt, chữ nhạt hơn một bậc.
- Thu gọn hoặc xổ ra được. Lúc thu gọn hiện số việc chưa xong hôm nay; lúc xổ ra hiện thêm dòng "không tính vào đo lường".
- Nhớ trạng thái thu/xổ lần cuối. **Không tự đoán theo giờ trong ngày.**

**Quy tắc tương tác ô habit**

| Loại | Chạm ô chưa nhập | Chạm ô đã nhập |
|---|---|---|
| binary, không chấm điểm | tick luôn, không mở dải | bỏ tick |
| binary có chấm điểm | tick rồi mở dải chấm điểm | mở dải để sửa, có nút bỏ tick |
| duration | mở dải chọn giờ | mở dải để sửa |

Dải nhập mở **dưới lưới**, lưới không xê dịch. Chỉ một dải mở tại một thời điểm. Chạm vào ô đã có dữ liệu **không bao giờ xoá dữ liệu đó** — phải bấm nút trong dải.

### 9.3 Check-in tối — 3 bước
1. **Ba thang gộp một màn**: hiệu suất, tâm trạng, thể trạng. Ngày nghỉ thành năm thang; ngày ốm bốn (không có `time_meaningful`).
2. Tâm trí chủ yếu ở đâu — chip chọn tối đa 2
3. Ghi chú — textarea, có nút bỏ qua

### 9.4 Kế hoạch
**v1 chỉ có tầng Tuần**, kèm một dòng mục tiêu năm read-only ở đầu tab (seed sẵn — R12). Tầng Tháng và Năm đầy đủ để v2; mô tả giữ lại dưới đây để khỏi thiết kế lại.

**Tuần** — hiện tuần hiện tại kèm số ngày còn lại.
- Chỉ tiêu habit: mỗi habit có target là một dòng, dạng "2 / 3 buổi" kèm thanh tiến độ. Chỉ tiêu lấy theo bản có hiệu lực cho tuần đó.
- Việc trong tuần chưa gán ngày: mỗi dòng có nút "gán ngày"

**Tháng (v2)** — mục tiêu tháng, mỗi cái treo dưới một mục tiêu năm.

**Năm (v2)** — mỗi mục tiêu là một card, bên trong liệt kê các mục tiêu tháng thuộc về nó kèm trạng thái và tháng dự kiến.

### 9.5 Phân tích — desktop
**Không dùng được trên mobile** và không cố làm cho dùng được: 31 ngày nhân 9 cột trên màn 390px thì mỗi ô còn 20px.

Lưới: hàng là ngày, cột là chỉ số, ô tô màu theo giá trị, chiều thang màu theo `polarity`. Ô `no_data` gạch chéo nền xám, phải nhìn ra ngay, không lẫn với giá trị thấp. Click một ngày mở chi tiết và note. Lọc theo `dayType`.

**(v2)** Khối "10 ngày tốt nhất vs 10 ngày tệ nhất" theo tâm trạng cuối ngày: đặt cạnh nhau, liệt kê chỉ số lệch hẳn giữa hai nhóm, kèm toàn bộ note của những ngày đó. Cần cỡ 2 tháng dữ liệu mới có nghĩa nên lùi lại.

### 9.6 Cài đặt (v2)
v1 không có màn này: seed và chỉnh config bằng migration hoặc insert thẳng vào DB — config-driven nên form tự cập nhật, không cần deploy.

Hai danh sách: **Habit** và **Biến theo dõi**. Mỗi danh sách có dòng "Thêm".

Tắt một biến là đánh dấu ngừng dùng, **không xoá** — dữ liệu cũ phải đọc được.

---

## 10. Hạ tầng

**Stack:** Vue 3 + TypeScript / .NET / MongoDB / GraphQL.

**DB:** MongoDB Atlas M0 — free vĩnh viễn, 512 MB. App sinh khoảng 365 document mỗi năm, mỗi cái cỡ 1KB.

**Backend:** Oracle Cloud Always Free, máy ARM Ampere A1 (2 OCPU / 12 GB sau đợt giảm hạn mức tháng 6/2026).
- Cần thẻ tín dụng để xác minh, không bị tính tiền nếu ở trong hạn mức
- Region hay báo hết capacity khi tạo máy ARM — fallback sang máy AMD micro, vẫn đủ
- Máy nhàn rỗi lâu có thể bị thu hồi, một cron nhẹ là xong
- Chạy ARM64 nên Dockerfile phải build đúng target
- **Dự phòng** nếu account bị chặn: máy ở nhà cộng Cloudflare Tunnel

**Loại trừ:** Render free tier (ngủ khi không có traffic, cold start khoảng 50 giây mỗi lần mở). Railway và Fly.io (2026 đã chuyển sang trial hoặc tính theo mức dùng).

**Sync:** server là nguồn sự thật duy nhất, cả hai client gọi cùng API. Mốc cập nhật theo từng trường, ai ghi sau thắng.

**GraphQL với dữ liệu động:** danh sách các cặp key–giá trị, mỗi cặp có sẵn ô cho từng kiểu dữ liệu. Client dù sao cũng phải lấy definitions về để dựng form.

**Type safety:** một file hằng số ở client cho những key mà màn Phân tích gọi đích danh.

**Timezone:** ngày lưu dạng chuỗi năm-tháng-ngày theo giờ local, không dùng timestamp UTC.

**Auth:** một khoá bí mật trong header, hoặc Cloudflare Access.

**Hệ thống thị giác:** dùng một bộ component có sẵn cho Vue, chỉ đổi màu nhấn và font. Không tự dựng từ đầu.

---

## 11. Backlog

| M | Nội dung |
|---|---|
| **M0** | Atlas cluster · schema + seed · migration runner |
| **M1** | Config-driven form renderer · check-in sáng + tối · **tick habit tối thiểu** · deploy 24/7 · vào được từ điện thoại |
| **M2** | Màn Hôm nay đầy đủ: lưới icon, hai vùng, khối công việc thu gọn |
| **M3** | Kế hoạch: tầng Tuần (chỉ tiêu + backlog) · dòng mục tiêu năm read-only |
| **M4** | Phân tích: lưới màu trên desktop |
| **M5** | Export CSV |
| **M6** | PWA, offline queue, install lên home screen |

Đẩy sang v2 (đã quyết ở v3.2): tầng Tháng/Năm đầy đủ · màn Cài đặt · khối 10-tốt/10-tệ — xem Phụ lục C.

M1 là milestone duy nhất có deadline thật: dữ liệu không lấy lại được, mỗi tuần chưa xong là mất vĩnh viễn một tuần.

**Vì sao M1 phải có tick habit.** Habit không phải biến theo dõi nên không đi qua form renderer; chỗ tick chúng là màn Hôm nay ở M2. Nếu M1 chạy vài tuần trước khi M2 xong thì có dữ liệu ngủ, tâm trạng, hiệu suất mà **trống hoàn toàn cột gym, đọc, tech** — tức là trống đúng nửa quan trọng của câu hỏi gốc. Một danh sách checkbox xấu xí cũng được, miễn là có.

### M0 xong khi
- [ ] Atlas cluster chạy, connection string trong env
- [ ] 6 collection tạo xong, ngày trong `daily_entries` là khoá duy nhất
- [ ] Seed 11 biến, 5 habit ở mục 8 và 1 mục tiêu năm chèn được bằng migration runner
- [ ] Chạy lại migration lần hai không nhân đôi dữ liệu

### M1 xong khi
- [ ] Query trả về definitions đã lọc theo `phase` và `dayType`
- [ ] `MetricField` render đủ 6 kiểu
- [ ] Check-in sáng ghi được, đóng sổ ngày hôm trước đúng trạng thái, chốt mẫu số
- [ ] Nút "để sau" hoạt động, field bị hoãn xuất hiện kèm đúng ngày
- [ ] Check-in tối ghi được, điều kiện hiện hoạt động (ngày nghỉ có thêm 2 thang)
- [ ] Giới hạn chọn tối đa 2 chặn đúng
- [ ] **Tick được 5 habit, phân biệt đủ ba trạng thái, nhập được số giờ**
- [ ] Backend chạy 24/7, có HTTPS, mở từ 4G ngoài đường được
- [ ] Nhập trên điện thoại, mở laptop thấy ngay
- [ ] Thêm một biến mới bằng cách insert document, form tự mọc thêm ô, không deploy lại

---

## 12. Còn treo

- [x] **Cắt scope cho v1** — đã quyết ở v3.2: cắt cả ba, giữ một dòng mục tiêu năm read-only đúng nghĩa R12. Xem mục 11 và Phụ lục C
- [ ] Các trạng thái rỗng chưa thiết kế: ngày đầu tiên, lưới Phân tích khi mới có 3 ngày, màn Hôm nay khi chưa có việc nào
- [ ] Thanh tiến độ chỉ tiêu tuần có tạo áp lực tick bừa không — dùng thật vài tuần rồi quyết, bỏ thanh chỉ để số nếu có
- [ ] Dải 10 ô trên màn hẹp: mỗi ô khoảng 24px, hẹp hơn vùng chạm khuyến nghị. Vuốt thì ổn, chạm thẳng hay trượt. Nếu khó chịu thì rút xuống 1–5, đổi lại phải thêm một dòng thay thế R20
- [ ] Khối Công việc có nên biến mất hẳn sau buổi sáng không
- [ ] Tài khoản Oracle có được duyệt không — biết trong ngày đầu

---
---

# Phụ lục A — Vì sao

**Điểm mood buổi sáng.** Giả thuyết trung tâm là hiệu suất cao dẫn tới tâm trạng tốt. Nhưng chiều ngược lại cũng hợp lý: sáng dậy thấy khoẻ nên mới làm được nhiều, tối chấm cao. Chỉ có điểm buổi tối thì hai câu chuyện này tạo ra số liệu giống hệt nhau. Có điểm buổi sáng thì câu hỏi đổi thành: với những ngày sáng dậy tương đương nhau, ngày nào làm được nhiều thì tối có khá hơn không.

**Phục hồi và ý nghĩa tách đôi.** Chúng kéo ngược nhau — nằm xem phim cả ngày thì phục hồi 8 ý nghĩa 2; cày side project 10 tiếng thì ý nghĩa 9 phục hồi 2. Gộp thành một điểm sẽ xoá mất đúng thông tin đáng giá nhất. Giả thuyết đáng kiểm chứng: tuần kiệt sức là tuần có ngày nghỉ ý nghĩa cao nhưng phục hồi thấp.

**Luôn tách theo loại ngày khi phân tích.** Hiệu suất mang nghĩa khác nhau giữa ngày làm và ngày nghỉ. Chủ nhật nghỉ trọn vẹn chấm hiệu suất 2 — con số không sai, nhưng trộn chung với ngày đi làm thì phân tích đọc thành ngày tệ.

**Trạng thái `partial`.** "Miss là miss" đúng ở chỗ không cho điền bù. Nhưng xoá luôn những tick đã làm ban ngày là tự huỷ dữ liệu thật.

**Cho phép để sau với dữ liệu khách quan.** 7h sáng không ai nhớ hôm qua dùng điện thoại mấy tiếng — bắt nhập ngay là tạo ma sát vô nghĩa và sẽ nhận về số bịa. Nhưng screen time khác tâm trạng ở một điểm: điện thoại đã ghi sẵn, đọc lúc nào con số cũng thế, không có gì để bypass.

**Ghi vào đúng ngày sở hữu giá trị.** Bản trước lưu screen time vào document hôm nay với nghĩa "của hôm qua". Cách đó buộc mọi phép phân tích phải nhớ lệch một ngày — sớm muộn cũng quên, và sai kiểu đó không có lỗi nào báo ra. Giấc ngủ định nghĩa "sở hữu" theo hướng ảnh hưởng: đêm D-1 → sáng D thuộc về ngày D, vì câu hỏi là "ngủ thế nào → hôm nay ra sao", không phải ngày bắt đầu nhắm mắt.

**Đo giờ thay vì ngưỡng nhị phân.** Ngưỡng nhị phân khoá cứng quyết định ngay lúc thu thập — tick "đọc trên 20 phút" thì ngày đọc 25 phút và ngày đọc 3 tiếng nằm chung một ô, ba tháng sau không hỏi lại được. Ghi số giờ thì ngưỡng chuyển sang lúc phân tích.

**Số 0 khác không có dữ liệu.** Ngày chủ động ghi "đọc 0 tiếng" là dữ liệu thật và có giá trị. Ngày quên check-in thì không phải 0.

**Tách việc một ngày và việc nhiều ngày.** Ticket kéo 5 ngày nếu nhét vào tử/mẫu sẽ bị tính chưa xong 4 ngày liên tiếp, kéo điểm 4 ngày đó xuống dù làm việc tốt.

**Lưu tử số và mẫu số, không lưu tỉ lệ.** Ngày khoẻ plan 8 làm 5 ra 0.62; ngày mệt plan 2 làm 2 ra 1.0. Chỉ lưu tỉ lệ thì app kết luận ngược.

**Chốt mẫu số ở check-in sáng.** Không chốt thì tối thêm một việc rồi làm luôn sẽ ra 1/1, ngày đó nhìn như hoàn hảo — trong khi ý định gốc là bắt ngày plan quá tay mà không làm hết.

**Config-driven thay vì hardcode cột.** Hai tháng đầu sẽ đổi biến theo dõi cỡ chục lần. Hardcode thì mỗi lần đụng năm chỗ: class C#, GraphQL type, resolver, TS type, form Vue.

**Lấy pattern config-driven từ CoverGo nhưng bỏ phần thừa.** CoverGo config-driven vì là platform đa tenant — lý do kinh doanh. App này một người dùng nên bỏ multi-tenancy, versioning có UI, migration tooling cho config, và expression language.

**Chỉ tiêu có mốc hiệu lực.** Đổi "gym 3 buổi" thành 4 mà sửa tại chỗ thì mọi tuần cũ bị chấm lại theo chỉ tiêu mới. Cùng một lỗi với việc sửa ý nghĩa của một biến đã có dữ liệu.

**Attention chặn ở 2 lựa chọn.** Chọn thoải mái thì thực tế sẽ tick 3–4 mục mỗi tối, và biến mất sạch khả năng phân biệt. Câu hỏi gốc là tâm trí **chủ yếu** ở đâu; giữ được chữ chủ yếu thì mới còn tín hiệu.

**Lưới icon thay danh sách dòng.** Quy ước thống trị trong nhóm app này: một lưới nén 8–12 mục vào chỗ mà danh sách chỉ chứa 4, và mắt quét lưới nhanh hơn quét dòng. Tên habit không bao giờ bị giá trị thay thế, nếu không thì ba tháng sau nhìn lưới không biết ô đó là gì.

**Ba thang gộp một màn buổi tối.** Giữ "một câu một màn" thì buổi tối thành 5 màn, ngày nghỉ thành 7 — quá dài cho việc làm mỗi đêm.

**Bỏ bước lên plan khỏi check-in sáng.** Nó trùng hoàn toàn với màn Hôm nay: chọn habit, thêm việc, xác nhận loại ngày.

**Thang màu chỉ đi nhạt sang đậm, không đỏ vàng xanh.** Quy ước đỏ nghĩa là sai. Dùng nó cho tâm trạng tức là app đang phán xét người dùng vì hôm nay thấy tệ. Đậm nhạt truyền đạt cùng lượng thông tin mà không kèm phán xét.

**Việc công ty hạ một bậc thị giác.** Không chỉ tách nhóm — nó thuộc hạng khác vì không vào đo lường. Để nó chen giữa thì mắt tưởng nó cũng đang được chấm điểm.

**Nhớ trạng thái thu/xổ thay vì tự đoán theo giờ.** App đoán ý người dùng thì lúc đoán trúng chả ai để ý, lúc đoán trượt thì khó chịu. Với thứ mở hai lần mỗi ngày, đoán được trước quan trọng hơn thông minh.

**Chạm ô đã có dữ liệu không xoá dữ liệu.** Chạm nhầm vào ô 60px trên điện thoại là chuyện thường; không được để nó âm thầm xoá mất buổi tập đã ghi.

**Chỉ tiêu chỉ ở tầng tuần.** Đặt cả tuần lẫn tháng cho cùng một habit thì hai con số mâu thuẫn và người dùng phải tự đối chiếu — mà đó đúng là việc app phải làm hộ.

**Việc backlog tuần không tự trôi sang tuần sau.** Tự trôi thì sau hai tháng backlog thành bãi rác, và mất luôn tín hiệu "tuần đó plan quá tay".

**Phân biệt tích luỹ theo chuỗi và đếm theo chu kỳ.** Streak tạo áp lực giữ chuỗi, dẫn tới tick bừa để khỏi mất 47 ngày liên tiếp — dữ liệu hỏng ngay tại nguồn. Số đếm reset mỗi ngày hoặc mỗi tuần chỉ trả lời "còn bao nhiêu" và không có gì để mất. Bản trước cấm nhầm cả hai, rồi lại tự vi phạm ở mục màn hình.

**Không dùng hệ số tương quan ở v1.** Với n nhỏ, so sánh hai cực trung thực hơn, và nó đẩy việc diễn giải về phía người dùng.

---

# Phụ lục B — Rủi ro

| Rủi ro | Xử lý |
|---|---|
| Nhân quả ngược | Điểm buổi sáng làm biến kiểm soát |
| Độ trễ — ngủ đêm qua ảnh hưởng hôm nay | Lưới màu cho mắt tự bắt; phân tích độ trễ để v2 |
| n nhỏ — 30 ngày, 15 biến nên dễ ra pattern ngẫu nhiên | Không dùng hệ số tương quan, luôn hiện số ngày dùng được |
| Chấm điểm buổi tập có n rất nhỏ, khoảng 12 điểm mỗi tháng | Hiện số ngày riêng cho biến có điều kiện |
| Trộn ngày nghỉ với ngày làm | Mọi phép so sánh tách theo loại ngày |
| Việc nhiều ngày bóp méo tỉ lệ | Tách việc một ngày và việc nhiều ngày |
| Mẫu số phình theo ngày | Chốt ở check-in sáng |
| Cột "cao là xấu" tô cùng chiều với cột "cao là tốt" | Trường polarity |
| Attention mất khả năng phân biệt | Chặn ở 2 lựa chọn |
| Đổi thang đo làm dữ liệu cũ vô dụng | Quy tắc versioning ở mục 5 |
| Đổi chỉ tiêu làm tuần cũ bị chấm lại | Mốc hiệu lực ở `habit_targets` |
| Bỏ dùng sau 2 tuần | Tối ưu ma sát, không streak |
| Feature creep: attention thành time tracking | Chốt chip chọn nhanh, tối đa 2 |
| Scope trôi sang task manager | Việc công ty không vào phân tích |
| **Spec phình nhanh hơn code** | Mục 12 có một dòng riêng cho việc cắt scope. Rà lại mỗi khi thêm màn mới |
| Chỉ tiêu tuần thành streak trá hình | Reset mỗi tuần, không có chuỗi. Theo dõi ở mục 12 |
| Oracle không duyệt account hoặc hết capacity | Fallback Cloudflare Tunnel, biết trong ngày đầu |

---

# Phụ lục C — v2

- **Kế hoạch tầng Tháng + Năm đầy đủ** — CRUD mục tiêu, treo tháng dưới năm, task scope `month` (cắt khỏi v1 ở v3.2)
- **Màn Cài đặt** — UI thêm/sửa habit và biến theo dõi; tạm thời insert thẳng DB
- **Khối 10-tốt/10-tệ** — cần ~2 tháng dữ liệu mới có nghĩa
- **Phân tích độ trễ** — dịch cột dữ liệu đi n ngày rồi so lại. Cần 2–3 tháng dữ liệu mới có ý nghĩa.
- **Nhắc nhở nhẹ** — một thông báo giờ cố định, không lặp, bỏ qua thì thôi.
- **Số lần bị kéo sang ngày khác như một biến theo dõi** — việc bị đẩy nhiều ngày liên tiếp là tín hiệu né tránh, có thể tương quan với tâm trạng.
- **Đối chiếu kế hoạch với thực tế** — tuần nào đặt chỉ tiêu cao hơn khả năng thì tâm trạng cuối tuần thế nào.
