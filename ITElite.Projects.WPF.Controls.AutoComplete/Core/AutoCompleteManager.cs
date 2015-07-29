using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using ITElite.Projects.WPF.Controls.AutoComplete.Providers;
using Microsoft.Windows.Themes;

namespace ITElite.Projects.WPF.Controls.AutoComplete.Core
{
    public class ItemSelectedEventArgs : EventArgs
    {
        public string OutputText;
        public object SelectedItem;
    }

    public delegate void ItemSelectedEventHandler(object sender, ItemSelectedEventArgs args);

    public class AutoCompleteManager : IWndProcHandler
    {
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        private const int WM_NCRBUTTONDOWN = 0x00A4;
        private const int WM_SIZE = 0x0005;
        private const int WM_WINDOWPOSCHANGED = 0x0047;
        private const int POPUP_SHADOW_DEPTH = 5;
        private const int MAX_RESULTS = 50;
        /*+---------------------------------------------------------------------+
          |                                                                     |
          |                  Internal States                                    |
          |                                                                     |
          +---------------------------------------------------------------------*/

        private static readonly ResourceDictionary _resources;
        private Thread _asyncThread;
        private bool _autoSelectFirstSuggestion = true; // new added
        private SystemDropShadowChrome _chrome;
        private bool _disabled;
        private double _downHeight;
        private double _downTop;
        private double _downWidth;
        private double _itemHeight;
        private ItemSelectedEventHandler _itemSelectedEventHandler;
        private ListBox _listBox;
        private bool _manualResized;
        private int _maxResults = MAX_RESULTS;
        private FrameworkElement _ownerControl;
        private Popup _popup;
        private bool _popupOnTop = true;
        private Point _ptDown;
        private ResizeGrip _resizeGrip;
        private ScrollBar _scrollBar;
        private ScrollViewer _scrollViewer;
        private bool _supressAutoAppend;
        private string _textBeforeChangedByCode;
        private TextBox _textBox;
        private bool _textChangedByCode;
        /*+---------------------------------------------------------------------+
          |                                                                     |
          |                       Initialier                                    |
          |                                                                     |
          +---------------------------------------------------------------------*/

        static AutoCompleteManager()
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            _resources = new ResourceDictionary();
            var uri = new Uri(assemblyName + ";component/Resources/Resources.xaml", UriKind.Relative);
            _resources.Source = uri;
        }

        public AutoCompleteManager()
        {
            // default constructor
        }

        public AutoCompleteManager(TextBox textBox)
        {
            AttachTextBox(textBox);
        }

        private bool PopupOnTop
        {
            get { return _popupOnTop; }
            set
            {
                if (_popupOnTop == value)
                {
                    return;
                }
                _popupOnTop = value;
                if (_popupOnTop)
                {
                    _resizeGrip.VerticalAlignment = VerticalAlignment.Top;
                    _scrollBar.Margin = new Thickness(0, SystemParameters.HorizontalScrollBarHeight, 0, 0);
                    _resizeGrip.LayoutTransform = new ScaleTransform(1, -1);
                    _resizeGrip.Cursor = Cursors.SizeNESW;
                }
                else
                {
                    _resizeGrip.VerticalAlignment = VerticalAlignment.Bottom;
                    _scrollBar.Margin = new Thickness(0, 0, 0, SystemParameters.HorizontalScrollBarHeight);
                    _resizeGrip.LayoutTransform = Transform.Identity;
                    _resizeGrip.Cursor = Cursors.SizeNWSE;
                }
            }
        }

        public IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = false;

