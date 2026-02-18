using System.ComponentModel;
using HappyHour.Model;
using HappyHour.ViewModel;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace HappyHour.View.Controls
{
    /// <summary>
    /// Interaction logic for LangEditBox.xaml
    /// </summary>
    public partial class LangComboBox : UserControl
    {
        public IEnumerable TextSource 
        {
            get { return (IEnumerable)GetValue(TextSourceValueProperty); }
            set { SetValue(TextSourceValueProperty, value); }
        }
        public static readonly DependencyProperty TextSourceValueProperty =
          DependencyProperty.Register(
              "TextSource",
              typeof(IEnumerable),
              typeof(LangComboBox),
              new PropertyMetadata(null, OnTextSourceChanged));

        public object DbEntry
        {
            get { return (object)GetValue(DbEntryProperty); }
            set { SetValue(DbEntryProperty, value); }
        }
        public static readonly DependencyProperty DbEntryProperty =
          DependencyProperty.Register(
              "DbEntry",
              typeof(object),
              typeof(LangComboBox),
              new PropertyMetadata(null));

        private static void OnTextSourceChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var control = sender as LangComboBox;
            control?.UpdateItemsSource();
        }

        private static bool CheckAndRemoveText<T>(ICollection<T> texts, T item)
        {
            if (texts.Count > 1)
            {
                texts.Remove(item);
                return true;
            }
            else
            {
                Log.Print("Cannot delete the last remaining text entry.");
            }
            return false;
        }

        public void OnDelete(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not MText itemToDelete) return;
            //get type of DbEntry
            var dbEntryType = DbEntry?.GetType();
            bool removed;
            if (dbEntryType == typeof(HashSet<ShortText>))
            {
                removed = CheckAndRemoveText((ICollection<ShortText>)DbEntry, (ShortText)itemToDelete);
            }
            else if (dbEntryType == typeof(HashSet<LongText>))
            {
                removed = CheckAndRemoveText((ICollection<LongText>)DbEntry, (LongText)itemToDelete); 
            }
            else
            {
                removed = false;
                Log.Print($"Unsupported DbEntry type: {dbEntryType}");
            }
            if (removed)
            {
                UpdateItemsSource();
            }
        }

        private void UpdateItemsSource()
        {
            if (TextSource == null)
            {
                _textList.ItemsSource = null;
                return;
            }

            var list = new List<object>();
            foreach (var item in TextSource)
            {
                list.Add(item);
            }

            var view = new ListCollectionView(list);
            view.SortDescriptions.Add(new SortDescription("Lang", ListSortDirection.Descending));
            view.SortDescriptions.Add(new SortDescription("Text", ListSortDirection.Ascending));

            _textList.ItemsSource = view;

            if (!view.IsEmpty)
            {
                view.MoveCurrentToFirst();
                _textList.SelectedItem = view.CurrentItem;
            }
        }

        private static void AddOrUpdateText<T>(ICollection<T> texts, string text, string lang)
            where T : MText, new()
        {
            lang = "ja" == lang ? "jp" : lang;

            var existing = texts.FirstOrDefault(t => t.Lang == lang);
            if (existing == null)
            {
                texts.Add(new T() { Lang = lang, Text = text });
            }
            else
            {
                Log.Print($"Updating existing text for lang '{lang}'");
                existing.Text = text;
            }
        }

        void OnUpdateItem(string text, string lang)
        {
            lang = "ja" == lang ? "jp" : lang;

            var item2Update = _textList.SelectedItem as MText;
            item2Update.Text = text;
            item2Update.Lang = lang;
            _textList.Items.Refresh();
        }

        private void Translate(object sender, RoutedEventArgs e)
        {
            var dataContext = new TranslatorViewModel(_textList.SelectedItem as MText)
            {
                UpdateItem = OnUpdateItem,
            };

            var dialog = new TranslatorDialog()
            {
                DataContext = dataContext,
            };
            bool? dialogResult = dialog.ShowDialog();
            if (dialogResult != true)
            {
                //Log.Print("Translation dialog cancelled.");
                return;
            }

            //get type of DbEntry
            var dbEntryType = DbEntry?.GetType();

            bool updated = true;

            if (dbEntryType == typeof(HashSet<ShortText>))
            {
                AddOrUpdateText((ICollection<ShortText>)DbEntry,
                    dataContext.RightDocument.Text, dataContext.SelectedRightLang);
            }
            else if (dbEntryType == typeof(HashSet<LongText>))
            {
                AddOrUpdateText((ICollection<LongText>)DbEntry,
                    dataContext.RightDocument.Text, dataContext.SelectedRightLang);
            }
            else
            {
                updated = false;
                Log.Print($"Unsupported DbEntry type: {dbEntryType}");
            }

            if (updated)
            {
                UpdateItemsSource();
            }
        }
        public LangComboBox()
        {
            InitializeComponent();
        }
    }
}
