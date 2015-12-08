﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.VisualStudio.R.Package.Wpf;

namespace Microsoft.VisualStudio.R.Package.DataInspect {
    /// <summary>
    /// A grid control that populates columns dynamically just like DataGrid's rows are loaded in stack panel
    /// 
    /// This stacks rows vertically, and each row stacks cells horizontally.
    /// Vertical scroll is handled by this controls's panel.
    /// Horizontal scroll is handled by propagating horizontal scroll event to each row.
    /// The source of horitonal scroll event comes from a scrollbar, named as HorizontalScrollBar in template. The scrollbar should stand alone outside scrollviewer.
    /// </summary>
    public class DynamicGrid : MultiSelector {
        private LinkedList<DynamicGridRow> _realizedRows = new LinkedList<DynamicGridRow>();

        static DynamicGrid() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DynamicGrid), new FrameworkPropertyMetadata(typeof(DynamicGrid)));
        }

        #region DataSource

        public IList RowHeaderSource { get; set; }

        public IList ColumnHeaderSource { get; set; }

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue) {
            base.OnItemsSourceChanged(oldValue, newValue);

            foreach (var item in newValue) {
                var rowSource = item as IList;
                if (rowSource != null) {
                    if (rowSource.Count > 0) {
                        _layoutInfo = new LayoutInfo() { FirstItemIndex = 0, FirstItemOffset = 0.0, ItemCountInViewport = 1 };
                    }
                } else {
                    throw new NotSupportedException($"{nameof(DynamicGrid)} supports only nested collection for ItemsSource");
                }
                break;
            }
        }

        #endregion

        #region override

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();

            EnsureHorizontalScrollbar();
        }

        protected override DependencyObject GetContainerForItemOverride() {
            return new DynamicGridRow();
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item) {
            base.PrepareContainerForItemOverride(element, item);

            DynamicGridRow row = (DynamicGridRow)element;

            _realizedRows.AddFirst(row.Track);  // ObservableCollection.Replace cause this fail, as it has been added already. That's fine for now.

            row.Header = RowHeaderSource[Items.IndexOf(item)];
            row.Prepare(this, item);
        }

        protected override void ClearContainerForItemOverride(DependencyObject element, object item) {
            base.ClearContainerForItemOverride(element, item);

            DynamicGridRow row = (DynamicGridRow)element;

            row.Header = null;

            _realizedRows.Remove(row.Track);
            row.CleanUp(this, item);
        }

        #endregion override

        #region RowHeader

        public double RowHeaderActualWidth {
            get { return (double)GetValue(RowHeaderActualWidthProperty); }
            internal set { SetValue(RowHeaderActualWidthPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey RowHeaderActualWidthPropertyKey =
            DependencyProperty.RegisterReadOnly("RowHeaderActualWidth", typeof(double), typeof(DynamicGrid), new FrameworkPropertyMetadata(0.0, new PropertyChangedCallback(OnNotifyRowHeaderPropertyChanged)));

        public static readonly DependencyProperty RowHeaderActualWidthProperty = RowHeaderActualWidthPropertyKey.DependencyProperty;

        private static void OnNotifyRowHeaderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((DynamicGrid)d).OnNotifyRowHeaderPropertyChanged();
        }

        private void OnNotifyRowHeaderPropertyChanged() {
            foreach (var row in _realizedRows) {
                row.NotifyRowHeader();
            }
        }

        #endregion

        #region ColumnHeader

        private DynamicGridColumnHeadersPresenter _columnHeadersPresenter;

        internal DynamicGridColumnHeadersPresenter ColumnHeadersPresenter {
            get {
                return _columnHeadersPresenter;
            }
            set {
                _columnHeadersPresenter = value;
                _columnHeadersPresenter.ItemsSource = ColumnHeaderSource;
            }
        }

        #endregion

        #region Columns and Horizontal scroll

        private SortedList<int, MaxDouble> _columns = new SortedList<int, MaxDouble>();
        internal MaxDouble GetColumnWidth(int index) {
            MaxDouble stack;
            if (_columns.TryGetValue(index, out stack)) {
                return stack;
            }

            stack = new MaxDouble(0.0);
            _columns.Add(index, stack);

            return stack;
        }

        private const double EstimatedWidth = 20.0; // TODO: configurable

        Size _panelSize;
        LayoutInfo _layoutInfo;

        private LayoutInfo ComputeHorizontalScroll(Size size) {
            int horizontalOffset = (int)HorizontalOffset;
            int startIndex = (int)Math.Floor(horizontalOffset / EstimatedWidth);
            double width = _layoutInfo.FirstItemOffset;
            int count = 0;

            int currentIndex = startIndex;
            while (currentIndex < Items.Count) {
                MaxDouble columnWidth;
                if (_columns.TryGetValue(currentIndex, out columnWidth)) {
                    width += columnWidth.Max;
                    count++;
                } else {
                    width += EstimatedWidth;
                    count++;
                }

                if (width > size.Width) {
                    break;
                }

                currentIndex++;
            }

            count = currentIndex - startIndex;

            ExtentWidth = width + (Items.Count - count) * EstimatedWidth;
            ViewportWidth = width;
            ScrollableWidth = ExtentWidth - ViewportWidth;

            return new LayoutInfo() {
                FirstItemIndex = startIndex,
                FirstItemOffset = _layoutInfo.FirstItemOffset,
                ItemCountInViewport = count,
            };
        }

        internal void OnViewportSizeChanged(Size newSize) {
            _panelSize = newSize;

            var newLayoutInfo = ComputeHorizontalScroll(newSize);

            if (!_layoutInfo.Equals(newLayoutInfo)) {
                _layoutInfo = newLayoutInfo;

                if (_columnHeadersPresenter != null) {
                    _columnHeadersPresenter.ScrollChanged();
                }

                // TODO: move to background(?)
                var toRemove = _columns.Where(c => c.Key < _layoutInfo.FirstItemIndex || c.Key >= (_layoutInfo.FirstItemIndex + _layoutInfo.ItemCountInViewport)).ToList();
                foreach (var item in toRemove) {
                    _columns.Remove(item.Key);
                }

                foreach (var row in _realizedRows) {
                    row.ScrollChanged();
                }
            }
        }

        internal LayoutInfo GetLayoutInfo(Size size) {
            return _layoutInfo;
        }

        public static readonly DependencyProperty ScrollableWidthProperty =
                DependencyProperty.Register(
                        "ScrollableWidth",
                        typeof(double),
                        typeof(DynamicGrid),
                        new FrameworkPropertyMetadata(0d));

        public double ScrollableWidth {
            get {
                return (double) GetValue(ScrollableWidthProperty);
            }
            set {
                SetValue(ScrollableWidthProperty, value);
            }
        }

        public static readonly DependencyProperty ExtentWidthProperty =
                DependencyProperty.Register(
                        "ExtentWidth",
                        typeof(double),
                        typeof(DynamicGrid),
                        new FrameworkPropertyMetadata(0d));

        public double ExtentWidth {
            get {
                return (double)GetValue(ExtentWidthProperty);
            }
            set {
                SetValue(ExtentWidthProperty, value);
            }
        }

        public static readonly DependencyProperty HorizontalOffsetProperty =
                DependencyProperty.Register(
                        "HorizontalOffset",
                        typeof(double),
                        typeof(DynamicGrid),
                        new FrameworkPropertyMetadata(0d));

        public double HorizontalOffset {
            get {
                return (double)GetValue(HorizontalOffsetProperty);
            }
            set {
                SetValue(HorizontalOffsetProperty, value);
            }
        }

        public static readonly DependencyProperty ViewportWidthProperty =
                DependencyProperty.Register(
                        "ViewportWidth",
                        typeof(double),
                        typeof(DynamicGrid),
                        new FrameworkPropertyMetadata(0d));

        public double ViewportWidth {
            get {
                return (double)GetValue(ViewportWidthProperty);
            }
            set {
                SetValue(ViewportWidthProperty, value);
            }
        }

        private bool _scrollbar = false;
        private void EnsureHorizontalScrollbar() {
            if (!_scrollbar) {

                var scrollbar = WpfHelper.FindChild<ScrollBar>(this, (bar) => bar.Name == "HorizontalScrollBar");
                if (scrollbar != null) {
                    scrollbar.Scroll += Scrollbar_Scroll; ;
                }

                _scrollbar = true;
            }
        }

        private void Scrollbar_Scroll(object sender, ScrollEventArgs e) {
            switch (e.ScrollEventType) {
                case ScrollEventType.EndScroll:
                case ScrollEventType.First:
                case ScrollEventType.LargeDecrement:
                case ScrollEventType.LargeIncrement:
                case ScrollEventType.Last:
                case ScrollEventType.SmallDecrement:
                case ScrollEventType.SmallIncrement:
                    OnViewportSizeChanged(_panelSize);
                    break;
                case ScrollEventType.ThumbPosition:
                case ScrollEventType.ThumbTrack:
                default:
                    break;
            }
        }

        #endregion
    }
}