            switch (msg)
            {
                case WM_WINDOWPOSCHANGED:
                case WM_LBUTTONDOWN:
                case WM_RBUTTONDOWN:
                case WM_NCLBUTTONDOWN: // pass through
                case WM_NCRBUTTONDOWN:
                    PopupIsOpen = false;
                    break;
            }
            return IntPtr.Zero;
        }

        private object GetResource(string resKey)
        {
            return (_resources[resKey]);
        }

        /// <summary>
        ///     Finds immediate parent of the child control
        /// </summary>
        /// <typeparam name="T">Finds specific Type of parent control</typeparam>
        /// <param name="child">Child control in use</param>
        /// <returns></returns>
        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            //get parent item
            var parentObject = VisualTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            var parent = parentObject as T;
            return parent ?? FindParent<T>(parentObject);
        }

        public void AttachTextBox(TextBox textBox)
        {
            if (DesignerProperties.GetIsInDesignMode(textBox))
            {
                return;
            }

            Debug.Assert(_textBox == null, "One AutoCompleteManager can only attach to one control!");

            _textBox = textBox;

            var ownerWindow = Window.GetWindow(_textBox);
            if (ownerWindow != null)
            {
                _ownerControl = ownerWindow;
                ownerWindow.LocationChanged += OwnerWindow_LocationChanged;
            }
            else
            {
                _ownerControl = FindParent<UserControl>(textBox);
            }

            if (_ownerControl == null)
            {
                throw new Exception(
                    "AutoCompleteManager can only be bound with a control hosted in a Window or UserControl.");
            }

            if (_ownerControl.IsLoaded)
            {
                Initialize();
            }
            else
            {
                _ownerControl.Loaded += OwnerControl_Loaded;
            }
        }

        private void OwnerWindow_LocationChanged(object sender, EventArgs e)
        {
            _popup.IsOpen = false;
        }

        private void OwnerControl_Loaded(object sender, RoutedEventArgs e)
        {
            Initialize();
        }

        private void Initialize()
        {
            _listBox = new ListBox();
            var tempItem = new ListBoxItem {Content = "TEMP_ITEM_FOR_MEASUREMENT"};
            _listBox.Items.Add(tempItem);
            _listBox.Focusable = false;
            _listBox.Style = (Style) GetResource("AcTb_ListBoxStyle");
            _listBox.ItemContainerStyle = (Style) GetResource("AcTb_ListBoxItemStyle");
                // This is a MUST if we use local ResourceDictionary to load xaml.

            if (ItemTemplate != null)
            {
                _listBox.ItemTemplate = ItemTemplate;
            }

            if (ItemTemplateSelector != null)
            {
                _listBox.ItemTemplateSelector = ItemTemplateSelector;
            }

            _chrome = new SystemDropShadowChrome();
            _chrome.Margin = new Thickness(0, 0, POPUP_SHADOW_DEPTH, POPUP_SHADOW_DEPTH);
            _chrome.Child = _listBox;

            _popup = new Popup();
            _popup.SnapsToDevicePixels = true;
            _popup.AllowsTransparency = true;
            _popup.MinHeight = SystemParameters.HorizontalScrollBarHeight + POPUP_SHADOW_DEPTH;
            _popup.MinWidth = SystemParameters.VerticalScrollBarWidth + POPUP_SHADOW_DEPTH;
            _popup.VerticalOffset = SystemParameters.PrimaryScreenHeight + 100;
            _popup.Child = _chrome;
            _popup.IsOpen = true;

            _itemHeight = tempItem.ActualHeight;
            _listBox.Items.Clear();

            // my code
/*
            var bind = new System.Windows.Data.Binding();
            bind.RelativeSource = System.Windows.Data.RelativeSource.Self;
            bind.Converter = new AutoCompleteItemColorizer();

            var style = new Style(typeof(ListBoxItem));
            style.Setters.Add(new Setter(ListBoxItem.BackgroundProperty, bind));

//            var sett = new EventSetter(ListBoxItem.MouseDoubleClickEvent, new MouseButtonEventHandler(OnListItemMouseDoubleClick));
//            style.Setters.Add(sett);

            Resources[typeof(ListBoxItem)] = style;
            // End My code.
 */

            //
            GetInnerElementReferences();
            UpdateGripVisual();
            SetupEventHandlers();
        }

        private void GetInnerElementReferences()
        {
            _scrollViewer = (_listBox.Template.FindName("Border", _listBox) as Border).Child as ScrollViewer;
            _resizeGrip = _scrollViewer.Template.FindName("ResizeGrip", _scrollViewer) as ResizeGrip;
            _scrollBar = _scrollViewer.Template.FindName("PART_VerticalScrollBar", _scrollViewer) as ScrollBar;
        }

        private void UpdateGripVisual()
        {
            var rectSize = SystemParameters.VerticalScrollBarWidth;
            var triangle = _resizeGrip.Template.FindName("RG_TRIANGLE", _resizeGrip) as Path;
            var pg = triangle.Data as PathGeometry;
            pg = pg.CloneCurrentValue();
            var figure = pg.Figures[0];
            var p = figure.StartPoint;
            p.X = rectSize;
            figure.StartPoint = p;
            var line = figure.Segments[0] as PolyLineSegment;
            p = line.Points[0];
            p.Y = rectSize;
            line.Points[0] = p;
            p = line.Points[1];
            p.X = p.Y = rectSize;
            line.Points[1] = p;
            triangle.Data = pg;
        }

        private void SetupEventHandlers()
        {
            var ownerWindow = Window.GetWindow(_textBox);

            if (ownerWindow != null)
            {
                ownerWindow.PreviewMouseDown += OwnerWindow_PreviewMouseDown;
                ownerWindow.Deactivated += OwnerWindow_Deactivated;

                var wih = new WindowInteropHelper(ownerWindow);
                var hwndSource = HwndSource.FromHwnd(wih.Handle);

                var hwndSourceHook = new HwndSourceHook(WndProc);
                hwndSource.AddHook(hwndSourceHook);
                //hwndSource.RemoveHook(); ?                
            }
            else if (_ownerControl != null)
            {
                // Owner window is NULL, so the parent must be a UserControl. 
                // We need to deal with the UserControl's events, too.
                _ownerControl.PreviewMouseDown += OwnerWindow_PreviewMouseDown;

                var hwndSource = PresentationSource.FromVisual(_textBox) as HwndSource;
                var hwndSourceHook = new HwndSourceHook(WndProc);
                hwndSource.AddHook(hwndSourceHook);

                /* NOTE:
                 * This could happen when you use this component in a Windows Forms application.
                 * So, in addition to the above code, you need to call this class' WndProc() method in your WinForm.
                 * Or else when the parent form is resized, re-positioned, the popup window will not be closed.
                 */
            }

            _textBox.TextChanged += TextBox_TextChanged;
            _textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
            _textBox.LostFocus += TextBox_LostFocus;

            _listBox.PreviewMouseLeftButtonDown += ListBox_PreviewMouseLeftButtonDown;
            _listBox.MouseLeftButtonUp += ListBox_MouseLeftButtonUp;
            _listBox.PreviewMouseMove += ListBox_PreviewMouseMove;

            _resizeGrip.PreviewMouseLeftButtonDown += ResizeGrip_PreviewMouseLeftButtonDown;
            _resizeGrip.PreviewMouseMove += ResizeGrip_PreviewMouseMove;
            _resizeGrip.PreviewMouseUp += ResizeGrip_PreviewMouseUp;
        }

        /*+---------------------------------------------------------------------+
          |                                                                     |
          |                   TextBox Event Handling                            |
          |                                                                     |
          +---------------------------------------------------------------------*/

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_textChangedByCode || Disabled || DataProvider == null)
            {
                return;
            }
            var text = _textBox.Text;
            if (string.IsNullOrEmpty(text))
            {
                _popup.IsOpen = false;
                return;
            }

            if (Asynchronous)
            {
                if (_asyncThread != null && _asyncThread.IsAlive)
                {
                    _asyncThread.Abort();
                }

                _asyncThread = new Thread(() =>
                {
                    var items = DataProvider.GetItems(text, _maxResults);
                    var dispatcher = Application.Current.Dispatcher;
                    if (dispatcher == null)
                    {
                        dispatcher = Dispatcher.CurrentDispatcher;
                    }

                    var currentText = dispatcher.Invoke(() => _textBox.Text);
                    if (text != currentText)
                    {
                        return;
                    }
                    dispatcher.Invoke(() => PopulatePopupList(items));
                });
                _asyncThread.Start();
            }
            else
            {
                var items = DataProvider.GetItems(text, _maxResults);
                PopulatePopupList(items);
            }
        }

        private void SelectItemAndUpdateText(bool selectAll)
        {
            if (_listBox.SelectedItem == null)
            {
                return;
            }
            var text = string.Empty;
            OnItemSelected(_listBox.SelectedItem, out text);
            UpdateText(text, false);
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            _supressAutoAppend = e.Key == Key.Delete || e.Key == Key.Back;
            if (!_popup.IsOpen)
            {
                return;
            }
            if (e.Key == Key.Enter)
            {
                if (_autoSelectFirstSuggestion)
                {
                    // Enter key as selection.
                    SelectItemAndUpdateText(false);
                }

                _popup.IsOpen = false;
                _textBox.SelectAll();
            }
            else if (e.Key == Key.Escape)
            {
                PopupIsOpen = false;
                e.Handled = true;
            }
            if (!_popup.IsOpen)
            {
                return;
            }
            var index = _listBox.SelectedIndex;
            if (e.Key == Key.PageUp)
            {
                if (index == -1)
                {
                    index = _listBox.Items.Count - 1;
                }
                else if (index == 0)
                {
                    index = -1;
                }
                else if (index == _scrollBar.Value)
                {
                    index -= (int) _scrollBar.ViewportSize;
                    if (index < 0)
                    {
                        index = 0;
                    }
                }
                else
                {
                    index = (int) _scrollBar.Value;
                }
            }
            else if (e.Key == Key.PageDown)
            {
                if (index == -1)
                {
                    index = 0;
                }
                else if (index == _listBox.Items.Count - 1)
                {
                    index = -1;
                }
                else if (index == _scrollBar.Value + _scrollBar.ViewportSize - 1)
                {
                    index += (int) _scrollBar.ViewportSize - 1;
                    if (index > _listBox.Items.Count - 1)
                    {
                        index = _listBox.Items.Count - 1;
                    }
                }
                else
                {
                    index = (int) (_scrollBar.Value + _scrollBar.ViewportSize - 1);
                }
            }
            else if (e.Key == Key.Up)
            {
                if (index == -1)
                {
                    index = _listBox.Items.Count - 1;
                }
                else
                {
                    --index;
                }
            }
            else if (e.Key == Key.Down)
            {
                ++index;
            }

            if (index != _listBox.SelectedIndex)
            {
                var text = string.Empty;

                if (index < 0 || index > _listBox.Items.Count - 1)
                {
                    text = _textBeforeChangedByCode;
                    _listBox.SelectedIndex = -1;
                }
                else
                {
                    _listBox.SelectedIndex = index;
                    _listBox.ScrollIntoView(_listBox.SelectedItem);

                    OnItemSelected(_listBox.SelectedItem, out text);
                }
                UpdateText(text, false);
                e.Handled = true;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_autoSelectFirstSuggestion)
            {
                SelectItemAndUpdateText(true);
            }

            _popup.IsOpen = false;
        }

        /*+---------------------------------------------------------------------+
          |                                                                     |
          |                     ListBox Event Handling                          |
          |                                                                     |
          +---------------------------------------------------------------------*/

        private void ListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(_listBox);
            var hitTestResult = VisualTreeHelper.HitTest(_listBox, pos);
            if (hitTestResult == null)
            {
                return;
            }
            var d = hitTestResult.VisualHit;
            while (d != null)
            {
                if (d is ListBoxItem)
                {
                    e.Handled = true;
                    break;
                }
                d = VisualTreeHelper.GetParent(d);
            }
        }

        private void ListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (Mouse.Captured != null)
            {
                return;
            }
            var pos = e.GetPosition(_listBox);
            var hitTestResult = VisualTreeHelper.HitTest(_listBox, pos);
            if (hitTestResult == null)
            {
                return;
            }
            var d = hitTestResult.VisualHit;
            while (d != null)
            {
                if (d is ListBoxItem)
                {
                    var item = (d as ListBoxItem);
                    item.IsSelected = true;
//                    _listBox.ScrollIntoView(item);
                    break;
                }
                d = VisualTreeHelper.GetParent(d);
            }
        }

        private void ListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem item = null;
            var d = e.OriginalSource as DependencyObject;
            while (d != null)
            {
                if (d is ListBoxItem)
                {
                    item = d as ListBoxItem;
                    break;
                }
                d = VisualTreeHelper.GetParent(d);
            }
            if (item != null)
            {
                _popup.IsOpen = false;

                var text = string.Empty;
                OnItemSelected(item.Content, out text);
                UpdateText(text, true);
            }
        }

        /*+---------------------------------------------------------------------+
          |                                                                     |
          |                 ResizeGrip Event Handling                           |
          |                                                                     |
          +---------------------------------------------------------------------*/

        private void ResizeGrip_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _downWidth = _chrome.ActualWidth + POPUP_SHADOW_DEPTH;
            _downHeight = _chrome.ActualHeight + POPUP_SHADOW_DEPTH;
            _downTop = _popup.VerticalOffset;

            var p = e.GetPosition(_resizeGrip);
            p = _resizeGrip.PointToScreen(p);
            _ptDown = p;

            _resizeGrip.CaptureMouse();
        }

        private void ResizeGrip_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }
            var ptMove = e.GetPosition(_resizeGrip);
            ptMove = _resizeGrip.PointToScreen(ptMove);
            var dx = ptMove.X - _ptDown.X;
            var dy = ptMove.Y - _ptDown.Y;
            var newWidth = _downWidth + dx;

            if (newWidth != _popup.Width && newWidth > 0)
            {
                _popup.Width = newWidth;
            }
            if (PopupOnTop)
            {
                var bottom = _downTop + _downHeight;
                var newTop = _downTop + dy;
                if (newTop != _popup.VerticalOffset && newTop < bottom - _popup.MinHeight)
                {
                    _popup.VerticalOffset = newTop;
                    _popup.Height = bottom - newTop;
                }
            }
            else
            {
                var newHeight = _downHeight + dy;
                if (newHeight != _popup.Height && newHeight > 0)
                {
                    _popup.Height = newHeight;
                }
            }
        }

        private void ResizeGrip_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _resizeGrip.ReleaseMouseCapture();
            if (_popup.Width != _downWidth || _popup.Height != _downHeight)
            {
                _manualResized = true;
            }
        }

        /*+---------------------------------------------------------------------+
          |                                                                     |
          |                    Window Event Handling                            |
          |                                                                     |
          +---------------------------------------------------------------------*/

        private void OwnerWindow_Deactivated(object sender, EventArgs e)
        {
            _popup.IsOpen = false;
        }

        private void OwnerWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source != _textBox)
            {
                _popup.IsOpen = false;
            }
        }

        /*+---------------------------------------------------------------------+
          |                                                                     |
          |                    AcTb State And Behaviors                         |
          |                                                                     |
          +---------------------------------------------------------------------*/

        private void PopulatePopupList(IEnumerable<object> items)
        {
            var text = _textBox.Text;

            _listBox.ItemsSource = items;
            if (_listBox.Items.Count == 0)
            {
                _popup.IsOpen = false;
                return;
            }
            var firstSuggestion = _listBox.Items[0] as string;
            if (_listBox.Items.Count == 1 && text.Equals(firstSuggestion, StringComparison.OrdinalIgnoreCase))
            {
                _popup.IsOpen = false;
            }
            else
            {
                _listBox.SelectedIndex = -1;
                if (_autoSelectFirstSuggestion && _listBox.Items.Count > 0)
                {
                    _listBox.SelectedIndex = 0;
                }

                _textBeforeChangedByCode = text;
                _scrollViewer.ScrollToHome();
                ShowPopup();

                //
                if (AutoAppend && !_supressAutoAppend &&
                    _textBox.SelectionLength == 0 &&
                    _textBox.SelectionStart == _textBox.Text.Length)
                {
                    _textChangedByCode = true;
                    try
                    {
                        string appendText;
                        var appendProvider = DataProvider as IAutoAppendDataProvider;
                        if (appendProvider != null)
                        {
                            appendText = appendProvider.GetAppendText(text, firstSuggestion);
                        }
                        else
                        {
                            appendText = firstSuggestion.Substring(_textBox.Text.Length);
                        }
                        if (!string.IsNullOrEmpty(appendText))
                        {
                            _textBox.SelectedText = appendText;
                        }
                    }
                    finally
                    {
                        _textChangedByCode = false;
                    }
                }
            }
        }

        private void ShowPopup()
        {
            var popupOnTop = false;

            var p = new Point(0, _textBox.ActualHeight);
            p = _textBox.PointToScreen(p);
            var tbBottom = p.Y;

            p = new Point(0, 0);
            p = _textBox.PointToScreen(p);
            var tbTop = p.Y;

            _popup.HorizontalOffset = p.X;
            var popupTop = tbBottom;

            if (!_manualResized)
            {
                _popup.Width = _textBox.ActualWidth + POPUP_SHADOW_DEPTH;
            }

            double popupHeight;
            if (_manualResized)
            {
                popupHeight = _popup.Height;
            }
            else
            {
                var visibleCount = Math.Min(16, _listBox.Items.Count + 1);
                popupHeight = visibleCount*_itemHeight + POPUP_SHADOW_DEPTH;
            }
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            if (popupTop + popupHeight > screenHeight)
            {
                if (screenHeight - tbBottom > tbTop)
                {
                    popupHeight = SystemParameters.PrimaryScreenHeight - popupTop;
                }
                else
                {
                    popupOnTop = true;
                    popupTop = tbTop - popupHeight + 4;
                    if (popupTop < 0)
                    {
                        popupTop = 0;
                        popupHeight = tbTop + 4;
                    }
                }
            }
            PopupOnTop = popupOnTop;
            _popup.Height = popupHeight;
            _popup.VerticalOffset = popupTop;

            _popup.IsOpen = true;
        }

        private void UpdateText(string text, bool selectAll)
        {
            _textChangedByCode = true;
            _textBox.Text = text;
            if (selectAll)
            {
                _textBox.SelectAll();
            }
            else
            {
                _textBox.SelectionStart = text.Length;
            }
            _textChangedByCode = false;
        }

        protected virtual void OnItemSelected(object selectedItem, out string text)
        {
            text = selectedItem.ToString();

            var args = new ItemSelectedEventArgs
            {
                SelectedItem = selectedItem,
                OutputText = text
            };

            if (_itemSelectedEventHandler != null)
            {
                _itemSelectedEventHandler(this, args);
                text = args.OutputText;
            }
        }

        #region EVENTS

        public event ItemSelectedEventHandler ItemSelected
        {
            add
            {
                lock (this)
                {
                    _itemSelectedEventHandler += value;
                }
            }
            remove
            {
                lock (this)
                {
                    _itemSelectedEventHandler -= value;
                }
            }
        }

        #endregion

        #region PROPERTIES

        public IAutoCompleteDataProvider DataProvider { get; set; }

        public bool Disabled
        {
            get { return _disabled; }
            set
            {
                _disabled = value;
                if (_disabled && _popup != null)
                {
                    PopupIsOpen = false;
                }
            }
        }

        public bool AutoCompleting
        {
            get { return _popup.IsOpen; }
        }

        public bool Asynchronous { get; set; }

        public bool AutoAppend { get; set; }

        /// <summary>
        ///     Warning: Do NOT set this property in your window's constructor. Use Load event instead.
        /// </summary>
        public double PopupWidth
        {
            get { return _popup.Width; }
            set
            {
                _popup.Width = value;
                _manualResized = true;
            }
        }

        /// <summary>
        ///     Warning: Do NOT set this property in your window's constructor. Use Load event instead.
        /// </summary>
        public double PopupMinWidth
        {
            get { return _popup.MinWidth; }
            set { _popup.MinWidth = value; }
        }

        public int MaxResults
        {
            get { return _maxResults; }

            set { _maxResults = value; }
        }

        public DataTemplate ItemTemplate { get; set; }

        public DataTemplateSelector ItemTemplateSelector { get; set; }

        public bool PopupIsOpen
        {
            get { return (_popup != null && _popup.IsOpen); }
            set
            {
                _popup.IsOpen = value;
                if (_popup.IsOpen == false)
                {
                    _listBox.SelectedIndex = -1;
                }
            }
        }

        public bool AutoSelectFirstSuggestion
        {
            get { return _autoSelectFirstSuggestion; }
            set { _autoSelectFirstSuggestion = value; }
        }

        #endregion
    }
}