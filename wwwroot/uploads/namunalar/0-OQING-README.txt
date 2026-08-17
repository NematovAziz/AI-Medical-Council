NAMUNA LABORATORIYA HUJJATLARI
AI MEDICAL COUNCIL — Clinical Intelligence Platform

Bu papkadagi fayllar "Analizlar" sahifasidagi drag & drop maydoniga
tashlash uchun tayyorlangan. Har biri boshqa formatni tekshiradi.

--------------------------------------------------------------------
FAYL                              FORMAT   KUTILAYOTGAN NATIJA
--------------------------------------------------------------------
1-umumiy-qon-tahlili.txt          TXT      5 ko'rsatkich, 3 tasi chetda
2-biokimyoviy-tahlil.csv          CSV      10 ko'rsatkich, 8 tasi chetda
3-obshiy-analiz-krovi-ru.txt      TXT      8 ko'rsatkich (rus tilida)
4-analyzer-codes-en.txt           TXT      11 ko'rsatkich (HGB/WBC/PLT)
5-kritik-holat.txt                TXT      7 ko'rsatkich, KRITIK daraja
6-laboratoriya-hisoboti.pdf       PDF      15 ko'rsatkich, matn qatlami
--------------------------------------------------------------------

QANDAY ISHLATILADI
1. Bemorlar -> bemorni tanlang -> Analizlar
2. Faylni maydonga tashlang
3. Ko'rsatkichlar bazaga yoziladi, tashrif yaratiladi va
   AI konsilium avtomatik ishga tushadi

DEMO UCHUN TAVSIYA
5-kritik-holat.txt faylini Sardor Karimovga yuklang:
Troponin I 82, D-dimer 2400, Kaliy 3.1 -> konsilium darhol
KRITIK darajani beradi. Bu eng ta'sirli ko'rsatuv stsenariysi.

CSV HAQIDA
Ajratuvchi belgi nuqtali vergul (;) bo'lishi kerak, chunki oddiy
vergul o'nlik kasr bilan chalkashadi. Excel'da "Save as CSV
(semicolon delimited)" ni tanlang.

RASM VA SKANER
PNG/JPG fayllar va matn qatlamisiz PDF uchun vision modeli kerak:
Sozlamalar -> AI agentlar -> LabExtractor -> Gemini kaliti.
Kalitsiz holatda tizim faylni saqlaydi, lekin ko'rsatkich ajratmaydi.

TANIYDIGAN KO'RSATKICHLAR (lokal parser, kalitsiz ham ishlaydi)
Gemoglobin, Eritrotsitlar, Leykotsitlar, Trombotsitlar, EChT,
Glyukoza, HbA1c, Umumiy xolesterin, Kreatinin, Mochevina, ALT, AST,
Umumiy bilirubin, C-reaktiv oqsil, Ferritin, Kaliy, Natriy, TSH,
D-dimer, Troponin I.
Har biri uchun o'zbek, rus, ingliz nomlari va analizator kodlari
(HGB, WBC, PLT, GLU, CREA, ALT, AST, CRP) tanib olinadi.
