using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeepL;
using DeepL.Model;
using HappyHour.Extension;
using HappyHour.Model;
using ICSharpCode.AvalonEdit.Document;
using MvvmDialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace HappyHour.ViewModel
{

    class TranslatorViewModel : ObservableObject, IModalDialogViewModel
    {
        private const string authKey = "";
        public bool? DialogResult { get; set; }

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

        public IEnumerable<MText> TextSources { get; set; }
        public MText Text { get; set; }

        private readonly List<string> _langsLeft =
        [
            LanguageCode.Japanese,
            LanguageCode.English,
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
            set 
            {
                SetProperty(ref _selectedRightLang, value);

                UiServices.WaitCursor(true);
                var translator = new Translator(authKey);
                NotifyTask.Create(translator.TranslateTextAsync(LeftDocument.Text, SelectedLeftLang, SelectedRightLang))
                    .PropertyChanged += OnTranslatioinCompleted;
            }
        }

        public ICommand CmdUpdate { get; private set; }
        public TranslatorViewModel(MText text)
        {
            Text = text;
            LeftDocument.Text = text.Text;
            SelectedLeftLang = text.Lang == "jp" ? "ja" : text.Lang;

            CmdUpdate = new RelayCommand(OnUpdate);
        }
        void OnUpdate()
        {
        }

        void OnTranslatioinCompleted(object s, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Result")
            {
                TextResult result = ((dynamic)s).Result;
                RightDocument.Text = result.Text;
                UiServices.WaitCursor(false);
            }
        }
    }
}
