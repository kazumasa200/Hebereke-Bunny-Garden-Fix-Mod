using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UITKit.Components;

/// <summary>
/// ラベル付きドロップダウン行。横並び [label(flex)] [value ▼]。
/// 値ボタン or 行クリックでポップアップを開き、選択肢一覧から選ぶ。クリック外 / Esc で閉じる。
/// キーボード操作向けに <see cref="Cycle"/> 公開（ポップアップは開かず即座に値を進める）。
/// </summary>
public class UITDropdown : VisualElement
{
    /// <summary>選択 index が変わるたびに発火（ポップアップ選択 / Cycle / SetIndex(notify:true) で）。</summary>
    public event Action<int> OnValueChanged;

    public int Index { get; private set; }
    public IReadOnlyList<string> Options => m_options;
    public bool IsPopupOpen => m_scrim != null && m_scrim.parent != null;

    // 同時に複数の dropdown ポップアップが開いた状態を許さない。
    // 別の dd ボタンを直接クリックしたときに前のポップアップ＋scrim が孤児化するのを防ぐ。
    private static UITDropdown s_openInstance;

    private Label m_label;
    private VisualElement m_button;
    private Label m_valueLabel;
    private Label m_caret;
    private string[] m_options = Array.Empty<string>();
    private Font m_font;

    // ポップアップ関連（パネルの visualTree 直下に追加し、行のクリッピングを跨ぐ）
    private VisualElement m_scrim;
    private VisualElement m_popup;

    private const float kButtonMinWidth = 120f;
    private static readonly Color kLabelColor = new(0.84f, 0.87f, 0.91f, 1f);
    private static readonly Color kButtonBg = new(0.20f, 0.23f, 0.31f, 1f); // #333a50
    private static readonly Color kButtonBgHover = new(0.25f, 0.29f, 0.39f, 1f);
    private static readonly Color kPopupBg = new(0.115f, 0.135f, 0.182f, 0.98f);
    private static readonly Color kPopupItemBgHover = new(0.22f, 0.34f, 0.55f, 1f);
    private static readonly Color kPopupItemBgSelected = new(0.18f, 0.28f, 0.45f, 1f);

    public UITDropdown()
    {
        BuildLayout();
    }

