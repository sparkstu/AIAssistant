using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Wpf;

namespace WpfDemo
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer? _statusTimer;
        private bool _debugExpanded;
        private bool _suppressCheckEvent;

        // 厂商定义: (CheckBox, 面板Border, WebView2, 名称, 选中色)
        private record VendorInfo(
            CheckBox CheckBox,
            Border Panel,
            WebView2 WebView,
            string Name,
            string AccentHex
        );

        private VendorInfo[] _vendors = null!;

        public MainWindow()
        {
            InitializeComponent();
            InitVendors();
            InitializeWebViewAsync();
        }

        private void InitVendors()
        {
            _vendors = new[]
            {
                new VendorInfo(chkDoubao,   panelDoubao,   webViewDoubao,   "豆包",     "#7c3aed"),
                new VendorInfo(chkQianwen,  panelQianwen,  webViewQianwen,  "千问",     "#3b82f6"),
                new VendorInfo(chkDeepSeek, panelDeepSeek, webViewDeepSeek, "DeepSeek", "#06b6d4"),
                new VendorInfo(chkKimi,     panelKimi,     webViewKimi,     "Kimi",     "#22c55e"),
                new VendorInfo(chkChatGLM,  panelChatGLM,  webViewChatGLM,  "智谱清言", "#f59e0b"),
                new VendorInfo(chkYiyan,    panelYiyan,    webViewYiyan,    "文心一言", "#ec4899"),
            };

            // 默认选中豆包
            _suppressCheckEvent = true;
            chkDoubao.IsChecked = true;
            _suppressCheckEvent = false;
        }

        // ==================== WebView2 初始化 ====================

        private async void InitializeWebViewAsync()
        {
            SetStatus("WebView2 初始化中...", StatusType.Info);

            foreach (var v in _vendors)
                await v.WebView.EnsureCoreWebView2Async();

            RefreshLayout();
            SetStatus("就绪", StatusType.Success);
        }

        // ==================== 布局 ====================

        private void RefreshLayout()
        {
            var selected = GetSelected();
            var count = selected.Length;

            if (count == 0)
            {
                // 至少选一个
                _suppressCheckEvent = true;
                chkDoubao.IsChecked = true;
                _suppressCheckEvent = false;
                count = 1;
                selected = GetSelected();
            }

            // 隐藏所有
            foreach (var v in _vendors)
                v.Panel.Visibility = Visibility.Collapsed;

            // 清除旧的行列定义
            webViewHost.RowDefinitions.Clear();
            webViewHost.ColumnDefinitions.Clear();

            if (count == 1)
            {
                // 单端: 全屏显示
                var v = selected[0];
                v.Panel.Visibility = Visibility.Visible;
                v.Panel.SetValue(Grid.RowProperty, 0);
                v.Panel.SetValue(Grid.ColumnProperty, 0);
                tabLabel.Text = v.Name;
                targetHint.Text = $"当前: {v.Name}";
            }
            else
            {
                // 多端: 网格布局
                int cols = count <= 3 ? count : 3;
                int rows = (count + cols - 1) / cols;

                for (int c = 0; c < cols; c++)
                    webViewHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                for (int r = 0; r < rows; r++)
                    webViewHost.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                for (int i = 0; i < count; i++)
                {
                    int row = i / cols;
                    int col = i % cols;

                    // 最后一行居中: 如果最后一行不满, 偏移起始列
                    int lastRowCount = count - (rows - 1) * cols;
                    if (row == rows - 1 && lastRowCount < cols)
                    {
                        int offset = (cols - lastRowCount) / 2;
                        col += offset;
                    }

                    var v = selected[i];
                    v.Panel.Visibility = Visibility.Visible;
                    v.Panel.SetValue(Grid.RowProperty, row);
                    v.Panel.SetValue(Grid.ColumnProperty, col);
                }

                tabLabel.Text = $"{count} 端";
                targetHint.Text = $"同时发送到 {count} 个 AI: {string.Join(", ", selected.Select(v => v.Name))}";
            }
        }

        // ==================== 厂商选择 ====================

        private VendorInfo[] GetSelected()
        {
            return _vendors.Where(v => v.CheckBox.IsChecked == true).ToArray();
        }

        private void VendorCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (_suppressCheckEvent) return;

            // 确保至少选一个
            if (GetSelected().Length == 0)
            {
                _suppressCheckEvent = true;
                (sender as CheckBox)!.IsChecked = true;
                _suppressCheckEvent = false;
                return;
            }

            RefreshLayout();
            _debugExpanded = false;
            ShowDebug("");
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            _suppressCheckEvent = true;
            foreach (var v in _vendors) v.CheckBox.IsChecked = true;
            _suppressCheckEvent = false;
            RefreshLayout();
            ShowDebug("");
        }

        private void DeselectAll_Click(object sender, RoutedEventArgs e)
        {
            _suppressCheckEvent = true;
            foreach (var v in _vendors) v.CheckBox.IsChecked = false;
            chkDoubao.IsChecked = true; // 至少留一个
            _suppressCheckEvent = false;
            RefreshLayout();
            ShowDebug("");
        }

        // ==================== 状态栏 ====================

        private enum StatusType { Info, Success, Error, Warning }

        private void SetStatus(string message, StatusType type = StatusType.Info, int autoClearMs = 0)
        {
            statusBar.Text = message;
            statusBar.Foreground = type switch
            {
                StatusType.Success => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22c55e")),
                StatusType.Error => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ef4444")),
                StatusType.Warning => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f59e0b")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9898b0"))
            };

            if (autoClearMs > 0)
            {
                _statusTimer?.Stop();
                _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(autoClearMs) };
                _statusTimer.Tick += (_, _) =>
                {
                    _statusTimer.Stop();
                    if (statusBar.Text == message)
                        SetStatus("就绪", StatusType.Success);
                };
                _statusTimer.Start();
            }
        }

        private void ShowDebug(string text)
        {
            debugOutput.Text = text;
            if (!string.IsNullOrEmpty(text))
            {
                debugOutput.Visibility = Visibility.Visible;
                clearDebug.Visibility = Visibility.Visible;
                if (!_debugExpanded)
                {
                    debugOutput.MaxHeight = 18;
                    debugOutput.TextTrimming = TextTrimming.CharacterEllipsis;
                    debugOutput.TextWrapping = TextWrapping.NoWrap;
                }
            }
            else
            {
                debugOutput.Visibility = Visibility.Collapsed;
                clearDebug.Visibility = Visibility.Collapsed;
            }
        }

        private void DebugOutput_Click(object sender, MouseButtonEventArgs e)
        {
            _debugExpanded = !_debugExpanded;
            if (_debugExpanded)
            {
                debugOutput.MaxHeight = 200;
                debugOutput.TextTrimming = TextTrimming.None;
                debugOutput.TextWrapping = TextWrapping.Wrap;
                debugOutput.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a0a0c0"));
            }
            else
            {
                debugOutput.MaxHeight = 18;
                debugOutput.TextTrimming = TextTrimming.CharacterEllipsis;
                debugOutput.TextWrapping = TextWrapping.NoWrap;
                debugOutput.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6a6a8a"));
            }
        }

        private void ClearDebug_Click(object sender, MouseButtonEventArgs e)
        {
            ShowDebug("");
            _debugExpanded = false;
        }

        // ==================== 输入 ====================

        private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var hasText = !string.IsNullOrWhiteSpace(inputBox.Text);
            sendButton.Opacity = hasText ? 1.0 : 0.6;
        }

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                e.Handled = true;
                SendMessage();
            }
        }

        // ==================== 诊断 ====================

        private async void DiagnoseButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelected();
            var target = selected[0]; // 诊断第一个选中的

            if (target.WebView.CoreWebView2 == null)
            {
                SetStatus("WebView2 尚未初始化", StatusType.Warning, 3000);
                return;
            }

            diagnoseButton.IsEnabled = false;
            SetStatus($"诊断 {target.Name} 页面...", StatusType.Info);

            try
            {
                var raw = await target.WebView.CoreWebView2.ExecuteScriptAsync(DiagnoseJs);
                var json = UnescapeJsonResult(raw);
                ShowDebug(json);
                SetStatus($"{target.Name} 诊断完成", StatusType.Success, 5000);
            }
            catch (Exception ex)
            {
                ShowDebug($"异常: {ex.Message}");
                SetStatus("诊断失败", StatusType.Error, 4000);
            }
            finally
            {
                diagnoseButton.IsEnabled = true;
            }
        }

        private static string DiagnoseJs => """
            (function() {
                var info = {};
                info.title = document.title;
                info.url = window.location.href;
                info.textareas = [];
                document.querySelectorAll('textarea').forEach(function(ta) {
                    info.textareas.push({id:ta.id,placeholder:ta.placeholder,className:(ta.className||'').substring(0,80),visible:ta.offsetParent!==null});
                });
                info.editables = [];
                document.querySelectorAll('[contenteditable="true"],[contenteditable]').forEach(function(el) {
                    if(el.getAttribute('contenteditable')==='false')return;
                    info.editables.push({tag:el.tagName,id:el.id,className:(el.className||'').substring(0,80),visible:el.offsetParent!==null});
                });
                info.buttons = [];
                document.querySelectorAll('button,[role="button"]').forEach(function(b) {
                    var lbl=b.getAttribute('aria-label')||b.innerText||'';
                    if(!lbl.trim())return;
                    info.buttons.push({tag:b.tagName,label:lbl.substring(0,60),id:b.id,disabled:b.disabled,ariaDisabled:b.getAttribute('aria-disabled'),visible:b.offsetParent!==null});
                });
                return JSON.stringify(info);
            })();
            """;

        // ==================== 发送 ====================

        private async void SendMessage()
        {
            var text = inputBox.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            var targets = GetSelected();
            if (targets.Length == 0) return;

            // 检查所有目标是否初始化
            var unready = targets.Where(v => v.WebView.CoreWebView2 == null).ToArray();
            if (unready.Length > 0)
            {
                SetStatus($"「{string.Join(", ", unready.Select(v => v.Name))}」尚未初始化", StatusType.Warning, 3000);
                return;
            }

            sendButton.IsEnabled = false;
            diagnoseButton.IsEnabled = false;
            inputBox.IsEnabled = false;
            sendButton.Content = targets.Length > 1 ? $"发送×{targets.Length}" : "…";

            SetStatus($"发送到 {targets.Length} 个 AI...", StatusType.Info);
            ShowDebug("");

            var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text));
            var results = new List<string>();

            // 并发发送到所有目标
            var tasks = targets.Select(async v =>
            {
                try
                {
                    var raw = await v.WebView.CoreWebView2!.ExecuteScriptAsync(BuildSendJs(base64, v.Name));
                    var result = UnescapeJsonResult(raw);
                    return (v.Name, Result: result, Error: (string?)null);
                }
                catch (Exception ex)
                {
                    return (v.Name, Result: "", Error: ex.Message);
                }
            });

            var outcomes = await Task.WhenAll(tasks);

            // 汇总结果
            var successCount = 0;
            var failCount = 0;
            var lines = new List<string>();
            foreach (var (name, result, error) in outcomes)
            {
                if (error != null)
                {
                    failCount++;
                    lines.Add($"[{name}] 异常: {error}");
                }
                else
                {
                    successCount++;
                    lines.Add($"[{name}] {result}");
                }
            }

            ShowDebug(string.Join("\n", lines));
            inputBox.Clear();

            if (failCount == 0)
                SetStatus($"已发送到 {successCount} 个 AI", StatusType.Success, 3000);
            else
                SetStatus($"{successCount} 成功 / {failCount} 失败", StatusType.Warning, 5000);

            sendButton.IsEnabled = true;
            diagnoseButton.IsEnabled = true;
            inputBox.IsEnabled = true;
            sendButton.Content = "发送";
            inputBox.Focus();
        }

        private static string BuildSendJs(string base64Text, string tabName)
        {
            return SendJsTemplate
                .Replace("__BASE64__", base64Text)
                .Replace("__TAB__", tabName);
        }

        private static string SendJsTemplate => """
            (async function() {
                var r = { step: 'start', tab: '__TAB__' };
                var text = null;

                function wait(ms) {
                    return new Promise(function(resolve) { setTimeout(resolve, ms); });
                }

                function isVisible(el) {
                    if (!el) return false;
                    var rect = el.getBoundingClientRect();
                    return el.offsetParent !== null || (rect.width > 0 && rect.height > 0);
                }

                function dispatchInputEvent(el, type, inputType, data) {
                    try {
                        el.dispatchEvent(new InputEvent(type, {
                            bubbles: true,
                            cancelable: type === 'beforeinput',
                            composed: true,
                            inputType: inputType,
                            data: data
                        }));
                    } catch (e) {
                        el.dispatchEvent(new Event(type, { bubbles: true, cancelable: type === 'beforeinput', composed: true }));
                    }
                }

                function setNativeValue(el, value) {
                    var proto = el.tagName === 'TEXTAREA'
                        ? window.HTMLTextAreaElement.prototype
                        : window.HTMLInputElement.prototype;
                    var descriptor = Object.getOwnPropertyDescriptor(proto, 'value');
                    if (descriptor && descriptor.set) {
                        descriptor.set.call(el, value);
                    } else {
                        el.value = value;
                    }
                }

                function selectEditableContent(el, collapseToEnd) {
                    var selection = window.getSelection();
                    if (!selection) return;
                    var range = document.createRange();
                    range.selectNodeContents(el);
                    if (collapseToEnd) {
                        range.collapse(false);
                    }
                    selection.removeAllRanges();
                    selection.addRange(range);
                }

                function getInputSnapshot(el) {
                    if (!el) return '';
                    if (el.tagName === 'TEXTAREA' || el.tagName === 'INPUT') {
                        return el.value || '';
                    }
                    if (isKimiEditor(el) && typeof el.__lexicalTextContent === 'string') {
                        return el.__lexicalTextContent || '';
                    }
                    return (el.innerText || el.textContent || '').trim();
                }

                function normalizeText(value) {
                    return (value || '').replace(/\s+/g, ' ').trim();
                }

                function isKimiEditor(el) {
                    var cls = (el && el.className ? String(el.className) : '').toLowerCase();
                    return '__TAB__' === 'Kimi' || cls.includes('chat-input-editor') || cls.includes('lexical');
                }

                function buildKimiEditorStateJson(value) {
                    var lines = String(value || '').replace(/\r\n/g, '\n').split('\n');
                    var root = {
                        children: [],
                        direction: 'ltr',
                        format: '',
                        indent: 0,
                        type: 'root',
                        version: 1
                    };

                    for (var i = 0; i < lines.length; i++) {
                        var line = lines[i];
                        var paragraph = {
                            children: [],
                            direction: 'ltr',
                            format: '',
                            indent: 0,
                            type: 'paragraph',
                            version: 1,
                            textFormat: 0
                        };

                        if (line) {
                            paragraph.children.push({
                                detail: 0,
                                format: 0,
                                mode: 'normal',
                                style: '',
                                text: line,
                                type: 'text',
                                version: 1
                            });
                        }

                        root.children.push(paragraph);
                    }

                    if (root.children.length === 0) {
                        root.children.push({
                            children: [],
                            direction: 'ltr',
                            format: '',
                            indent: 0,
                            type: 'paragraph',
                            version: 1,
                            textFormat: 0
                        });
                    }

                    return JSON.stringify({ root: root });
                }

                function isButtonEnabled(el) {
                    if (!el || !isVisible(el)) return false;
                    var ariaDisabled = (el.getAttribute('aria-disabled') || '').toLowerCase();
                    var cls = (el.className || '').toString().toLowerCase();
                    return !el.disabled &&
                        ariaDisabled !== 'true' &&
                        !cls.includes('disabled') &&
                        !cls.includes('disable');
                }

                function findSendButton(input) {
                    var btn = null;
                    var btnMethod = '';
                    var selectors = [
                        '#flow-end-msg-send',
                        'button[type="submit"]',
                        '#send-message-button',
                        '.send-button',
                        'button[class*="send"]',
                        'button[class*="Send"]',
                        'button[aria-label*="send" i]',
                        'button[aria-label*="Send" i]',
                        'button[aria-label*="发送"]',
                        'div[role="button"][aria-label*="send" i]',
                        'div[role="button"][aria-label*="发送"]',
                        'div[role="button"][class*="send"]'
                    ];

                    for (var s = 0; s < selectors.length; s++) {
                        try { btn = document.querySelector(selectors[s]); } catch(e) {}
                        if (isButtonEnabled(btn)) {
                            btnMethod = 'selector:' + selectors[s].substring(0, 30);
                            return { button: btn, method: btnMethod };
                        }
                    }

                    var allBtns = document.querySelectorAll('button, div[role="button"], span[role="button"]');
                    for (var j = 0; j < allBtns.length; j++) {
                        if (!isButtonEnabled(allBtns[j])) continue;
                        var label = (allBtns[j].getAttribute('aria-label') || '').toLowerCase();
                        var inner = (allBtns[j].innerText || allBtns[j].textContent || '').toLowerCase().trim();
                        if (label.includes('send') || label.includes('发送') || inner === '发送' || inner === 'send') {
                            return { button: allBtns[j], method: 'text' };
                        }
                    }

                    var parent = input ? input.parentElement : null;
                    for (var depth = 0; depth < 6 && parent; depth++) {
                        var nearby = parent.querySelectorAll('button, div[role="button"], span[role="button"]');
                        for (var k = 0; k < nearby.length; k++) {
                            if (!isButtonEnabled(nearby[k])) continue;
                            var btnText = (nearby[k].innerText || nearby[k].textContent || '').trim();
                            if (btnText.length <= 4 && btnText.length > 0 && !/^[0-9]+$/.test(btnText) && btnText !== '×') {
                                return { button: nearby[k], method: 'nearby:text=' + btnText };
                            }
                            if (nearby[k].querySelector('svg, img, [class*="arrow"], [class*="plane"], [class*="send"]')) {
                                return { button: nearby[k], method: 'nearby:icon' };
                            }
                        }
                        parent = parent.parentElement;
                    }

                    return { button: null, method: '' };
                }

                function insertIntoEditor(input, value) {
                    input.focus();
                    input.click();

                    if (input.tagName === 'TEXTAREA' || input.tagName === 'INPUT') {
                        dispatchInputEvent(input, 'beforeinput', 'insertText', value);
                        setNativeValue(input, value);
                        input.setSelectionRange(value.length, value.length);
                        dispatchInputEvent(input, 'input', 'insertText', value);
                        input.dispatchEvent(new Event('change', { bubbles: true }));
                        return;
                    }

                    if (isKimiEditor(input)) {
                        var editor = input.__lexicalEditor;
                        if (editor && typeof editor.parseEditorState === 'function' && typeof editor.setEditorState === 'function') {
                            editor.focus();
                            editor.setEditorState(editor.parseEditorState(buildKimiEditorStateJson(value)));
                            selectEditableContent(input, true);
                            input.dispatchEvent(new Event('input', { bubbles: true, composed: true }));
                            input.dispatchEvent(new Event('change', { bubbles: true }));
                            return;
                        }

                        var paragraph = document.createElement('p');
                        paragraph.setAttribute('dir', 'ltr');
                        paragraph.textContent = value;
                        input.innerHTML = '';
                        input.appendChild(paragraph);
                        selectEditableContent(input, true);
                        input.dispatchEvent(new Event('input', { bubbles: true, composed: true }));
                        input.dispatchEvent(new Event('change', { bubbles: true }));
                        return;
                    }

                    selectEditableContent(input, false);
                    dispatchInputEvent(input, 'beforeinput', 'insertText', value);

                    var inserted = false;
                    try {
                        inserted = document.execCommand('selectAll', false, null);
                        inserted = document.execCommand('insertText', false, value) || inserted;
                    } catch (e) {}

                    if (!inserted) {
                        input.innerHTML = '';
                        input.textContent = value;
                    }

                    selectEditableContent(input, true);
                    dispatchInputEvent(input, 'input', 'insertText', value);
                    input.dispatchEvent(new Event('change', { bubbles: true }));
                }

                function sendByEnter(input) {
                    var keOpts = {
                        key: 'Enter',
                        code: 'Enter',
                        keyCode: 13,
                        which: 13,
                        bubbles: true,
                        cancelable: true,
                        composed: true
                    };
                    input.dispatchEvent(new KeyboardEvent('keydown', keOpts));
                    document.dispatchEvent(new KeyboardEvent('keydown', keOpts));
                    input.dispatchEvent(new KeyboardEvent('keypress', keOpts));
                    document.dispatchEvent(new KeyboardEvent('keypress', keOpts));
                    input.dispatchEvent(new KeyboardEvent('keyup', keOpts));
                    document.dispatchEvent(new KeyboardEvent('keyup', keOpts));
                }

                function clickSendButton(button) {
                    if (!button) return;
                    button.scrollIntoView({ block: 'center', inline: 'center' });
                    button.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, composed: true, pointerType: 'mouse' }));
                    button.dispatchEvent(new MouseEvent('mousedown', { bubbles: true, composed: true }));
                    button.dispatchEvent(new PointerEvent('pointerup', { bubbles: true, composed: true, pointerType: 'mouse' }));
                    button.dispatchEvent(new MouseEvent('mouseup', { bubbles: true, composed: true }));
                    button.dispatchEvent(new MouseEvent('click', { bubbles: true, composed: true }));
                }

                try {
                    var bytes = Uint8Array.from(atob('__BASE64__'), function(c) { return c.charCodeAt(0); });
                    text = new TextDecoder().decode(bytes);
                } catch(e) {
                    r.step = 'decode_error';
                    r.error = e.message;
                    return JSON.stringify(r);
                }
                r.textLen = text.length;

                var input = null;
                var candidates = document.querySelectorAll('textarea:not([readonly]):not([disabled]), [contenteditable="true"]:not([readonly]), div[role="textbox"], [contenteditable]:not([contenteditable="false"])');
                for (var i = 0; i < candidates.length; i++) {
                    if (isVisible(candidates[i])) {
                        input = candidates[i];
                        break;
                    }
                }
                if (!input) input = document.querySelector('textarea');
                if (!input) {
                    r.step = 'no_input';
                    r.candidates = candidates.length;
                    return JSON.stringify(r);
                }

                r.inputTag = input.tagName;
                r.inputId = input.id || '';
                r.inputClass = (input.className || '').substring(0, 80);

                insertIntoEditor(input, text);
                await wait(80);

                var snapshot = getInputSnapshot(input);
                r.step = 'text_inserted';
                r.inputSnapshot = snapshot.substring(0, 80);
                r.snapshotLen = snapshot.length;

                var normalizedSnapshot = normalizeText(snapshot);
                var normalizedText = normalizeText(text);
                if (!normalizedSnapshot) {
                    insertIntoEditor(input, text);
                    await wait(180);
                    snapshot = getInputSnapshot(input);
                    normalizedSnapshot = normalizeText(snapshot);
                    r.inputSnapshot = snapshot.substring(0, 80);
                    r.snapshotLen = snapshot.length;
                }

                r.snapshotNormalized = normalizedSnapshot.substring(0, 80);
                r.textNormalized = normalizedText.substring(0, 80);

                if (isKimiEditor(input)) {
                    sendByEnter(input);
                    r.step = 'kimi_enter';
                    r.enterTarget = input.tagName;
                    return JSON.stringify(r);
                }

                var btnInfo = { button: null, method: '' };
                for (var attempt = 0; attempt < 8; attempt++) {
                    btnInfo = findSendButton(input);
                    if (btnInfo.button) break;
                    await wait(200);
                }

                if (!btnInfo.button) {
                    sendByEnter(input);
                    r.step = 'enter_key';
                    r.enterTarget = input.tagName;
                    return JSON.stringify(r);
                }

                clickSendButton(btnInfo.button);

                r.step = 'clicked';
                r.btnMethod = btnInfo.method;
                r.btnTag = btnInfo.button.tagName;
                r.btnText = (btnInfo.button.getAttribute('aria-label') || btnInfo.button.innerText || '').substring(0, 30);
                return JSON.stringify(r);
            })();
            """;

        private static string UnescapeJsonResult(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "(空)";

            var s = raw;
            if (s.StartsWith("\"") && s.EndsWith("\""))
                s = s.Substring(1, s.Length - 2);

            s = s.Replace("\\\"", "\"");
            s = s.Replace("\\\\", "\\");
            s = s.Replace("\\n", "\n");
            s = s.Replace("\\r", "\r");
            s = s.Replace("\\t", "\t");

            return s;
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }
    }
}
