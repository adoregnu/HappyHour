using CefSharp.DevTools.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeepL;
using DeepL.Model;
using HappyHour.Extension;
using HappyHour.Model;
using ICSharpCode.AvalonEdit.Document;
using MvvmDialogs;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace HappyHour.ViewModel
{

    class TranslatorViewModel : ObservableObject, IModalDialogViewModel
    {
        private bool? _dialogResult;
        public bool? DialogResult
        {
            get => _dialogResult;
            set => SetProperty(ref _dialogResult, value);
        }

        private readonly TextDocument _leftDocument = new();
        private readonly TextDocument _rightDocument = new();
        public TextDocument LeftDocument
        {
            get { return _leftDocument; }
        }
        public TextDocument RightDocument
        {
            get { return _rightDocument; }
        }

        public MText Text { get; set; }

        private readonly List<string> _langsLeft =
        [
            LanguageCode.Japanese,
            LanguageCode.English,
            LanguageCode.Korean,
        ];
        private readonly List<string> _langsRight =
        [
            LanguageCode.Korean,
            LanguageCode.EnglishAmerican,
        ];
        public List<string> LangsLeft => _langsLeft;
        public List<string> LangsRight => _langsRight;
        private string _selectedLeftLang;
        public string SelectedLeftLang
        {
            get => _selectedLeftLang;
            set => SetProperty(ref _selectedLeftLang, value);
        }
        private string _selectedRightLang;
        public string SelectedRightLang
        {
            get => _selectedRightLang;
            set => SetProperty(ref _selectedRightLang, value);
        }
        private readonly Translator _translator;
        private readonly ChatClient _chatClient;

        private string _selectedTranslator = "DeepL";
        public string SelectedTranslator {
            get => _selectedTranslator;
            set
            {
                if (value != _selectedTranslator)
                {
                    RightDocument.Text = string.Empty;
                }
                SetProperty(ref _selectedTranslator, value);
                OnPropertyChanged(nameof(IsChatVisible));
            }
        } 
        private static readonly List<string> list =
        [
            "DeepL",
            "OpenAI",
        ];
        public List<string> Translators { get; } = list;

        private string _chatText;
        public string ChatText 
        {
            get => _chatText;
            set => SetProperty(ref _chatText, value);
        }

        public Visibility IsChatVisible
        {
            get => SelectedTranslator == "OpenAI" ? Visibility.Visible : Visibility.Collapsed;
        }

        public ICommand CmdUpdateSource { get; private set; }
        public ICommand CmdUpdateTarget { get; private set; }
        public ICommand CmdTranslate { get; private set; }
        public ICommand CmdChat { get; private set; }
        public Action<string, string> UpdateItem { get; internal set; }

        public TranslatorViewModel(MText text)
        {
            Text = text;
            LeftDocument.Text = text.Text;
            SelectedLeftLang = text.Lang == "jp" ? "ja" : text.Lang;

            CmdUpdateSource = new RelayCommand(OnUpdateSource);
            CmdUpdateTarget = new RelayCommand(OnUpdateTarget);
            CmdTranslate = new RelayCommand(OnTranslate);
            CmdChat = new RelayCommand(OnChat);

            _translator = new (App.Current.GetConf("api_keys", "deepl"));
            _chatClient = new (model: "gpt-5.2", apiKey: App.Current.GetConf("api_keys", "openai"));
        }

        void OnChat()
        {
            if (string.IsNullOrEmpty(ChatText)) return;

            UiServices.WaitCursor(true);
            RightDocument.Text += "\n> " + ChatText;

            NotifyTask.Create(_chatClient.CompleteChatAsync(
            [
                new UserChatMessage(ChatText)
            ])).PropertyChanged += OnTranslationCompleted;
            ChatText = string.Empty;
        }

        void OnTranslate()
        {
            if (string.IsNullOrEmpty(SelectedLeftLang) || string.IsNullOrEmpty(SelectedRightLang))
            {
                Log.Print("Please select both source and target languages.");
                return;
            }

            UiServices.WaitCursor(true);
            if (SelectedTranslator == "DeepL")
            {
                NotifyTask.Create(_translator.TranslateTextAsync(LeftDocument.Text, SelectedLeftLang, SelectedRightLang))
                    .PropertyChanged += OnTranslationCompleted;
            }
            else if (SelectedTranslator == "OpenAI")
            {
                // 번역 프롬프트 구성 (System 메시지로 역할 부여)
                NotifyTask.Create(_chatClient.CompleteChatAsync(
                [
                    new SystemChatMessage($"Translate the following {SelectedLeftLang} text into {SelectedRightLang}."),
                    new UserChatMessage(LeftDocument.Text)
                ])).PropertyChanged += OnTranslationCompleted;
            }
        }

        void OnUpdateSource()
        {
            UpdateItem?.Invoke(LeftDocument.Text, SelectedLeftLang);
        }
        void OnUpdateTarget()
        {
            if (!string.IsNullOrEmpty(RightDocument.Text))
            {
                DialogResult = true;
            }
        }

        void OnTranslationCompleted(object s, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Result")
            {
                if (SelectedTranslator == "DeepL")
                {
                    TextResult result = ((dynamic)s).Result;
                    RightDocument.Text = result.Text;
                }
                else if (SelectedTranslator == "OpenAI")
                {
                    ChatCompletion completion = ((dynamic)s).Result;
                    RightDocument.Text += completion.Content[0].Text;
                }
            }
           UiServices.WaitCursor(false);
        }
    }
}
