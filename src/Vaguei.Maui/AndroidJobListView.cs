using System.Collections;
using System.Collections.Specialized;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.Handlers;
using Vaguei.Desktop.ViewModels;
using AColor = Android.Graphics.Color;
using AView = Android.Views.View;

namespace Vaguei.Maui;

public sealed class AndroidJobListView : Microsoft.Maui.Controls.View
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource),
        typeof(IEnumerable),
        typeof(AndroidJobListView),
        null,
        propertyChanged: OnItemsSourceChanged);

    private INotifyCollectionChanged? _observedCollection;

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public event EventHandler<string>? OpenRequested;

    internal IReadOnlyList<JobResultItemViewModel> Snapshot() =>
        ItemsSource?.Cast<JobResultItemViewModel>().ToArray() ?? [];

    internal void RaiseOpenRequested(string url) => OpenRequested?.Invoke(this, url);

    public void ScrollToTop() =>
        (Handler as AndroidJobListViewHandler)?.ScrollToTop();

    public void RefreshTheme() =>
        (Handler as AndroidJobListViewHandler)?.RefreshTheme();

    private static void OnItemsSourceChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        var view = (AndroidJobListView)bindable;
        if (view._observedCollection is not null)
            view._observedCollection.CollectionChanged -= view.OnCollectionChanged;
        view._observedCollection = newValue as INotifyCollectionChanged;
        if (view._observedCollection is not null)
            view._observedCollection.CollectionChanged += view.OnCollectionChanged;
        (view.Handler as AndroidJobListViewHandler)?.Reload();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs) =>
        MainThread.BeginInvokeOnMainThread(() =>
            (Handler as AndroidJobListViewHandler)?.Reload());
}