    private void BuildLayout()
    {
        style.flexDirection = FlexDirection.Row;
        style.alignItems = Align.Center;
        style.flexGrow = 1;

        m_label = new Label
        {
            style =
            {
                color = kLabelColor,
                fontSize = 10,
                flexGrow = 1,
            },
            pickingMode = PickingMode.Ignore,
        };
        Add(m_label);

        m_button = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                alignItems = Align.Center,
                minWidth = kButtonMinWidth,
                paddingLeft = 8,
                paddingRight = 6,
                paddingTop = 2,
                paddingBottom = 2,
                backgroundColor = kButtonBg,
                borderTopLeftRadius = 3,
                borderTopRightRadius = 3,
                borderBottomLeftRadius = 3,
                borderBottomRightRadius = 3,
                flexShrink = 0,
            },
        };
        m_button.RegisterCallback<MouseEnterEvent>(_ => m_button.style.backgroundColor = kButtonBgHover);
        m_button.RegisterCallback<MouseLeaveEvent>(_ => m_button.style.backgroundColor = kButtonBg);
        m_button.RegisterCallback<ClickEvent>(evt =>
        {
            TogglePopup();
            evt.StopPropagation();
        });
        Add(m_button);

        m_valueLabel = new Label
        {
            style =
            {
                color = Color.white,
                fontSize = 10,
                flexGrow = 1,
                unityTextAlign = TextAnchor.MiddleLeft,
            },
            pickingMode = PickingMode.Ignore,
        };
        m_button.Add(m_valueLabel);

        m_caret = new Label("▼")
        {
            style =
            {
                color = new Color(0.70f, 0.74f, 0.82f, 1f),
                fontSize = 9,
                marginLeft = 6,
                unityTextAlign = TextAnchor.MiddleCenter,
            },
            pickingMode = PickingMode.Ignore,
        };
        m_button.Add(m_caret);

        // 自要素が panel から外れた瞬間に scrim/popup を確実に回収する。
        // visualTree 直下に貼った scrim は親子関係上自動破棄されないため、ここで明示的に閉じる。
        RegisterCallback<DetachFromPanelEvent>(_ => ClosePopup());
    }

    public void Setup(string label, IReadOnlyList<string> options, Font font = null)
    {
        m_label.text = label ?? string.Empty;
        m_font = font;
        if (font != null)
        {
            m_label.style.unityFont = font;
            m_valueLabel.style.unityFont = font;
        }
        m_options = options == null ? Array.Empty<string>() : System.Linq.Enumerable.ToArray(options);
        Index = 0;
        RefreshValueLabel();
    }

    /// <summary>イベント発火なしで index を反映する初期値設定用。</summary>
    public void SetIndex(int idx, bool notify = false)
    {
        if (m_options.Length == 0) return;
        if (idx < 0) idx = 0;
        if (idx >= m_options.Length) idx = m_options.Length - 1;
        var changed = idx != Index;
        Index = idx;
        RefreshValueLabel();
        if (changed && notify) OnValueChanged?.Invoke(Index);
    }

    /// <summary>delta だけ index を進める（端到達で wrap）。常に OnValueChanged を発火。キーボード ←→ 用。</summary>
    public void Cycle(int delta)
    {
        if (m_options.Length == 0) return;
        var len = m_options.Length;
        // C# の % は負数で負を返すため、(% + len) % len で正に正規化。
        var next = ((Index + delta) % len + len) % len;
        if (next == Index)
        {
            RefreshValueLabel();
            return;
        }
        Index = next;
        RefreshValueLabel();
        OnValueChanged?.Invoke(Index);
    }

    public void TogglePopup()
    {
        if (IsPopupOpen) ClosePopup();
        else OpenPopup();
    }

    public void OpenPopup()
    {
        if (m_options.Length == 0) return;
        var rootPanel = panel?.visualTree;
        if (rootPanel == null) return;
        if (IsPopupOpen) return;

        // 既に他の dd が開いていれば先に閉じる（同時 open 不可）。
        if (s_openInstance != null && !ReferenceEquals(s_openInstance, this))
            s_openInstance.ClosePopup();

        // クリック外で閉じるための透明 scrim を全画面に貼る。
        // popup は scrim の子にすることで、popup 内クリックは scrim へ伝播しない（兄弟構造より確実）。
        m_scrim = new VisualElement
        {
            style =
            {
                position = Position.Absolute,
                left = 0,
                top = 0,
                right = 0,
                bottom = 0,
                backgroundColor = new Color(0, 0, 0, 0),
            },
        };
        m_scrim.RegisterCallback<ClickEvent>(evt =>
        {
            ClosePopup();
            evt.StopPropagation();
        });
        rootPanel.Add(m_scrim);

        m_popup = new VisualElement
        {
            style =
            {
                position = Position.Absolute,
                backgroundColor = kPopupBg,
                borderTopLeftRadius = 4,
                borderTopRightRadius = 4,
                borderBottomLeftRadius = 4,
                borderBottomRightRadius = 4,
                paddingTop = 4,
                paddingBottom = 4,
                paddingLeft = 0,
                paddingRight = 0,
                borderLeftWidth = 1, borderRightWidth = 1, borderTopWidth = 1, borderBottomWidth = 1,
                borderLeftColor = new Color(0.30f, 0.34f, 0.42f, 1f),
                borderRightColor = new Color(0.30f, 0.34f, 0.42f, 1f),
                borderTopColor = new Color(0.30f, 0.34f, 0.42f, 1f),
                borderBottomColor = new Color(0.30f, 0.34f, 0.42f, 1f),
                // 配置確定（PositionPopup）まで非表示にしておくと初回 (0,0) のちらつきを回避できる。
                visibility = Visibility.Hidden,
            },
        };
        m_scrim.Add(m_popup);
        s_openInstance = this;

        for (int i = 0; i < m_options.Length; i++)
        {
            var idx = i;
            var item = new Label(m_options[i])
            {
                style =
                {
                    color = Color.white,
                    fontSize = 10,
                    paddingLeft = 10,
                    paddingRight = 10,
                    paddingTop = 4,
                    paddingBottom = 4,
                    backgroundColor = (idx == Index) ? kPopupItemBgSelected : new Color(0, 0, 0, 0),
                    unityTextAlign = TextAnchor.MiddleLeft,
                },
            };
            if (m_font != null) item.style.unityFont = m_font;
            item.RegisterCallback<MouseEnterEvent>(_ => item.style.backgroundColor = kPopupItemBgHover);
            item.RegisterCallback<MouseLeaveEvent>(_ => item.style.backgroundColor = (idx == Index) ? kPopupItemBgSelected : new Color(0, 0, 0, 0));
            item.RegisterCallback<ClickEvent>(evt =>
            {
                ClosePopup();
                if (idx != Index)
                {
                    Index = idx;
                    RefreshValueLabel();
                    OnValueChanged?.Invoke(Index);
                }
                evt.StopPropagation();
            });
            m_popup.Add(item);
        }

        // 配置: ボタン直下に表示。worldBound はレイアウト確定後でないと 0 のため
        // schedule で次フレームに位置決め＆画面端はみ出し補正を行う。
        m_popup.schedule.Execute(PositionPopup).StartingIn(0);
    }

    public void ClosePopup()
    {
        // popup は scrim の子なので scrim を外せば popup も巻き取られる。順序は二重 Remove 防止のため保つ。
        if (m_popup != null && m_popup.parent != null) m_popup.parent.Remove(m_popup);
        if (m_scrim != null && m_scrim.parent != null) m_scrim.parent.Remove(m_scrim);
        m_popup = null;
        m_scrim = null;
        if (ReferenceEquals(s_openInstance, this)) s_openInstance = null;
    }

    private void PositionPopup()
    {
        if (m_popup == null) return;
        var btnRect = m_button.worldBound;
        var popupHeight = m_popup.resolvedStyle.height;
        if (float.IsNaN(popupHeight) || popupHeight <= 0f) popupHeight = m_popup.layout.height;

        var width = Mathf.Max(btnRect.width, kButtonMinWidth);
        m_popup.style.minWidth = width;

        var left = btnRect.xMin;
        var top = btnRect.yMax + 2f;

        // 画面端はみ出し補正は parent (scrim) の panel 座標系を使う。
        // Screen.width/height は panel 座標系と PanelSettings.scale で乖離するためフォールバックは置かない。
        var parentRect = panel?.visualTree?.worldBound ?? default;
        if (parentRect.width > 0f && parentRect.height > 0f)
        {
            if (!float.IsNaN(popupHeight) && top + popupHeight > parentRect.yMax)
                top = btnRect.yMin - popupHeight - 2f;
            if (left + width > parentRect.xMax) left = parentRect.xMax - width;
            if (left < parentRect.xMin) left = parentRect.xMin;
        }

        m_popup.style.left = left;
        m_popup.style.top = top;
        m_popup.style.visibility = Visibility.Visible;
    }

    /// <summary>
    /// ドロップダウン全体の幅を固定する（0 以下なら可変に戻す）。
    /// 内部 m_button の minWidth も上書きして kButtonMinWidth 制約を解除する。
    /// </summary>
    public UITDropdown SetWidth(float width)
    {
        if (width > 0)
        {
            style.width = width;
            style.minWidth = width;
            style.maxWidth = width;
            style.flexGrow = 0;
            style.flexShrink = 0;
            // m_button は default では auto width のため、親 (UITDropdown) の幅と合わない。
            // 明示的に同じ width に揃えて minWidth=0 で kButtonMinWidth 制約を解除する。
            m_button.style.width = width;
            m_button.style.minWidth = 0;
            m_button.style.maxWidth = width;
        }
        else
        {
            style.width = StyleKeyword.Auto;
            style.minWidth = StyleKeyword.Auto;
            style.maxWidth = StyleKeyword.Auto;
            style.flexGrow = 1;
            style.flexShrink = 1;
            m_button.style.width = StyleKeyword.Auto;
            m_button.style.minWidth = kButtonMinWidth;
            m_button.style.maxWidth = StyleKeyword.Auto;
        }
        return this;
    }

    /// <summary>内部の値ラベルとキャレットのフォントサイズを変更する（幅固定時の長テキスト対策）。</summary>
    public UITDropdown SetButtonFontSize(int px)
    {
        if (m_valueLabel != null) m_valueLabel.style.fontSize = px;
        if (m_caret != null) m_caret.style.fontSize = Mathf.Max(6, px - 2);
        return this;
    }

    private void RefreshValueLabel()
    {
        m_valueLabel.text = m_options.Length == 0 ? "-" : m_options[Index];
    }
}
