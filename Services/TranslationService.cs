using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using VisualNovel.Models;
using VisualNovel.Services;

namespace VisualNovel.Services
{
    public class TranslationService
    {
        private static TranslationService? _instance;
        private Dictionary<string, Dictionary<string, string>> _translations;
        private string _currentLanguage = "English";

        private TranslationService()
        {
            _translations = new Dictionary<string, Dictionary<string, string>>();
            LoadTranslations();
        }

        public static TranslationService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TranslationService();
                }
                return _instance;
            }
        }

        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_translations.ContainsKey(value))
                {
                    _currentLanguage = value;
                }
            }
        }

        public event EventHandler? LanguageChanged;

        public void SetLanguage(string language)
        {
            if (_translations.ContainsKey(language))
            {
                _currentLanguage = language;
                LanguageChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public string GetTranslation(string key)
        {
            if (_translations.ContainsKey(_currentLanguage) && 
                _translations[_currentLanguage].ContainsKey(key))
            {
                return _translations[_currentLanguage][key];
            }
            
            // Fallback to English if translation not found
            if (_translations.ContainsKey("English") && 
                _translations["English"].ContainsKey(key))
            {
                return _translations["English"][key];
            }
            
            return key; // Return key if no translation found
        }

        public List<string> GetAvailableLanguages()
        {
            return new List<string> { "English", "Russian", "Ukrainian", "Polish" };
        }

        private void LoadTranslations()
        {
            // English translations
            _translations["English"] = new Dictionary<string, string>
            {
                // Main Menu
                { "MainMenu_Continue", "Continue" },
                { "MainMenu_NewGame", "New Game" },
                { "MainMenu_LoadGame", "Load Game" },
                { "MainMenu_Options", "Options" },
                { "MainMenu_Credits", "Credits" },
                { "MainMenu_Quit", "Quit" },
                { "MainMenu_GameTitle", "The Winter Sun" },
                { "MainMenu_Subtitle", "Silent Mother, Starving God" },
                
                // Settings Menu
                { "Settings_Title", "Settings" },
                { "Settings_Audio", "Audio" },
                { "Settings_Text", "Text" },
                { "Settings_Display", "Display" },
                { "Settings_Graphics", "Graphics" },
                { "Settings_Gameplay", "Gameplay" },
                { "Settings_Controls", "Controls" },
                { "Settings_Accessibility", "Accessibility" },
                { "Settings_Language", "Language" },
                
                // Audio Settings
                { "Settings_MasterVolume", "Master Volume" },
                { "Settings_MusicVolume", "Music Volume" },
                { "Settings_SoundEffectsVolume", "Sound Effects Volume" },
                { "Settings_MuteMusic", "Mute Music" },
                { "Settings_MuteSoundEffects", "Mute Sound Effects" },
                
                // Text Settings
                { "Settings_TextSpeed", "Text Speed" },
                { "Settings_FontSize", "Font Size" },
                { "Settings_AutoAdvanceText", "Auto Advance Text" },
                { "Settings_AutoAdvanceDelay", "Auto Advance Delay" },
                { "Settings_SkipUnreadText", "Skip Unread Text" },
                { "Settings_SkipReadText", "Skip Read Text" },
                
                // Display Settings
                { "Settings_Fullscreen", "Fullscreen" },
                { "Settings_WindowWidth", "Window Width" },
                { "Settings_WindowHeight", "Window Height" },
                { "Settings_VSync", "VSync" },
                { "Settings_TargetFPS", "Target FPS" },
                { "Settings_ShowFPSCounter", "Show FPS Counter" },
                
                // Graphics Settings
                { "Settings_Optimisation", "Optimisation" },
                { "Settings_GraphicsQuality", "Graphics Quality" },
                { "Settings_EnableVisualEffects", "Enable Visual Effects" },
                { "Settings_EnableShadows", "Enable Shadows" },
                { "Settings_BackgroundOpacity", "Background Opacity" },
                { "Settings_Quality_Low", "Low" },
                { "Settings_Quality_Medium", "Medium" },
                { "Settings_Quality_High", "High" },
                
                // Gameplay Settings
                { "Settings_EnableAutoSave", "Enable Auto-Save" },
                { "Settings_AutoSaveInterval", "Auto-Save Interval" },
                { "Settings_ConfirmOnExit", "Confirm on Exit" },
                { "Settings_ShowSkipIndicator", "Show Skip Indicator" },
                { "Settings_ShowLocationTime", "Show Location & Time" },
                { "Settings_ShowChapterTitle", "Show Chapter Title" },
                
                // Controls Settings
                { "Settings_SkipKey", "Skip Key" },
                { "Settings_SaveKey", "Save Key" },
                { "Settings_LoadKey", "Load Key" },
                { "Settings_MenuKey", "Menu Key" },
                { "Settings_AutoAdvanceToggleKey", "Auto Advance Toggle Key" },
                
                // Accessibility Settings
                { "Settings_HighContrastMode", "High Contrast Mode" },
                { "Settings_LargeTextMode", "Large Text Mode" },
                { "Settings_ReduceAnimations", "Reduce Animations" },
                
                // Buttons
                { "Settings_ResetToDefaults", "Reset to Defaults" },
                { "Settings_Cancel", "Cancel" },
                { "Settings_Apply", "Apply" },
                
                // Language Names
                { "Language_English", "English" },
                { "Language_Russian", "Russian" },
                { "Language_Ukrainian", "Ukrainian" },
                { "Language_Polish", "Polish" },
                
                // Dialog Buttons
                { "Dialog_OK", "OK" },
                { "Dialog_Yes", "Yes" },
                { "Dialog_No", "No" },
                { "Dialog_Cancel", "Cancel" },
                
                // Dialogs
                { "Dialog_QuitGame_Title", "Quit Game" },
                { "Dialog_QuitGame_Message", "Are you sure you want to quit?\n\nYour progress will be automatically saved." },
                { "Dialog_Credits_Title", "Credits" },
                { "Dialog_Credits_Message", "Visual Novel System\n\nCreated with WPF and C#\n\nDeveloped with passion for storytelling\n\n© 2025" },
                { "Dialog_ResetSettings_Title", "Reset Settings" },
                { "Dialog_ResetSettings_Message", "Are you sure you want to reset all settings to their default values?" },
                { "Dialog_UnsavedChanges_Title", "Unsaved Changes" },
                { "Dialog_UnsavedChanges_Message", "You have unsaved changes. Do you want to save them before closing?" },
                { "Dialog_Error_Title", "Error" },
                
                // GameScene
                { "GameScene_Save", "Save" },
                { "GameScene_Load", "Load" },
                { "GameScene_Menu", "Menu" },
                { "GameScene_SkipIndicator", "[ENTER/SPACE/CLICK]" },
                { "GameScene_FPS", "FPS" },
                { "GameScene_UnknownLocation", "UNKNOWN" },
                { "GameScene_Sympathy", "Sympathy" },
                { "GameScene_Obedience", "Obedience" },
                { "GameScene_TheEnd", "THE END" },
                { "GameScene_ThankYou", "Thank you for playing!" },
                { "GameScene_Chapter", "Chapter" },
                { "Chapter1_Title", "Chapter 1: Religious Nobility" },
                { "Chapter1_Subtitle", "Religious Nobility" },
                
                // Scene1 - Winter Sun
                { "Scene1_Name", "Village" },
                { "Scene1_Character_Player", "You" },
                { "Scene1_Character_GirlWithCat", "Ada" },
                { "Scene1_Character_Mother", "Mother" },
                { "Scene1_Character_VillageChief", "Village Chief" },
                { "Scene1_Character_Narrator", "Narrator" },
                
                // Introduction
                { "Scene1_Introduction_WinterSun", "*introduction with a description of the winter sun*" },
                { "Scene1_VillageAppears", "Finally, the village appeared from behind the hills and trees." },
                { "Scene1_JourneyText_1", "Although the journey was long and exhausting, this view does not bring joy." },
                { "Scene1_JourneyText_2", "You know that your appearance there will only cause suspicion. The best you can hope for is that they will look at you imploringly." },
                { "Scene1_ChainsText", "Chains clinked on your shoulders, as if to remind you of themselves. They hold a black furnace on your back. Whatever is burning inside it glows with an unnatural red light." },
                { "Scene1_CurseText", "Your curse, the origin of which you have vague, almost faded memories. Sometimes it seems that you can remember a fragment of the past, but it immediately slips away and dissolves without a trace." },
                { "Scene1_LastSteps", "Just a few more steps with clumps of wet snow stuck to your clothes. One last push, and you can rest a little." },
                { "Scene1_VillageNoise", "You hear the noise of the village—conversations, the sound of hammers, the high-pitched cries of children somewhere in the distance." },
                { "Scene1_MainStreet", "The background changes to the main street of the village." },
                
                // Girl with cat
                { "Scene1_GirlWithCat_1", "..." },
                { "Scene1_GirlWithCat_2", "Hello... Could you heal me?" },
                { "Scene1_GirlWithCat_3_1", "This is Matilda! We found her on the street, near an abandoned hut! She was so tiny! About this big..." },
                { "Scene1_GirlWithCat_3_2", "Oh... Anyway, she was small... But we fed her and she grew big... We go for walks together... I love to pet her and carry her everywhere..." },
                { "Scene1_GirlWithCat_4", "Yes, I love my mom very, very much... She said we should come see you..." },
                { "Scene1_GirlWithCat_Response_Cat_1", "The girl perks up and even looks at you." },
                { "Scene1_GirlWithCat_Response_Cat_2", "The girl wanted to show the size of little Matilda with her hands, but the cat, which seems to be very tame, began to roll away." },
                { "Scene1_GirlWithCat_AfterBurn", "The girl clearly perked up, laughing, petting, and hugging the cat." },
                
                // Mother
                { "Scene1_Mother_1", "Come on, honey, not now." },
                { "Scene1_Mother_2", "Hello..." },
                { "Scene1_Mother_3", "Is it over already?" },
                { "Scene1_Mother_4_1", "Yes..." },
                { "Scene1_Mother_4_2", "It's easier with children, right? They don't have very important memories yet." },
                { "Scene1_Mother_Context", "Out of the corner of your eye, you noticed the mother looking wary." },
                
                // Village Chief
                { "Scene1_VillageChief_1_1", "Good afternoon. I am the village chief. I'll say this right away: we have several people who could use your help. If you could call it \"help\". Otherwise, don't stay long. Your folk scare the locals." },
                { "Scene1_VillageChief_1_2", "If you need warmth or food, we will help as much as we can, just do what you need to do and leave as soon as possible." },
                { "Scene1_VillageChief_2", "..." },
                { "Scene1_VillageChief_Response_WhoNeedsHelp", "Everyone who wasn't afraid to be on the same street as you. Or almost everyone. Some are just onlookers." },
                { "Scene1_VillageChief_Response_LastBurner", "About a year ago. As you may have noticed, ours is the only village for many kilometers around." },
                { "Scene1_VillageChief_Response_RestockSupplies", "You can ask the locals for food. If they give it to you. There's also the doctor's shop... What's his name... Frankie. I think he'll sell you anything you want to buy. Although I don't think your kind has much money." },
                
                // Player
                { "Scene1_Player_1", "Good afternoon." },
                { "Scene1_Player_1_2", "Don't worry. I'll just talk to her. It's usually easier with children." },
                { "Scene1_Player_2", "What kind of cat is that?" },
                { "Scene1_Player_3", "Do you love your mom too?" },
                { "Scene1_Player_4", "Yes, it's usually easier with children..." },
                { "Scene1_Player_Context_1_1", "The woman hesitates. She doesn't know how to approach you." },
                { "Scene1_Player_Context_1_3", "The girl is staring at the floor. She's not even looking at you now." },
                
                // Context text
                { "Scene1_GirlApproaches", "You see a girl approaching you with her eyes downcast. She is carrying a cat in her arms. Behind her walks what appears to be her mother, with a stern but determined look on her face." },
                { "Scene1_GirlWithCat_Context", "Time and again, you see this strange scene. There is a doctor in the settlements. But people come to you for help, asking you to \"heal\" them. All these people look healthy. The only thing they have in common is sadness and fatigue in their eyes. The girl clearly looked distant and tired. You have seen this many times before." },
                
                // Burn Memory
                { "Scene1_BurnMemory_Prompt", "..." },
                { "Scene1_Choice_BurnMemory", "Burn Memory" },
                { "Scene1_BurnMemory_Effect_1", "For a few seconds, the girl's gaze becomes unfocused. She looks through you, completely lost." },
                { "Scene1_BurnMemory_Effect_2", "As if waking up, she begins to move. Her hands discover the cat sitting on her chest." },
                { "Scene1_BurnMemory_Effect_3", "The girl stretches out her arms with the cat in front of her and begins to study it with her eyes. She is seeing it for the first time. Again. She smiles and turns around, looking for her mother." },
                
                // Choices
                { "Scene1_Choice_WhoNeedsHelp", "Who needs my help?" },
                { "Scene1_Choice_LastBurner", "When was the last time you had a burner?" },
                { "Scene1_Choice_RestockSupplies", "Where can I restock my supplies?" },
                { "Scene1_Choice_ExitDialogue", "Exit dialogue" },
                
                // End
                { "Scene1_End_Hub", "The player enters the hub — the village street." }
            };

            // Russian translations
            _translations["Russian"] = new Dictionary<string, string>
            {
                // Main Menu
                { "MainMenu_Continue", "Продолжить" },
                { "MainMenu_NewGame", "Новая игра" },
                { "MainMenu_LoadGame", "Загрузить игру" },
                { "MainMenu_Options", "Настройки" },
                { "MainMenu_Credits", "Авторы" },
                { "MainMenu_Quit", "Выход" },
                { "MainMenu_GameTitle", "Зимнее Солнце" },
                { "MainMenu_Subtitle", "Молчаливая Мать, Голодный Бог" },
                
                // Settings Menu
                { "Settings_Title", "Настройки" },
                { "Settings_Audio", "Аудио" },
                { "Settings_Text", "Текст" },
                { "Settings_Display", "Отображение" },
                { "Settings_Graphics", "Графика" },
                { "Settings_Optimisation", "Оптимизация" },
                { "Settings_Gameplay", "Игровой процесс" },
                { "Settings_Controls", "Управление" },
                { "Settings_Accessibility", "Доступность" },
                { "Settings_Language", "Язык" },
                
                // Audio Settings
                { "Settings_MasterVolume", "Общая громкость" },
                { "Settings_MusicVolume", "Громкость музыки" },
                { "Settings_SoundEffectsVolume", "Громкость звуковых эффектов" },
                { "Settings_MuteMusic", "Отключить музыку" },
                { "Settings_MuteSoundEffects", "Отключить звуковые эффекты" },
                
                // Text Settings
                { "Settings_TextSpeed", "Скорость текста" },
                { "Settings_FontSize", "Размер шрифта" },
                { "Settings_AutoAdvanceText", "Автопродолжение текста" },
                { "Settings_AutoAdvanceDelay", "Задержка автопродолжения" },
                { "Settings_SkipUnreadText", "Пропускать непрочитанный текст" },
                { "Settings_SkipReadText", "Пропускать прочитанный текст" },
                
                // Display Settings
                { "Settings_Fullscreen", "Полноэкранный режим" },
                { "Settings_WindowWidth", "Ширина окна" },
                { "Settings_WindowHeight", "Высота окна" },
                { "Settings_VSync", "Вертикальная синхронизация" },
                { "Settings_TargetFPS", "Целевой FPS" },
                { "Settings_ShowFPSCounter", "Показывать счётчик FPS" },
                
                // Graphics Settings
                { "Settings_GraphicsQuality", "Качество графики" },
                { "Settings_EnableVisualEffects", "Включить визуальные эффекты" },
                { "Settings_EnableShadows", "Включить тени" },
                { "Settings_BackgroundOpacity", "Прозрачность фона" },
                { "Settings_Quality_Low", "Низкое" },
                { "Settings_Quality_Medium", "Среднее" },
                { "Settings_Quality_High", "Высокое" },
                
                // Gameplay Settings
                { "Settings_EnableAutoSave", "Включить автосохранение" },
                { "Settings_AutoSaveInterval", "Интервал автосохранения" },
                { "Settings_ConfirmOnExit", "Подтверждать при выходе" },
                { "Settings_ShowSkipIndicator", "Показывать индикатор пропуска" },
                { "Settings_ShowLocationTime", "Показывать место и время" },
                { "Settings_ShowChapterTitle", "Показывать название главы" },
                
                // Controls Settings
                { "Settings_SkipKey", "Клавиша пропуска" },
                { "Settings_SaveKey", "Клавиша сохранения" },
                { "Settings_LoadKey", "Клавиша загрузки" },
                { "Settings_MenuKey", "Клавиша меню" },
                { "Settings_AutoAdvanceToggleKey", "Клавиша переключения автопродолжения" },
                
                // Accessibility Settings
                { "Settings_HighContrastMode", "Режим высокой контрастности" },
                { "Settings_LargeTextMode", "Режим крупного текста" },
                { "Settings_ReduceAnimations", "Уменьшить анимации" },
                
                // Buttons
                { "Settings_ResetToDefaults", "Сбросить по умолчанию" },
                { "Settings_Cancel", "Отмена" },
                { "Settings_Apply", "Применить" },
                
                // Language Names
                { "Language_English", "Английский" },
                { "Language_Russian", "Русский" },
                { "Language_Ukrainian", "Украинский" },
                { "Language_Polish", "Польский" },
                
                // Dialog Buttons
                { "Dialog_OK", "ОК" },
                { "Dialog_Yes", "Да" },
                { "Dialog_No", "Нет" },
                { "Dialog_Cancel", "Отмена" },
                
                // Dialogs
                { "Dialog_QuitGame_Title", "Выход из игры" },
                { "Dialog_QuitGame_Message", "Вы уверены, что хотите выйти?\n\nВаш прогресс будет автоматически сохранён." },
                { "Dialog_Credits_Title", "Авторы" },
                { "Dialog_Credits_Message", "Система визуальных новелл\n\nСоздано с использованием WPF и C#\n\nРазработано с любовью к повествованию\n\n© 2025" },
                { "Dialog_ResetSettings_Title", "Сброс настроек" },
                { "Dialog_ResetSettings_Message", "Вы уверены, что хотите сбросить все настройки к значениям по умолчанию?" },
                { "Dialog_UnsavedChanges_Title", "Несохранённые изменения" },
                { "Dialog_UnsavedChanges_Message", "У вас есть несохранённые изменения. Хотите сохранить их перед закрытием?" },
                { "Dialog_Error_Title", "Ошибка" },
                
                // GameScene
                { "GameScene_Save", "Сохранить" },
                { "GameScene_Load", "Загрузить" },
                { "GameScene_Menu", "Меню" },
                { "GameScene_SkipIndicator", "[ENTER/ПРОБЕЛ/КЛИК]" },
                { "GameScene_FPS", "FPS" },
                { "GameScene_UnknownLocation", "НЕИЗВЕСТНО" },
                { "GameScene_Sympathy", "Симпатия" },
                { "GameScene_Obedience", "Послушание" },
                { "GameScene_TheEnd", "КОНЕЦ" },
                { "GameScene_ThankYou", "Спасибо за игру!" },
                { "GameScene_Chapter", "Глава" },
                { "Chapter1_Title", "Глава 1: Религиозное дворянство" },
                { "Chapter1_Subtitle", "Религиозное дворянство" },
                
                // Scene1 - Winter Sun
                { "Scene1_Name", "Деревня" },
                { "Scene1_Character_Player", "Вы" },
                { "Scene1_Character_GirlWithCat", "Ада" },
                { "Scene1_Character_Mother", "Мать" },
                { "Scene1_Character_VillageChief", "Староста деревни" },
                { "Scene1_Character_Narrator", "Рассказчик" },
                
                // Introduction
                { "Scene1_Introduction_WinterSun", "Слабое зимнее солнце цепляется за небо — бледное, далёкое, больше света, чем тепла." },
                { "Scene1_SnowUnderfoot", "Снег скрипит под сапогами, а потом превращается в мокрую слякоть там, где дорога истоптана." },
                { "Scene1_VillageAppears", "Наконец, деревня появляется между холмами и тёмной полосой деревьев." },
                { "Scene1_JourneyText_1", "Путь был долгим и изнурительным, но вид впереди не приносит радости." },
                { "Scene1_JourneyText_2", "Вы знаете, что означает ваше появление здесь: сначала подозрение. Если повезёт — умоляющий взгляд, молчаливая просьба забрать что-то." },
                { "Scene1_ChainsText", "Цепи звенят на ваших плечах, тянут с каждым шагом — тяжёлые, привычные, неизбежные." },
                { "Scene1_FurnaceGlow", "Они держат чёрную печь у вас за спиной. То, что горит внутри, светится неестественным красно-угольным светом." },
                { "Scene1_CurseText", "Ваше проклятие — его происхождение превратилось в смутные, полустёртые впечатления. Иногда фрагмент прошлого всплывает на поверхность... и ускользает прежде, чем вы успеваете его удержать." },
                { "Scene1_LastSteps", "Мокрый снег налипает на одежду. Ещё несколько шагов. Последний рывок — и вы сможете отдохнуть, хотя бы немного." },
                { "Scene1_VillageNoise", "Вы слышите деревню — приглушённые разговоры, звон молотка, детский смех где-то вдалеке." },
                { "Scene1_MainStreet", "Фон меняется на главную улицу деревни: грязные колеи, низкие крыши и дым из труб, ползущий в холодный воздух." },
                { "Scene1_VillagersNotice", "Головы поворачиваются, когда вы входите. Слова обрываются на полуслове, словно вся улица затаила дыхание." },
                { "Scene1_VillagersMakeWay", "Люди расступаются и оставляют вам узкий проход — как вода, обтекающая камень." },
                
                // Girl with cat
                { "Scene1_GirlWithCat_1", "..." },
                { "Scene1_GirlWithCat_2", "Здравствуйте... Не могли бы вы исцелить меня?" },
                { "Scene1_GirlWithCat_3_1", "Это Матильда! Мы нашли её на улице, возле заброшенной хижины! Она была такой крошечной! Примерно такого размера..." },
                { "Scene1_GirlWithCat_3_2", "О... В любом случае, она была маленькой... Но мы кормили её, и она выросла большой... Мы гуляем вместе... Я люблю гладить её и везде носить..." },
                { "Scene1_GirlWithCat_4", "Да, я очень, очень люблю свою маму... Она сказала, что мы должны прийти к вам..." },
                { "Scene1_GirlWithCat_Response_Cat_1", "Девочка оживляется и даже смотрит на вас." },
                { "Scene1_GirlWithCat_Response_Cat_2", "Девочка хотела показать размер маленькой Матильды руками, но кошка, которая кажется очень ручной, начала укатываться." },
                { "Scene1_GirlWithCat_AfterBurn", "Девочка явно оживилась, смеясь, гладя и обнимая кошку." },
                
                // Mother
                { "Scene1_Mother_1", "Давай, дорогая, не сейчас." },
                { "Scene1_Mother_2", "Здравствуйте..." },
                { "Scene1_Mother_3", "Уже всё?" },
                { "Scene1_Mother_4_1", "Да..." },
                { "Scene1_Mother_4_2", "С детьми легче, правда? У них ещё нет очень важных воспоминаний." },
                { "Scene1_Mother_Context", "Краем глаза вы заметили, что мать смотрит настороженно." },
                
                // Village Chief
                { "Scene1_VillageChief_1_1", "Добрый день. Я староста деревни. Скажу сразу: у нас есть несколько человек, которым может понадобиться ваша помощь. Если это можно назвать \"помощью\". В противном случае, не задерживайтесь. Ваши люди пугают местных." },
                { "Scene1_VillageChief_1_2", "Если вам нужны тепло или еда, мы поможем, насколько сможем, просто сделайте то, что вам нужно, и уходите как можно скорее." },
                { "Scene1_VillageChief_2", "..." },
                { "Scene1_VillageChief_Response_WhoNeedsHelp", "Все, кто не боялся быть на одной улице с вами. Или почти все. Некоторые просто зрители." },
                { "Scene1_VillageChief_Response_LastBurner", "Примерно год назад. Как вы могли заметить, наша деревня — единственная на многие километры вокруг." },
                { "Scene1_VillageChief_Response_RestockSupplies", "Вы можете попросить у местных еду. Если они дадут. Есть ещё лавка доктора... Как его зовут... Фрэнки. Думаю, он продаст вам всё, что вы захотите купить. Хотя я не думаю, что у ваших много денег." },
                
                // Player
                { "Scene1_Player_1", "Добрый день." },
                { "Scene1_Player_1_2", "Не волнуйтесь. Я просто поговорю с ней. Обычно с детьми легче." },
                { "Scene1_Player_2", "Что это за кошка?" },
                { "Scene1_Player_3", "Ты тоже любишь свою маму?" },
                { "Scene1_Player_4", "Да, обычно с детьми легче..." },
                { "Scene1_Player_Context_1_1", "Женщина колеблется. Она не знает, как к вам подойти." },
                { "Scene1_Player_Context_1_3", "Девочка смотрит в пол. Она даже не смотрит на вас сейчас." },
                
                // Context text
                { "Scene1_GirlApproaches", "Вы видите девочку, приближающуюся к вам с опущенными глазами. Она несёт кошку на руках. За ней идёт то, что кажется её матерью, с суровым, но решительным выражением лица." },
                { "Scene1_GirlWithCat_Context", "Снова и снова вы видите эту странную сцену. В поселениях есть врач. Но люди приходят к вам за помощью, прося вас \"исцелить\" их. Все эти люди выглядят здоровыми. Единственное, что их объединяет, — это печаль и усталость в глазах. Девочка явно выглядела отстранённой и уставшей. Вы видели это много раз раньше." },
                
                // Burn Memory
                { "Scene1_BurnMemory_Prompt", "..." },
                { "Scene1_Choice_BurnMemory", "Сжечь память" },
                { "Scene1_BurnMemory_Effect_1", "На несколько секунд взгляд девочки становится расфокусированным. Она смотрит сквозь вас, совершенно потерянная." },
                { "Scene1_BurnMemory_Effect_2", "Как будто просыпаясь, она начинает двигаться. Её руки обнаруживают кошку, сидящую у неё на груди." },
                { "Scene1_BurnMemory_Effect_3", "Девочка протягивает руки с кошкой перед собой и начинает изучать её глазами. Она видит её впервые. Снова. Она улыбается и оборачивается, ища мать." },
                
                // Choices
                { "Scene1_Choice_WhoNeedsHelp", "Кому нужна моя помощь?" },
                { "Scene1_Choice_LastBurner", "Когда у вас в последний раз был сжигатель?" },
                { "Scene1_Choice_RestockSupplies", "Где я могу пополнить запасы?" },
                { "Scene1_Choice_ExitDialogue", "Выйти из диалога" },
                
                // End
                { "Scene1_End_Hub", "Игрок входит в хаб — улицу деревни." }
            };

            // Ukrainian translations
            _translations["Ukrainian"] = new Dictionary<string, string>
            {
                // Main Menu
                { "MainMenu_Continue", "Продовжити" },
                { "MainMenu_NewGame", "Нова гра" },
                { "MainMenu_LoadGame", "Завантажити гру" },
                { "MainMenu_Options", "Налаштування" },
                { "MainMenu_Credits", "Автори" },
                { "MainMenu_Quit", "Вихід" },
                { "MainMenu_GameTitle", "The Winter Sun" },
                { "MainMenu_Subtitle", "Мовчазна Матір, Голодний Бог" },
                
                // Settings Menu
                { "Settings_Title", "Налаштування" },
                { "Settings_Audio", "Аудіо" },
                { "Settings_Text", "Текст" },
                { "Settings_Display", "Відображення" },
                { "Settings_Graphics", "Графіка" },
                { "Settings_Optimisation", "Оптимізація" },
                { "Settings_Gameplay", "Ігровий процес" },
                { "Settings_Controls", "Керування" },
                { "Settings_Accessibility", "Доступність" },
                { "Settings_Language", "Мова" },
                
                // Audio Settings
                { "Settings_MasterVolume", "Загальна гучність" },
                { "Settings_MusicVolume", "Гучність музики" },
                { "Settings_SoundEffectsVolume", "Гучність звукових ефектів" },
                { "Settings_MuteMusic", "Вимкнути музику" },
                { "Settings_MuteSoundEffects", "Вимкнути звукові ефекти" },
                
                // Text Settings
                { "Settings_TextSpeed", "Швидкість тексту" },
                { "Settings_FontSize", "Розмір шрифту" },
                { "Settings_AutoAdvanceText", "Автопродовження тексту" },
                { "Settings_AutoAdvanceDelay", "Затримка автопродовження" },
                { "Settings_SkipUnreadText", "Пропускати непрочитаний текст" },
                { "Settings_SkipReadText", "Пропускати прочитаний текст" },
                
                // Display Settings
                { "Settings_Fullscreen", "Повноекранний режим" },
                { "Settings_WindowWidth", "Ширина вікна" },
                { "Settings_WindowHeight", "Висота вікна" },
                { "Settings_VSync", "Вертикальна синхронізація" },
                { "Settings_TargetFPS", "Цільовий FPS" },
                { "Settings_ShowFPSCounter", "Показувати лічильник FPS" },
                
                // Graphics Settings
                { "Settings_GraphicsQuality", "Якість графіки" },
                { "Settings_EnableVisualEffects", "Увімкнути візуальні ефекти" },
                { "Settings_EnableShadows", "Увімкнути тіні" },
                { "Settings_BackgroundOpacity", "Прозорість фону" },
                { "Settings_Quality_Low", "Низька" },
                { "Settings_Quality_Medium", "Середня" },
                { "Settings_Quality_High", "Висока" },
                
                // Gameplay Settings
                { "Settings_EnableAutoSave", "Увімкнути автозбереження" },
                { "Settings_AutoSaveInterval", "Інтервал автозбереження" },
                { "Settings_ConfirmOnExit", "Підтверджувати при виході" },
                { "Settings_ShowSkipIndicator", "Показувати індикатор пропуску" },
                { "Settings_ShowLocationTime", "Показувати місце та час" },
                { "Settings_ShowChapterTitle", "Показувати назву глави" },
                
                // Controls Settings
                { "Settings_SkipKey", "Клавіша пропуску" },
                { "Settings_SaveKey", "Клавіша збереження" },
                { "Settings_LoadKey", "Клавіша завантаження" },
                { "Settings_MenuKey", "Клавіша меню" },
                { "Settings_AutoAdvanceToggleKey", "Клавіша перемикання автопродовження" },
                
                // Accessibility Settings
                { "Settings_HighContrastMode", "Режим високого контрасту" },
                { "Settings_LargeTextMode", "Режим великого тексту" },
                { "Settings_ReduceAnimations", "Зменшити анімації" },
                
                // Buttons
                { "Settings_ResetToDefaults", "Скинути за замовчуванням" },
                { "Settings_Cancel", "Скасувати" },
                { "Settings_Apply", "Застосувати" },
                
                // Language Names
                { "Language_English", "Англійська" },
                { "Language_Russian", "Російська" },
                { "Language_Ukrainian", "Українська" },
                { "Language_Polish", "Польська" },
                
                // Dialog Buttons
                { "Dialog_OK", "ОК" },
                { "Dialog_Yes", "Так" },
                { "Dialog_No", "Ні" },
                { "Dialog_Cancel", "Скасувати" },
                
                // Dialogs
                { "Dialog_QuitGame_Title", "Вихід з гри" },
                { "Dialog_QuitGame_Message", "Ви впевнені, що хочете вийти?\n\nВаш прогрес буде автоматично збережено." },
                { "Dialog_Credits_Title", "Автори" },
                { "Dialog_Credits_Message", "Система візуальних новел\n\nСтворено з використанням WPF та C#\n\nРозроблено з любов'ю до оповідання\n\n© 2025" },
                { "Dialog_ResetSettings_Title", "Скидання налаштувань" },
                { "Dialog_ResetSettings_Message", "Ви впевнені, що хочете скинути всі налаштування до значень за замовчуванням?" },
                { "Dialog_UnsavedChanges_Title", "Незбережені зміни" },
                { "Dialog_UnsavedChanges_Message", "У вас є незбережені зміни. Хочете зберегти їх перед закриттям?" },
                { "Dialog_Error_Title", "Помилка" },
                
                // GameScene
                { "GameScene_Save", "Зберегти" },
                { "GameScene_Load", "Завантажити" },
                { "GameScene_Menu", "Меню" },
                { "GameScene_SkipIndicator", "[ENTER/ПРОБІЛ/КЛІК]" },
                { "GameScene_FPS", "FPS" },
                { "GameScene_UnknownLocation", "НЕВІДОМО" },
                { "GameScene_Sympathy", "Симпатія" },
                { "GameScene_Obedience", "Послух" },
                { "GameScene_TheEnd", "КІНЕЦЬ" },
                { "GameScene_ThankYou", "Дякуємо за гру!" },
                { "GameScene_Chapter", "Глава" },
                { "Chapter1_Title", "Глава 1: Релігійна знать" },
                { "Chapter1_Subtitle", "Релігійна знать" },
                
                // Scene1 - Winter Sun
                { "Scene1_Name", "Село" },
                { "Scene1_Character_Player", "Ви" },
                { "Scene1_Character_GirlWithCat", "Ада" },
                { "Scene1_Character_Mother", "Мати" },
                { "Scene1_Character_VillageChief", "Сільський староста" },
                { "Scene1_Character_Narrator", "Оповідач" },
                
                // Introduction
                { "Scene1_Introduction_WinterSun", "*вступ з описом зимового сонця*" },
                { "Scene1_VillageAppears", "Нарешті, село з'явилося з-за пагорбів і дерев." },
                { "Scene1_JourneyText_1", "Хоча подорож була довгою і виснажливою, цей вид не приносить радості." },
                { "Scene1_JourneyText_2", "Ви знаєте, що ваша поява там викличе лише підозри. Найкраще, на що ви можете сподіватися, це те, що на вас будуть дивитися благально." },
                { "Scene1_ChainsText", "Ланцюги дзвеніли на ваших плечах, ніби нагадуючи про себе. Вони тримають чорну піч на вашій спині. Те, що горить всередині неї, світиться неприродним червоним світлом." },
                { "Scene1_CurseText", "Ваше прокляття, походження якого ви пам'ятаєте нечітко, майже стерті спогади. Іноді здається, що ви можете згадати фрагмент минулого, але він одразу вислизає і розчиняється без сліду." },
                { "Scene1_LastSteps", "Ще кілька кроків з грудами мокрого снігу, що прилипли до вашого одягу. Останній порив, і ви зможете трохи відпочити." },
                { "Scene1_VillageNoise", "Ви чуєте шум села—розмови, звук молотків, пронизливі крики дітей десь вдалині." },
                { "Scene1_MainStreet", "Фон змінюється на головну вулицю села." },
                
                // Girl with cat
                { "Scene1_GirlWithCat_1", "..." },
                { "Scene1_GirlWithCat_2", "Вітаю... Чи могли б ви зцілити мене?" },
                { "Scene1_GirlWithCat_3_1", "Це Матильда! Ми знайшли її на вулиці, біля покинутої хатини! Вона була такою крихітною! Приблизно такого розміру..." },
                { "Scene1_GirlWithCat_3_2", "О... У будь-якому разі, вона була маленькою... Але ми годували її, і вона виросла великою... Ми гуляємо разом... Я люблю гладити її і всюди носити..." },
                { "Scene1_GirlWithCat_4", "Так, я дуже, дуже люблю свою маму... Вона сказала, що ми повинні прийти до вас..." },
                { "Scene1_GirlWithCat_Response_Cat_1", "Дівчинка оживає і навіть дивиться на вас." },
                { "Scene1_GirlWithCat_Response_Cat_2", "Дівчинка хотіла показати розмір маленької Матильди руками, але кішка, яка здається дуже ручною, почала відкочуватися." },
                { "Scene1_GirlWithCat_AfterBurn", "Дівчинка явно оживилася, сміючись, гладячи і обіймаючи кішку." },
                
                // Mother
                { "Scene1_Mother_1", "Давай, дорога, не зараз." },
                { "Scene1_Mother_2", "Вітаю..." },
                { "Scene1_Mother_3", "Вже все?" },
                { "Scene1_Mother_4_1", "Так..." },
                { "Scene1_Mother_4_2", "З дітьми легше, правда? У них ще немає дуже важливих спогадів." },
                { "Scene1_Mother_Context", "Краєм ока ви помітили, що мати дивиться насторожено." },
                
                // Village Chief
                { "Scene1_VillageChief_1_1", "Добрий день. Я сільський староста. Скажу одразу: у нас є кілька людей, яким може знадобитися ваша допомога. Якщо це можна назвати \"допомогою\". В іншому випадку, не затримуйтеся. Ваші люди лякають місцевих." },
                { "Scene1_VillageChief_1_2", "Якщо вам потрібні тепло або їжа, ми допоможемо, наскільки зможемо, просто зробіть те, що вам потрібно, і йдіть якнайшвидше." },
                { "Scene1_VillageChief_2", "..." },
                { "Scene1_VillageChief_Response_WhoNeedsHelp", "Всі, хто не боявся бути на одній вулиці з вами. Або майже всі. Деякі просто глядачі." },
                { "Scene1_VillageChief_Response_LastBurner", "Приблизно рік тому. Як ви могли помітити, наше село — єдине на багато кілометрів навколо." },
                { "Scene1_VillageChief_Response_RestockSupplies", "Ви можете попросити у місцевих їжу. Якщо вони дадуть. Є ще крамниця лікаря... Як його звуть... Френкі. Думаю, він продасть вам все, що ви захочете купити. Хоча я не думаю, що у ваших багато грошей." },
                
                // Player
                { "Scene1_Player_1", "Добрий день." },
                { "Scene1_Player_1_2", "Не хвилюйтеся. Я просто поговорю з нею. Зазвичай з дітьми легше." },
                { "Scene1_Player_2", "Що це за кішка?" },
                { "Scene1_Player_3", "Ти теж любиш свою маму?" },
                { "Scene1_Player_4", "Так, зазвичай з дітьми легше..." },
                { "Scene1_Player_Context_1_1", "Жінка вагається. Вона не знає, як до вас підійти." },
                { "Scene1_Player_Context_1_3", "Дівчинка дивиться в підлогу. Вона навіть не дивиться на вас зараз." },
                
                // Context text
                { "Scene1_GirlApproaches", "Ви бачите дівчинку, що наближається до вас з опущеними очима. Вона несе кішку на руках. За нею йде те, що здається її матір'ю, з суворим, але рішучим виразом обличчя." },
                { "Scene1_GirlWithCat_Context", "Знову і знову ви бачите цю дивну сцену. В поселеннях є лікар. Але люди приходять до вас по допомогу, просячи вас \"зцілити\" їх. Всі ці люди виглядають здоровими. Єдине, що їх об'єднує, — це сум і втома в очах. Дівчинка явно виглядала відстороненою і втомленою. Ви бачили це багато разів раніше." },
                
                // Burn Memory
                { "Scene1_BurnMemory_Prompt", "..." },
                { "Scene1_Choice_BurnMemory", "Спалити пам'ять" },
                { "Scene1_BurnMemory_Effect_1", "На кілька секунд погляд дівчинки стає розфокусованим. Вона дивиться крізь вас, зовсім втрачена." },
                { "Scene1_BurnMemory_Effect_2", "Ніби прокидаючись, вона починає рухатися. Її руки виявляють кішку, що сидить у неї на грудях." },
                { "Scene1_BurnMemory_Effect_3", "Дівчинка простягає руки з кішкою перед собою і починає вивчати її очима. Вона бачить її вперше. Знову. Вона посміхається і обертається, шукаючи матір." },
                
                // Choices
                { "Scene1_Choice_WhoNeedsHelp", "Кому потрібна моя допомога?" },
                { "Scene1_Choice_LastBurner", "Коли у вас востаннє був спалювач?" },
                { "Scene1_Choice_RestockSupplies", "Де я можу поповнити запаси?" },
                { "Scene1_Choice_ExitDialogue", "Вийти з діалогу" },
                
                // End
                { "Scene1_End_Hub", "Гравець входить у хаб — вулицю села." }
            };

            // Polish translations
            _translations["Polish"] = new Dictionary<string, string>
            {
                // Main Menu
                { "MainMenu_Continue", "Kontynuuj" },
                { "MainMenu_NewGame", "Nowa gra" },
                { "MainMenu_LoadGame", "Wczytaj grę" },
                { "MainMenu_Options", "Opcje" },
                { "MainMenu_Credits", "Autorzy" },
                { "MainMenu_Quit", "Wyjdź" },
                { "MainMenu_GameTitle", "The Winter Sun" },
                { "MainMenu_Subtitle", "Milcząca Matka, Głodny Bóg" },
                
                // Settings Menu
                { "Settings_Title", "Ustawienia" },
                { "Settings_Audio", "Dźwięk" },
                { "Settings_Text", "Tekst" },
                { "Settings_Display", "Wyświetlanie" },
                { "Settings_Graphics", "Grafika" },
                { "Settings_Optimisation", "Optymalizacja" },
                { "Settings_Gameplay", "Rozgrywka" },
                { "Settings_Controls", "Sterowanie" },
                { "Settings_Accessibility", "Dostępność" },
                { "Settings_Language", "Język" },
                
                // Audio Settings
                { "Settings_MasterVolume", "Głośność główna" },
                { "Settings_MusicVolume", "Głośność muzyki" },
                { "Settings_SoundEffectsVolume", "Głośność efektów dźwiękowych" },
                { "Settings_MuteMusic", "Wycisz muzykę" },
                { "Settings_MuteSoundEffects", "Wycisz efekty dźwiękowe" },
                
                // Text Settings
                { "Settings_TextSpeed", "Prędkość tekstu" },
                { "Settings_FontSize", "Rozmiar czcionki" },
                { "Settings_AutoAdvanceText", "Automatyczne przewijanie tekstu" },
                { "Settings_AutoAdvanceDelay", "Opóźnienie automatycznego przewijania" },
                { "Settings_SkipUnreadText", "Pomijaj nieprzeczytany tekst" },
                { "Settings_SkipReadText", "Pomijaj przeczytany tekst" },
                
                // Display Settings
                { "Settings_Fullscreen", "Pełny ekran" },
                { "Settings_WindowWidth", "Szerokość okna" },
                { "Settings_WindowHeight", "Wysokość okna" },
                { "Settings_VSync", "Synchronizacja pionowa" },
                { "Settings_TargetFPS", "Docelowy FPS" },
                { "Settings_ShowFPSCounter", "Pokaż licznik FPS" },
                
                // Graphics Settings
                { "Settings_GraphicsQuality", "Jakość grafiki" },
                { "Settings_EnableVisualEffects", "Włącz efekty wizualne" },
                { "Settings_EnableShadows", "Włącz cienie" },
                { "Settings_BackgroundOpacity", "Przezroczystość tła" },
                { "Settings_Quality_Low", "Niska" },
                { "Settings_Quality_Medium", "Średnia" },
                { "Settings_Quality_High", "Wysoka" },
                
                // Gameplay Settings
                { "Settings_EnableAutoSave", "Włącz automatyczne zapisywanie" },
                { "Settings_AutoSaveInterval", "Interwał automatycznego zapisywania" },
                { "Settings_ConfirmOnExit", "Potwierdź przy wyjściu" },
                { "Settings_ShowSkipIndicator", "Pokaż wskaźnik pomijania" },
                { "Settings_ShowLocationTime", "Pokaż lokalizację i czas" },
                { "Settings_ShowChapterTitle", "Pokaż tytuł rozdziału" },
                
                // Controls Settings
                { "Settings_SkipKey", "Klawisz pomijania" },
                { "Settings_SaveKey", "Klawisz zapisu" },
                { "Settings_LoadKey", "Klawisz wczytania" },
                { "Settings_MenuKey", "Klawisz menu" },
                { "Settings_AutoAdvanceToggleKey", "Klawisz przełączania automatycznego przewijania" },
                
                // Accessibility Settings
                { "Settings_HighContrastMode", "Tryb wysokiego kontrastu" },
                { "Settings_LargeTextMode", "Tryb dużego tekstu" },
                { "Settings_ReduceAnimations", "Zmniejsz animacje" },
                
                // Buttons
                { "Settings_ResetToDefaults", "Przywróć domyślne" },
                { "Settings_Cancel", "Anuluj" },
                { "Settings_Apply", "Zastosuj" },
                
                // Language Names
                { "Language_English", "Angielski" },
                { "Language_Russian", "Rosyjski" },
                { "Language_Ukrainian", "Ukraiński" },
                { "Language_Polish", "Polski" },
                
                // Dialog Buttons
                { "Dialog_OK", "OK" },
                { "Dialog_Yes", "Tak" },
                { "Dialog_No", "Nie" },
                { "Dialog_Cancel", "Anuluj" },
                
                // Dialogs
                { "Dialog_QuitGame_Title", "Wyjdź z gry" },
                { "Dialog_QuitGame_Message", "Czy na pewno chcesz wyjść?\n\nTwój postęp zostanie automatycznie zapisany." },
                { "Dialog_Credits_Title", "Autorzy" },
                { "Dialog_Credits_Message", "System Visual Novel\n\nStworzony z użyciem WPF i C#\n\nRozwijany z pasją do opowiadania historii\n\n© 2025" },
                { "Dialog_ResetSettings_Title", "Resetuj ustawienia" },
                { "Dialog_ResetSettings_Message", "Czy na pewno chcesz zresetować wszystkie ustawienia do wartości domyślnych?" },
                { "Dialog_UnsavedChanges_Title", "Niezapisane zmiany" },
                { "Dialog_UnsavedChanges_Message", "Masz niezapisane zmiany. Czy chcesz je zapisać przed zamknięciem?" },
                { "Dialog_Error_Title", "Błąd" },
                
                // GameScene
                { "GameScene_Save", "Zapisz" },
                { "GameScene_Load", "Wczytaj" },
                { "GameScene_Menu", "Menu" },
                { "GameScene_SkipIndicator", "[ENTER/SPACJA/KLIKNIJ]" },
                { "GameScene_FPS", "FPS" },
                { "GameScene_UnknownLocation", "NIEZNANE" },
                { "GameScene_Sympathy", "Sympatia" },
                { "GameScene_Obedience", "Posłuszeństwo" },
                { "GameScene_TheEnd", "KONIEC" },
                { "GameScene_ThankYou", "Dziękujemy za grę!" },
                { "GameScene_Chapter", "Rozdział" },
                { "Chapter1_Title", "Rozdział 1: Religijna szlachta" },
                { "Chapter1_Subtitle", "Religijna szlachta" },
                
                // Scene1 - Winter Sun
                { "Scene1_Name", "Wioska" },
                { "Scene1_Character_Player", "Ty" },
                { "Scene1_Character_GirlWithCat", "Ada" },
                { "Scene1_Character_Mother", "Matka" },
                { "Scene1_Character_VillageChief", "Sołtys" },
                { "Scene1_Character_Narrator", "Narrator" },
                
                // Introduction
                { "Scene1_Introduction_WinterSun", "*wprowadzenie z opisem zimowego słońca*" },
                { "Scene1_VillageAppears", "Wreszcie wioska pojawiła się zza wzgórz i drzew." },
                { "Scene1_JourneyText_1", "Chociaż podróż była długa i wyczerpująca, ten widok nie przynosi radości." },
                { "Scene1_JourneyText_2", "Wiesz, że twoje pojawienie się tam wywoła tylko podejrzenia. Najlepsze, na co możesz liczyć, to to, że będą patrzeć na ciebie błagalnie." },
                { "Scene1_ChainsText", "Łańcuchy brzęczały na twoich ramionach, jakby przypominając o sobie. Trzymają czarny piec na twoich plecach. Cokolwiek w nim płonie, świeci nienaturalnym czerwonym światłem." },
                { "Scene1_CurseText", "Twoje przekleństwo, którego pochodzenie masz mgliste, niemal zatarte wspomnienia. Czasami wydaje się, że możesz przypomnieć sobie fragment przeszłości, ale natychmiast wymyka się i rozpuszcza bez śladu." },
                { "Scene1_LastSteps", "Jeszcze kilka kroków z grudami mokrego śniegu przyklejonymi do twoich ubrań. Ostatni wysiłek i możesz trochę odpocząć." },
                { "Scene1_VillageNoise", "Słyszysz hałas wioski—rozmowy, dźwięk młotków, wysokie krzyki dzieci gdzieś w oddali." },
                { "Scene1_MainStreet", "Tło zmienia się na główną ulicę wioski." },
                
                // Girl with cat
                { "Scene1_GirlWithCat_1", "..." },
                { "Scene1_GirlWithCat_2", "Witaj... Czy mogłabyś mnie uleczyć?" },
                { "Scene1_GirlWithCat_3_1", "To Matylda! Znaleźliśmy ją na ulicy, przy opuszczonej chacie! Była taka malutka! Około takiej wielkości..." },
                { "Scene1_GirlWithCat_3_2", "Och... W każdym razie, była mała... Ale karmiliśmy ją i wyrosła duża... Chodzimy razem na spacery... Uwielbiam ją głaskać i wszędzie nosić..." },
                { "Scene1_GirlWithCat_4", "Tak, bardzo, bardzo kocham swoją mamę... Powiedziała, że powinniśmy do ciebie przyjść..." },
                { "Scene1_GirlWithCat_Response_Cat_1", "Dziewczynka ożywia się i nawet patrzy na ciebie." },
                { "Scene1_GirlWithCat_Response_Cat_2", "Dziewczynka chciała pokazać rozmiar małej Matyldy rękami, ale kot, który wydaje się bardzo oswojony, zaczął się odtaczać." },
                { "Scene1_GirlWithCat_AfterBurn", "Dziewczynka wyraźnie ożywiła się, śmiejąc się, głaszcząc i przytulając kota." },
                
                // Mother
                { "Scene1_Mother_1", "Daj spokój, kochanie, nie teraz." },
                { "Scene1_Mother_2", "Witaj..." },
                { "Scene1_Mother_3", "Już po wszystkim?" },
                { "Scene1_Mother_4_1", "Tak..." },
                { "Scene1_Mother_4_2", "Z dziećmi jest łatwiej, prawda? Nie mają jeszcze bardzo ważnych wspomnień." },
                { "Scene1_Mother_Context", "Kątem oka zauważyłeś, że matka patrzy nieufnie." },
                
                // Village Chief
                { "Scene1_VillageChief_1_1", "Dzień dobry. Jestem sołtysem. Powiem od razu: mamy kilku ludzi, którym mogłabyś pomóc. Jeśli można to nazwać \"pomocą\". W przeciwnym razie, nie zatrzymuj się długo. Twoi ludzie straszą miejscowych." },
                { "Scene1_VillageChief_1_2", "Jeśli potrzebujesz ciepła lub jedzenia, pomożemy, na ile możemy, po prostu zrób to, co musisz, i odejdź jak najszybciej." },
                { "Scene1_VillageChief_2", "..." },
                { "Scene1_VillageChief_Response_WhoNeedsHelp", "Wszyscy, którzy nie bali się być na tej samej ulicy co ty. Lub prawie wszyscy. Niektórzy to tylko gapie." },
                { "Scene1_VillageChief_Response_LastBurner", "Około rok temu. Jak mogłaś zauważyć, nasza wioska to jedyna na wiele kilometrów wokół." },
                { "Scene1_VillageChief_Response_RestockSupplies", "Możesz poprosić miejscowych o jedzenie. Jeśli ci dadzą. Jest też sklep doktora... Jak się nazywa... Frankie. Myślę, że sprzeda ci wszystko, co chcesz kupić. Chociaż nie sądzę, żeby twoi mieli dużo pieniędzy." },
                
                // Player
                { "Scene1_Player_1", "Dzień dobry." },
                { "Scene1_Player_1_2", "Nie martw się. Po prostu z nią porozmawiam. Zwykle z dziećmi jest łatwiej." },
                { "Scene1_Player_2", "Jaki to kot?" },
                { "Scene1_Player_3", "Kochasz też swoją mamę?" },
                { "Scene1_Player_4", "Tak, zwykle z dziećmi jest łatwiej..." },
                { "Scene1_Player_Context_1_1", "Kobieta waha się. Nie wie, jak do ciebie podejść." },
                { "Scene1_Player_Context_1_3", "Dziewczynka patrzy w podłogę. Nawet teraz na ciebie nie patrzy." },
                
                // Context text
                { "Scene1_GirlApproaches", "Widzisz dziewczynkę zbliżającą się do ciebie z opuszczonymi oczami. Niesie kota na rękach. Za nią idzie to, co wydaje się być jej matką, z surowym, ale zdecydowanym wyrazem twarzy." },
                { "Scene1_GirlWithCat_Context", "Raz za razem widzisz tę dziwną scenę. W osadach jest lekarz. Ale ludzie przychodzą do ciebie po pomoc, prosząc cię, abyś ich \"uleczyła\". Wszyscy ci ludzie wyglądają zdrowo. Jedyną rzeczą, która ich łączy, jest smutek i zmęczenie w oczach. Dziewczynka wyraźnie wyglądała na odległą i zmęczoną. Widziałaś to wiele razy wcześniej." },
                
                // Burn Memory
                { "Scene1_BurnMemory_Prompt", "..." },
                { "Scene1_Choice_BurnMemory", "Spalić wspomnienie" },
                { "Scene1_BurnMemory_Effect_1", "Przez kilka sekund wzrok dziewczynki staje się nieostry. Patrzy przez ciebie, całkowicie zagubiona." },
                { "Scene1_BurnMemory_Effect_2", "Jakby budząc się, zaczyna się poruszać. Jej ręce odkrywają kota siedzącego na jej klatce piersiowej." },
                { "Scene1_BurnMemory_Effect_3", "Dziewczynka wyciąga ręce z kotem przed sobą i zaczyna go studiować oczami. Widzi go po raz pierwszy. Znowu. Uśmiecha się i odwraca, szukając matki." },
                
                // Choices
                { "Scene1_Choice_WhoNeedsHelp", "Kto potrzebuje mojej pomocy?" },
                { "Scene1_Choice_LastBurner", "Kiedy ostatnio mieliście palacza?" },
                { "Scene1_Choice_RestockSupplies", "Gdzie mogę uzupełnić zapasy?" },
                { "Scene1_Choice_ExitDialogue", "Wyjdź z dialogu" },
                
                // End
                { "Scene1_End_Hub", "Gracz wchodzi do centrum — ulicę wioski." }
            };
        }

        public void InitializeFromConfig(ConfigService configService)
        {
            var config = configService.GetConfig();
            if (!string.IsNullOrEmpty(config.Language) && _translations.ContainsKey(config.Language))
            {
                _currentLanguage = config.Language;
            }
        }
    }
}

