namespace AI.MedicalCouncil.Services.Localization;

public interface ILocalizer
{
    /// <summary>Translated text. The key is the Uzbek-Latin string, so a missing entry degrades to Latin.</summary>
    string this[string key] { get; }

    string Lang { get; }
    string LangLabel { get; }
}

public class Localizer(IHttpContextAccessor accessor) : ILocalizer
{
    public const string CookieName = "amc_lang";
    public const string Latin = "uz-Latn";
    public const string Cyrillic = "uz-Cyrl";
    public const string Russian = "ru";

    public string Lang
    {
        get
        {
            var value = accessor.HttpContext?.Request.Cookies[CookieName];
            return value is Cyrillic or Russian or Latin ? value : Latin;
        }
    }

    public string LangLabel => Lang switch
    {
        Cyrillic => "ЎЗ",
        Russian => "РУ",
        _ => "UZ"
    };

    public string this[string key]
    {
        get
        {
            if (Lang == Latin) return key;
            if (!Translations.Map.TryGetValue(key, out var row)) return key;
            return Lang == Cyrillic ? row.Cyrillic : row.Russian;
        }
    }
}

public record Row(string Cyrillic, string Russian);

/// <summary>
/// Single translation table. Key = Uzbek Latin (also the default rendering),
/// value = Uzbek Cyrillic and Russian.
/// </summary>
public static class Translations
{
    public static readonly Dictionary<string, Row> Map = new(StringComparer.Ordinal)
    {
        // ---------- brand / chrome ----------
        ["Clinical Intelligence Platform"] = new("Clinical Intelligence Platform", "Clinical Intelligence Platform"),
        ["Klinik qarorlarni qo'llab-quvvatlash tizimi"] = new("Клиник қарорларни қўллаб-қувватлаш тизими", "Система поддержки клинических решений"),
        ["KLINIK BOSHQARUV"] = new("КЛИНИК БОШҚАРУВ", "КЛИНИЧЕСКОЕ УПРАВЛЕНИЕ"),
        ["TIZIM"] = new("ТИЗИМ", "СИСТЕМА"),
        ["Dashboard"] = new("Бошқарув панели", "Панель управления"),
        ["Bemorlar"] = new("Беморлар", "Пациенты"),
        ["AI konsilium"] = new("AI консилиум", "AI консилиум"),
        ["Analizlar"] = new("Анализлар", "Анализы"),
        ["Yangi bemor"] = new("Янги бемор", "Новый пациент"),
        ["AI agentlar"] = new("AI агентлар", "AI агенты"),
        ["Tizim ishlamoqda"] = new("Тизим ишламоқда", "Система работает"),
        ["Shifokor"] = new("Шифокор", "Врач"),
        ["Mutaxassis"] = new("Мутахассис", "Специалист"),
        ["Til"] = new("Тил", "Язык"),

        // ---------- dashboard ----------
        ["OPERATSION MARKAZ"] = new("ОПЕРАЦИОН МАРКАЗ", "ОПЕРАЦИОННЫЙ ЦЕНТР"),
        ["Klinik boshqaruv paneli"] = new("Клиник бошқарув панели", "Клиническая панель"),
        ["Bemorlar, klinik tarix va AI konsilium — bitta ish maydonida."] =
            new("Беморлар, клиник тарих ва AI консилиум — битта иш майдонида.",
                "Пациенты, клиническая история и AI консилиум — в одном рабочем пространстве."),
        ["BEMORLAR"] = new("БЕМОРЛАР", "ПАЦИЕНТЫ"),
        ["TASHRIFLAR"] = new("ТАШРИФЛАР", "ВИЗИТЫ"),
        ["KONSILIUMLAR"] = new("КОНСИЛИУМЛАР", "КОНСИЛИУМЫ"),
        ["KRITIK"] = new("КРИТИК", "КРИТИЧЕСКИЕ"),
        ["Ro'yxatdagi jami"] = new("Рўйхатдаги жами", "Всего в реестре"),
        ["Klinik yozuvlar"] = new("Клиник ёзувлар", "Клинические записи"),
        ["O'rtacha risk"] = new("Ўртача риск", "Средний риск"),
        ["Shoshilinch e'tibor"] = new("Шошилинч эътибор", "Срочное внимание"),
        ["So'nggi konsiliumlar"] = new("Сўнгги консилиумлар", "Последние консилиумы"),
        ["So'nggi bemorlar"] = new("Сўнгги беморлар", "Последние пациенты"),
        ["Barchasi"] = new("Барчаси", "Все"),
        ["Agent tarmog'i"] = new("Агент тармоғи", "Сеть агентов"),
        ["Ulangan"] = new("Уланган", "Подключено"),
        ["Lokal rejim"] = new("Локал режим", "Локальный режим"),
        ["Sozlamalar"] = new("Созламалар", "Настройки"),

        // ---------- common table headers ----------
        ["BEMOR"] = new("БЕМОР", "ПАЦИЕНТ"),
        ["ASOSIY GIPOTEZA"] = new("АСОСИЙ ГИПОТЕЗА", "ОСНОВНАЯ ГИПОТЕЗА"),
        ["RISK"] = new("РИСК", "РИСК"),
        ["SANA"] = new("САНА", "ДАТА"),
        ["JINS"] = new("ЖИНС", "ПОЛ"),
        ["YOSH"] = new("ЁШ", "ВОЗРАСТ"),
        ["HOLAT"] = new("ҲОЛАТ", "СТАТУС"),
        ["KONSENSUS"] = new("КОНСЕНСУС", "КОНСЕНСУС"),
        ["TAHLIL SANASI"] = new("ТАҲЛИЛ САНАСИ", "ДАТА АНАЛИЗА"),
        ["SIMPTOMLAR"] = new("СИМПТОМЛАР", "СИМПТОМЫ"),
        ["ALLERGIYA"] = new("АЛЛЕРГИЯ", "АЛЛЕРГИЯ"),
        ["SURUNKALI"] = new("СУРУНКАЛИ", "ХРОНИЧЕСКИЕ"),
        ["FAYL"] = new("ФАЙЛ", "ФАЙЛ"),
        ["KO'RSATKICH"] = new("КЎРСАТКИЧ", "ПОКАЗАТЕЛЬ"),
        ["QIYMAT"] = new("ҚИЙМАТ", "ЗНАЧЕНИЕ"),
        ["REFERENS"] = new("РЕФЕРЕНС", "РЕФЕРЕНС"),
        ["BIRLIK"] = new("БИРЛИК", "ЕДИНИЦА"),
        ["BAYROQ"] = new("БАЙРОҚ", "ФЛАГ"),
        ["MANBA"] = new("МАНБА", "ИСТОЧНИК"),
        ["AGENT"] = new("АГЕНТ", "АГЕНТ"),
        ["RAUND"] = new("РАУНД", "РАУНД"),
        ["XULOSA"] = new("ХУЛОСА", "ЗАКЛЮЧЕНИЕ"),
        ["ISHONCH"] = new("ИШОНЧ", "УВЕРЕННОСТЬ"),
        ["DARAJA"] = new("ДАРАЖА", "УРОВЕНЬ"),

        // ---------- patients ----------
        ["REGISTR"] = new("РЕГИСТР", "РЕЕСТР"),
        ["Bemorlar registri"] = new("Беморлар регистри", "Реестр пациентов"),
        ["Har bir bemorning individual tibbiy tarixi va konsilium natijalari."] =
            new("Ҳар бир беморнинг индивидуал тиббий тарихи ва консилиум натижалари.",
                "Индивидуальная медицинская история и результаты консилиумов по каждому пациенту."),
        ["Ism yoki telefon bo'yicha qidirish"] = new("Исм ёки телефон бўйича қидириш", "Поиск по имени или телефону"),
        ["Qidirish"] = new("Қидириш", "Поиск"),
        ["Karta"] = new("Карта", "Карта"),
        ["Timeline"] = new("Таймлайн", "Таймлайн"),
        ["Bemor kartasi"] = new("Бемор картаси", "Карта пациента"),
        ["Klinik profil"] = new("Клиник профил", "Клинический профиль"),
        ["Oxirgi ko'rsatkichlar"] = new("Охирги кўрсаткичлар", "Последние показатели"),
        ["Oxirgi konsilium"] = new("Охирги консилиум", "Последний консилиум"),
        ["Klinik tashriflar"] = new("Клиник ташрифлар", "Клинические визиты"),
        ["Dorilar"] = new("Дорилар", "Лекарства"),
        ["Konsilium tarixi"] = new("Консилиум тарихи", "История консилиумов"),
        ["Yangi tashrif"] = new("Янги ташриф", "Новый визит"),
        ["Konsilium"] = new("Консилиум", "Консилиум"),
        ["Hisobot"] = new("Ҳисобот", "Отчёт"),
        ["Tug'ilgan sana"] = new("Туғилган сана", "Дата рождения"),
        ["Telefon"] = new("Телефон", "Телефон"),
        ["Ro'yxatga olingan"] = new("Рўйхатга олинган", "Зарегистрирован"),
        ["yosh"] = new("ёш", "лет"),

        // ---------- forms ----------
        ["F.I.SH."] = new("Ф.И.Ш.", "Ф.И.О."),
        ["Shaxsiy ma'lumotlar"] = new("Шахсий маълумотлар", "Личные данные"),
        ["Klinik kontekst"] = new("Клиник контекст", "Клинический контекст"),
        ["Allergiyalar"] = new("Аллергиялар", "Аллергии"),
        ["Surunkali kasalliklar"] = new("Сурункали касалликлар", "Хронические заболевания"),
        ["Bekor qilish"] = new("Бекор қилиш", "Отмена"),
        ["Saqlash"] = new("Сақлаш", "Сохранить"),
        ["Erkak"] = new("Эркак", "Мужской"),
        ["Ayol"] = new("Аёл", "Женский"),
        ["Noma'lum"] = new("Номаълум", "Не указан"),
        ["Tashrif sanasi"] = new("Ташриф санаси", "Дата визита"),
        ["Shikoyat va anamnez"] = new("Шикоят ва анамнез", "Жалобы и анамнез"),
        ["Anamnez"] = new("Анамнез", "Анамнез"),
        ["Vital ko'rsatkichlar"] = new("Витал кўрсаткичлар", "Витальные показатели"),
        ["Qo'shimcha"] = new("Қўшимча", "Дополнительно"),
        ["Triaj"] = new("Триаж", "Триаж"),
        ["Izoh"] = new("Изоҳ", "Комментарий"),
        ["Saqlash va konsiliumni boshlash"] = new("Сақлаш ва консилиумни бошлаш", "Сохранить и запустить консилиум"),
        ["Eski tahlillarni kiritsangiz, o'sha sanani ko'rsating."] =
            new("Эски таҳлилларни киритсангиз, ўша санани кўрсатинг.",
                "Если вводите старые анализы, укажите их дату."),

        // ---------- council ----------
        ["Konsilium arxivi"] = new("Консилиум архиви", "Архив консилиумов"),
        ["Barcha o'tkazilgan sessiyalar va ularning xavf darajasi."] =
            new("Барча ўтказилган сессиялар ва уларнинг хавф даражаси.",
                "Все проведённые сессии и их уровень риска."),
        ["Barcha darajalar"] = new("Барча даражалар", "Все уровни"),
        ["Jonli konsilium"] = new("Жонли консилиум", "Живой консилиум"),
        ["Konsilium arenasi"] = new("Консилиум аренаси", "Арена консилиума"),
        ["Jarayon jurnali"] = new("Жараён журнали", "Журнал процесса"),
        ["Kirish ma'lumotlari"] = new("Кириш маълумотлари", "Входные данные"),
        ["ULANMOQDA"] = new("УЛАНМОҚДА", "ПОДКЛЮЧЕНИЕ"),
        ["Konsilium natijasi"] = new("Консилиум натижаси", "Результат консилиума"),
        ["Asosiy gipoteza"] = new("Асосий гипотеза", "Основная гипотеза"),
        ["Alternativ gipotezalar"] = new("Альтернатив гипотезалар", "Альтернативные гипотезы"),
        ["Tavsiya etilgan tekshiruvlar"] = new("Тавсия этилган текширувлар", "Рекомендованные обследования"),
        ["Qizil bayroqlar"] = new("Қизил байроқлар", "Красные флаги"),
        ["Mustaqil tahlil"] = new("Мустақил таҳлил", "Независимый анализ"),
        ["O'zaro nazorat"] = new("Ўзаро назорат", "Взаимный контроль"),
        ["Tashrif ma'lumotlari"] = new("Ташриф маълумотлари", "Данные визита"),
        ["Yakuniy qaror shifokorga tegishli"] = new("Якуний қарор шифокорга тегишли", "Окончательное решение за врачом"),
        ["Bu hisobot AI qoralamasi. Tashxis va davolash rejasini shifokor tasdiqlaydi."] =
            new("Бу ҳисобот AI қораламаси. Ташхис ва даволаш режасини шифокор тасдиқлайди.",
                "Этот отчёт — черновик AI. Диагноз и план лечения утверждает врач."),
        ["Chop etish"] = new("Чоп этиш", "Печать"),
        ["Klinik hisobot"] = new("Клиник ҳисобот", "Клинический отчёт"),
        ["Agent xulosalari"] = new("Агент хулосалари", "Заключения агентов"),
        ["Konsilium o'tkazilgan"] = new("Консилиум ўтказилган", "Консилиум проведён"),

        // ---------- labs ----------
        ["LABORATORIYA"] = new("ЛАБОРАТОРИЯ", "ЛАБОРАТОРИЯ"),
        ["Faylni yuklash"] = new("Файлни юклаш", "Загрузка файла"),
        ["Faylni shu yerga tashlang"] = new("Файлни шу ерга ташланг", "Перетащите файл сюда"),
        ["yoki bosing va kompyuterdan tanlang"] = new("ёки босинг ва компьютердан танланг", "или нажмите и выберите на компьютере"),
        ["AI hujjatni o'zi o'qib, ko'rsatkichlarni bazaga yozadi."] =
            new("AI ҳужжатни ўзи ўқиб, кўрсаткичларни базага ёзади.",
                "AI сам читает документ и записывает показатели в базу."),
        ["Yuklangan hujjatlar"] = new("Юкланган ҳужжатлар", "Загруженные документы"),
        ["So'nggi ko'rsatkichlar"] = new("Сўнгги кўрсаткичлар", "Последние показатели"),
        ["Ajratilgan ko'rsatkichlar"] = new("Ажратилган кўрсаткичлар", "Извлечённые показатели"),
        ["AJRATILGAN"] = new("АЖРАТИЛГАН", "ИЗВЛЕЧЕНО"),
        ["ME'YORDAN CHETDA"] = new("МЕЪЁРДАН ЧЕТДА", "ВНЕ НОРМЫ"),
        ["TAHLIL VAQTI"] = new("ТАҲЛИЛ ВАҚТИ", "ВРЕМЯ АНАЛИЗА"),
        ["Original fayl"] = new("Оригинал файл", "Оригинал файла"),
        ["Barcha analizlar"] = new("Барча анализлар", "Все анализы"),
        ["Konsilium o'tkazish"] = new("Консилиум ўтказиш", "Провести консилиум"),
        ["Olingan sana"] = new("Олинган сана", "Дата забора"),
        ["Fayl yuklanmoqda"] = new("Файл юкланмоқда", "Файл загружается"),

        // ---------- settings ----------
        ["AI agent sozlamalari"] = new("AI агент созламалари", "Настройки AI агентов"),
        ["Har bir agent uchun alohida provayder, endpoint, kalit va model kiriting."] =
            new("Ҳар бир агент учун алоҳида провайдер, endpoint, калит ва модель киритинг.",
                "Укажите для каждого агента отдельного провайдера, endpoint, ключ и модель."),
        ["Provayder"] = new("Провайдер", "Провайдер"),
        ["Yoqilgan"] = new("Ёқилган", "Включён"),
        ["API kalit"] = new("API калит", "API ключ"),
        ["Model"] = new("Модель", "Модель"),
        ["Kalit saqlangan"] = new("Калит сақланган", "Ключ сохранён"),
        ["Kalit kiritilmagan"] = new("Калит киритилмаган", "Ключ не задан"),
        ["Bo'sh qoldirsangiz eski kalit saqlanadi"] =
            new("Бўш қолдирсангиз эски калит сақланади", "Оставьте пустым — прежний ключ сохранится"),
        ["Sozlamalar saqlandi"] = new("Созламалар сақланди", "Настройки сохранены"),
        ["Barchasini saqlash"] = new("Барчасини сақлаш", "Сохранить всё"),
        ["Ulanishni tekshirish"] = new("Уланишни текшириш", "Проверить подключение"),
        ["Tekshirilmoqda"] = new("Текширилмоқда", "Проверяется"),
        ["Ulanish muvaffaqiyatli"] = new("Уланиш муваффақиятли", "Подключение успешно"),

        // ---------- states ----------
        ["Ma'lumot yo'q"] = new("Маълумот йўқ", "Нет данных"),
        ["Yozuvlar topilmadi"] = new("Ёзувлар топилмади", "Записи не найдены"),
        ["Bemorlar topilmadi"] = new("Беморлар топилмади", "Пациенты не найдены"),
        ["Hujjatlar yo'q"] = new("Ҳужжатлар йўқ", "Документов нет"),
        ["Sessiyalar yo'q"] = new("Сессиялар йўқ", "Сессий нет"),
        ["Tashriflar yo'q"] = new("Ташрифлар йўқ", "Визитов нет"),
        ["ONLINE"] = new("ONLINE", "ONLINE"),
        ["LOKAL"] = new("ЛОКАЛ", "ЛОКАЛЬНО"),
        ["Kritik"] = new("Критик", "Критический"),
        ["Yuqori"] = new("Юқори", "Высокий"),
        ["O'rta"] = new("Ўрта", "Средний"),
        ["Past"] = new("Паст", "Низкий"),
        ["Yashil"] = new("Яшил", "Зелёный"),
        ["Sariq"] = new("Сариқ", "Жёлтый"),
        ["Qizil"] = new("Қизил", "Красный")
    };
}