public sealed class AndroidJobListViewHandler
    : ViewHandler<AndroidJobListView, RecyclerView>
{
    private JobAdapter? _adapter;

    public static readonly IPropertyMapper<AndroidJobListView, AndroidJobListViewHandler> Mapper =
        new PropertyMapper<AndroidJobListView, AndroidJobListViewHandler>(ViewMapper)
        {
            [nameof(AndroidJobListView.ItemsSource)] = static (handler, _) => handler.Reload()
        };

    public AndroidJobListViewHandler() : base(Mapper)
    {
    }

    protected override RecyclerView CreatePlatformView()
    {
        var recyclerView = new RecyclerView(Context)
        {
            HasFixedSize = false,
            NestedScrollingEnabled = true,
            OverScrollMode = OverScrollMode.Never,
            VerticalScrollBarEnabled = false
        };
        recyclerView.SetClipChildren(true);
        recyclerView.SetClipToPadding(true);
        var layoutManager = new LinearLayoutManager(Context);
        layoutManager.ItemPrefetchEnabled = true;
        layoutManager.InitialPrefetchItemCount = 5;
        recyclerView.SetLayoutManager(layoutManager);
        recyclerView.SetItemAnimator(null);
        recyclerView.SetItemViewCacheSize(12);
        recyclerView.GetRecycledViewPool().SetMaxRecycledViews(0, 24);
        _adapter = new JobAdapter(VirtualView);
        recyclerView.SetAdapter(_adapter);
        return recyclerView;
    }

    protected override void ConnectHandler(RecyclerView platformView)
    {
        base.ConnectHandler(platformView);
        Reload();
    }

    protected override void DisconnectHandler(RecyclerView platformView)
    {
        platformView.SetAdapter(null);
        _adapter?.Dispose();
        _adapter = null;
        base.DisconnectHandler(platformView);
    }

    internal void Reload()
    {
        if (_adapter is null) return;
        _adapter.SetItems(VirtualView.Snapshot(), IsDarkTheme());
    }

    internal void ScrollToTop() => PlatformView?.ScrollToPosition(0);

    internal void RefreshTheme()
    {
        if (_adapter is null) return;
        _adapter.SetTheme(IsDarkTheme());
    }

    private static bool IsDarkTheme() =>
        Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark;

    private sealed class JobAdapter(AndroidJobListView owner)
        : RecyclerView.Adapter
    {
        private IReadOnlyList<JobResultItemViewModel> _items = [];
        private bool _dark;

        public override int ItemCount => _items.Count;

        public void SetItems(IReadOnlyList<JobResultItemViewModel> items, bool dark)
        {
            var previousItems = _items;
            var themeChanged = _dark != dark;
            _items = items;
            _dark = dark;

            // A busca progressiva pode publicar uma primeira cobertura e, pouco
            // depois, a cobertura consolidada. NotifyDataSetChanged recriava a
            // janela visível nessa segunda etapa e interrompia a posição do
            // usuário. O diff nativo preserva o RecyclerView e só altera os
            // cards que realmente mudaram.
            if (themeChanged || previousItems.Count == 0 || items.Count == 0)
            {
                NotifyDataSetChanged();
                return;
            }

            DiffUtil.CalculateDiff(new JobDiffCallback(previousItems, items), false)
                .DispatchUpdatesTo(this);
        }

        public void SetTheme(bool dark)
        {
            if (_dark == dark) return;
            _dark = dark;
            NotifyDataSetChanged();
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType) =>
            CreateHolder(parent.Context!);

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var jobHolder = (JobHolder)holder;
            var item = _items[position];
            jobHolder.Bind(item, _dark);
            jobHolder.SetClickHandlers(owner, item);
        }

        private sealed class JobDiffCallback(
            IReadOnlyList<JobResultItemViewModel> oldItems,
            IReadOnlyList<JobResultItemViewModel> newItems)
            : DiffUtil.Callback
        {
            public override int OldListSize => oldItems.Count;

            public override int NewListSize => newItems.Count;

            public override bool AreItemsTheSame(int oldItemPosition, int newItemPosition) =>
                string.Equals(
                    oldItems[oldItemPosition].FavoriteKey,
                    newItems[newItemPosition].FavoriteKey,
                    StringComparison.Ordinal);

            public override bool AreContentsTheSame(int oldItemPosition, int newItemPosition)
            {
                var oldItem = oldItems[oldItemPosition];
                var newItem = newItems[newItemPosition];

                return oldItem.Title == newItem.Title &&
                       oldItem.Company == newItem.Company &&
                       oldItem.Location == newItem.Location &&
                       oldItem.Score == newItem.Score &&
                       oldItem.ShowCompatibility == newItem.ShowCompatibility &&
                       oldItem.Source == newItem.Source &&
                       oldItem.Url == newItem.Url &&
                       oldItem.Published == newItem.Published &&
                       oldItem.IsFavorite == newItem.IsFavorite;
            }
        }

        private static JobHolder CreateHolder(Context context)
        {
            var density = context.Resources?.DisplayMetrics?.Density ?? 1;
            int Dp(float value) => (int)Math.Round(value * density);

            var card = new LinearLayout(context)
            {
                Orientation = Orientation.Vertical
            };
            card.SetPadding(Dp(12), Dp(7), Dp(12), Dp(7));
            card.LayoutParameters = new RecyclerView.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent)
            {
                BottomMargin = Dp(8)
            };

            var title = Text(context, 14, true, 2);
            var company = Text(context, 11.5f, true, 1);
            var location = Text(context, 10.5f, false, 1);
            var compatibility = Text(context, 10.5f, true, 1);
            compatibility.SetPadding(0, Dp(2), 0, 0);
            card.AddView(title);
            card.AddView(company);
            card.AddView(location);
            card.AddView(compatibility);

            var actions = new LinearLayout(context)
            {
                Orientation = Orientation.Horizontal
            };
            actions.SetGravity(GravityFlags.CenterVertical);
            actions.SetPadding(0, Dp(2), 0, 0);
            var metadata = new LinearLayout(context) { Orientation = Orientation.Vertical };
            var source = Text(context, 9.5f, false, 1);
            var published = Text(context, 9.5f, false, 1);
            metadata.AddView(source);
            metadata.AddView(published);
            actions.AddView(metadata, new LinearLayout.LayoutParams(0, Dp(36), 1));

            var favorite = Text(context, 17, false, 1);
            favorite.Gravity = GravityFlags.Center;
            actions.AddView(favorite, ActionParams(Dp(36), Dp(36), Dp(7)));

            var open = new TextView(context)
            {
                Text = "Abrir ›",
                Gravity = GravityFlags.Center,
                Clickable = true,
                Focusable = true
            };
            open.SetTextSize(ComplexUnitType.Sp, 12.5f);
            actions.AddView(open, ActionParams(Dp(74), Dp(36), Dp(7)));
            card.AddView(actions);

            return new JobHolder(
                card,
                new JobViews(title, company, location, compatibility, source, published, favorite, open));
        }

        private static LinearLayout.LayoutParams ActionParams(int width, int height, int marginStart) =>
            new(width, height) { MarginStart = marginStart };

        private static TextView Text(Context context, float size, bool bold, int maxLines)
        {
            var view = new TextView(context)
            {
                Ellipsize = Android.Text.TextUtils.TruncateAt.End
            };
            view.SetMaxLines(maxLines);
            view.SetIncludeFontPadding(false);
            view.SetTextSize(ComplexUnitType.Sp, size);
            view.SetTypeface(Android.Graphics.Typeface.Default,
                bold ? TypefaceStyle.Bold : TypefaceStyle.Normal);
            return view;
        }

        private sealed class JobHolder(AView itemView, JobViews views) : RecyclerView.ViewHolder(itemView)
        {
            private readonly JobViews _views = views;
            private EventHandler? _favoriteClick;
            private EventHandler? _openClick;
            public TextView Favorite => _views.Favorite;
            public TextView Open => _views.Open;

            public void SetClickHandlers(AndroidJobListView owner, JobResultItemViewModel item)
            {
                if (_favoriteClick is not null) Favorite.Click -= _favoriteClick;
                if (_openClick is not null) Open.Click -= _openClick;
                _favoriteClick = (_, _) =>
                {
                    if (item.ToggleFavoriteCommand.CanExecute(null))
                        item.ToggleFavoriteCommand.Execute(null);
                    Favorite.Text = item.FavoriteSymbol;
                };
                _openClick = (_, _) => owner.RaiseOpenRequested(item.Url);
                Favorite.Click += _favoriteClick;
                Open.Click += _openClick;
            }

            public void Bind(JobResultItemViewModel item, bool dark)
            {
                var primary = dark ? AColor.Rgb(242, 242, 242) : AColor.Rgb(21, 21, 21);
                var secondary = dark ? AColor.Rgb(181, 181, 181) : AColor.Rgb(98, 104, 121);
                var surface = dark ? AColor.Rgb(27, 27, 27) : AColor.White;
                var action = dark ? AColor.Rgb(41, 41, 41) : AColor.Rgb(236, 238, 244);
                var stroke = dark ? AColor.Rgb(56, 56, 56) : AColor.Rgb(221, 225, 234);
                _views.Title.Text = item.Title;
                _views.Company.Text = item.Company;
                _views.Location.Text = item.Location;
                _views.Compatibility.Text = $"Compatibilidade {item.Score}";
                _views.Compatibility.Visibility = item.ShowCompatibility ? ViewStates.Visible : ViewStates.Gone;
                _views.Source.Text = item.Source;
                _views.Published.Text = item.Published;
                _views.Favorite.Text = item.FavoriteSymbol;
                foreach (var text in new[] { _views.Title, _views.Company, _views.Compatibility, _views.Favorite, _views.Open })
                    text.SetTextColor(primary);
                foreach (var text in new[] { _views.Location, _views.Source, _views.Published })
                    text.SetTextColor(secondary);
                ItemView.Background = Rounded(surface, stroke, 16, ItemView.Context!);
                _views.Favorite.Background = Rounded(action, null, 12, ItemView.Context!);
                _views.Open.Background = Rounded(action, null, 12, ItemView.Context!);
            }
        }

        private static GradientDrawable Rounded(AColor fill, AColor? stroke, float radiusDp, Context context)
        {
            var density = context.Resources?.DisplayMetrics?.Density ?? 1;
            var drawable = new GradientDrawable();
            drawable.SetColor(fill);
            drawable.SetCornerRadius(radiusDp * density);
            if (stroke is not null) drawable.SetStroke(Math.Max(1, (int)density), stroke.Value);
            return drawable;
        }

        private sealed record JobViews(
            TextView Title,
            TextView Company,
            TextView Location,
            TextView Compatibility,
            TextView Source,
            TextView Published,
            TextView Favorite,
            TextView Open);
    }
}
