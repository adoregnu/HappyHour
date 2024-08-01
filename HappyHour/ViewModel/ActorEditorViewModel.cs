using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MvvmDialogs;

using HappyHour.Model;
using HappyHour.Interfaces;
using HappyHour.Spider;
using CommunityToolkit.Mvvm.Messaging;
using System.Threading.Tasks;
using HappyHour.Extension;
using System.Threading;

namespace HappyHour.ViewModel
{
    internal class ActorInitial : ObservableObject
    {
        private bool _isChecked;

        public ActorEditorViewModel ActorEditor;
        public string Initial { get; set; }
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                SetProperty(ref _isChecked, value);
                UiServices.Invoke(async () => await ActorEditor.OnActorAlphabet(Initial, value), true);
            }
        }
        public void UnCheck()
        {
            _isChecked = false;
            OnPropertyChanged(nameof(IsChecked));
        }
        public async Task Check()
        {
            await ActorEditor.OnActorAlphabet(Initial, true);
            _isChecked = true;
            OnPropertyChanged(nameof(IsChecked));
        }
    }

    internal class ActorEditorViewModel : ObservableRecipient, IModalDialogViewModel
    {
        private string _searchText;
        private bool? _dialogResult = false;
        private readonly MovieDbContext _db = App.Current.DbContext;

        private ObservableCollection<ActorName> _actorNames = [];
        public ObservableCollection<ActorName> ActorNames
        {
            get => _actorNames;
            set => SetProperty(ref _actorNames, value);
        }
        public ActorName SelectedActorName { get; set; }

        private ObservableCollection<Actor> _actors = [];
        public ObservableCollection<Actor> Actors
        {
            get => _actors;
            private set => SetProperty(ref _actors, value);
        }

        private Actor _actor;
        public Actor SelectedActor
        {
            get => _actor;
            set
            {
                SetProperty(ref _actor, value);
                if (value != null)
                {
                    ActorNames.Clear();
                    foreach (var an in _actor.Names)
                    {
                        ActorNames.Add(an);
                    }
                }
            }
        }

        private ActorOrderType _orderType = ActorOrderType.Key;
        public ActorOrderType OrderType
        {
            get => _orderType;
            set => SetProperty(ref _orderType, value);
        }

        public List<ActorInitial> ActorInitials { get; private set; }
        public List<SpiderBase> SpiderList { get; set; }

        public bool? DialogResult
        {
            get => _dialogResult;
            private set => SetProperty(ref _dialogResult, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                OnPropertyChanged(nameof(SearchNames));
            }
        }

        public List<ActorName> SearchNames => _db.GetActorNames(SearchText);

        ActorName _selectedSearchName = null;
        public ActorName SelectedSearchName
        {
            get => _selectedSearchName;
            set => SetProperty(ref _selectedSearchName, value);
        }

        public IMediaList MediaList { get; set; }
        public IMainView MainView { get; set; }
        public IDialogService DialogService { get; set; }

        public ICommand CmdDoubleClick { get; private set; }
        public ICommand CmdSearchNameDoubleClick { get; private set; }
        public ICommand CmdMergeActors { get; private set; }
        public ICommand CmdRemoveActor {  get; private set; }
        public ICommand CmdClearActors { get; private set; }
        public ICommand CmdDeleteNameOfActor { get; private set; }
        public ICommand CmdClosed { get; private set; }

        public ActorEditorViewModel(IMainView mainView)
        {
            MainView = mainView;
            CmdDoubleClick = new RelayCommand(OnDoubleClicked);
            CmdSearchNameDoubleClick = new RelayCommand(OnSearchNameDoubleClicked);
            CmdMergeActors = new RelayCommand<object>(
                OnMergeActors,
                p => p is IList<object> list && list.Count > 1);
            CmdRemoveActor = new RelayCommand<Actor>(OnRemoveActor);
            CmdClearActors = new RelayCommand(OnClearActors);
            CmdClosed = new RelayCommand(OnClose);

            ActorInitials = Enumerable.Range('A', 'Z' - 'A' + 1)
                .Select(c => new ActorInitial
                {
                    ActorEditor = this,
                    Initial = ((char)c).ToString(),
                }).ToList();
            ActorInitials.Insert(0, new ActorInitial
            {
                ActorEditor = this,
                Initial = "All",
            });
            //MainView.OnViewUpdate += ReceiveAsync;//(s, e) => { };
        }

        async void ReceiveAsync(ViewEventArgs e)
        {
            if (e.Message != "Refresh")
            {
                return;
            }

            Log.Print($"ActorEditorViewModel received {e.Message}");
            List<ActorInitial> initials = [];
            ActorInitials.ForEach(i => { if (i.IsChecked) initials.Add(i); });
            OnClearActors();
            foreach (var i in initials)
            {
                await i.Check();
            }
            Log.Print($"ActorEditorViewModel::ReceiveAsync End");
            //initials.ForEach(async i => await i.Check());
        }

        private void OnRemoveActor(Actor actor)
        {
            if (actor == null) return;

            SelectedActor = null;
            ActorNames.Clear();
            Actors.Remove(actor);

            _db.RemoveActor(actor);
        }

        public async Task OnActorAlphabet(string p, bool isSelected)
        {
            string keyword = p;
            int limit = 0;
            if (p == "All")
            {
                foreach (var initial in ActorInitials)
                {
                    if (initial.Initial != "All") { initial.UnCheck(); }
                }
                keyword = null;
                limit = 50;
            }

            if (isSelected)
            {
                var actors = await _db.GetActors(keyword, OrderType, limit);
                actors?.ForEach(Actors.Add);
            }
            else if (p == "All")
            {
                Actors.Clear();
            }
            else
            {
                List<Actor> tmpList = [];
                foreach (var actor in Actors)
                {
                    if (actor.Names.Any(n => n.Name.Text.StartsWith(p)))
                    {
                        tmpList.Add(actor);
                    }
                }
                tmpList.ForEach(a => Actors.Remove(a));
            }
        }
        private async void OnDoubleClicked()
        {
            if (SelectedActor == null) { return; }

            var movies = await _db.GetMovies(SelectedActor);
            MediaList?.LoadItems(movies);
        }

        private void OnSearchNameDoubleClicked()
        {
            if (SelectedSearchName == null) { return; }
            if (Actors.Any(a => a.Names.Contains(SelectedSearchName))) { return; }

            var actor = _db.GetActor(SelectedSearchName);
            if (actor != null)
            {
                Actors.Add(actor);
            }
        }

        private void OnClearActors()
        {
            ActorInitials.ForEach(i => i.UnCheck());
            Actors.Clear();
        }

        private void OnMergeActors(object p)
        {
            var selectedActors = (p as IList<object>).Select(o => o as Actor).ToList();
            _db.MergeActors(selectedActors, a => Actors.Remove(a));
        }

        private void OnClose()
        {
            Messenger.Unregister<ViewEventArgs>(this);
        }
    }
}
