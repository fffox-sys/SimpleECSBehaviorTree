#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace SECS.AI.BT.Editor
{
    public class BehaviorTreeEditorWindow : EditorWindow
    {
        public static BehaviorTreeEditorWindow Active { get; private set; }
        private SECS.AI.BT.BehaviorTreeAsset _asset;
        private SECS.AI.BT.Editor.Graph.BTGraphView _graphView;
        private Unity.Entities.Entity _selectedEntity;
        private PopupField<string> _entityPopup;
        private Toggle _autoRefreshToggle;
        private Toggle _showOrderToggle;
        private Toggle _focusRunningToggle;
        private string _entityPopupLabel = "<none>";
        private readonly System.Collections.Generic.List<(Unity.Entities.Entity ent, string label)> _entitiesCache = new();
        private double _lastRefreshTime;
        private Label _statsLabel;
        private bool _isDebugMode = false;
        private ScrollView _traceScrollView;
        private VisualElement _tracePanel;

        [MenuItem("AI/Behavior Tree Editor")] 
        public static void Open()
        {
            var wnd = GetWindow<BehaviorTreeEditorWindow>();
            wnd.titleContent = new GUIContent("BT Editor");
            wnd.Show();
        }

        private void OnEnable()
        {
            Active = this;
            rootVisualElement.style.flexGrow = 1;

            // === 第一行工具栏：编辑操作 ===
            var toolbar1 = new Toolbar();
            var newBtn = new ToolbarButton(CreateAsset) { text = "New", tooltip = "创建新的行为树资产" };
            var saveBtn = new ToolbarButton(Save) { text = "Save", tooltip = "保存当前行为树到资产" };
            var loadBtn = new ToolbarButton(LoadFromAsset) { text = "Load", tooltip = "从资产加载行为树" };
            var bakeBtn = new ToolbarButton(() => { Selection.activeObject = _asset; BTBakeToBlob.BakeSelectedAsset(); RefreshEntityList(); }) { text = "Bake", tooltip = "烘焙行为树为二进制Blob格式" };
            var undoBtn = new ToolbarButton(()=>Undo.PerformUndo()){ text = "Undo", tooltip = "撤销上一步操作" };
            var redoBtn = new ToolbarButton(()=>Undo.PerformRedo()){ text = "Redo", tooltip = "重做已撤销的操作" };
            var objField = new ObjectField { objectType = typeof(SECS.AI.BT.BehaviorTreeAsset), tooltip = "拖入行为树资产文件" };
            objField.RegisterValueChangedCallback(evt =>
            {
                _asset = (SECS.AI.BT.BehaviorTreeAsset)evt.newValue;
                LoadFromAsset();
            });
            toolbar1.Add(new Label("Edit编辑:") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginRight = 4 } });
            toolbar1.Add(newBtn);
            toolbar1.Add(saveBtn);
            toolbar1.Add(loadBtn);
            toolbar1.Add(bakeBtn);
            toolbar1.Add(new ToolbarSpacer());
            toolbar1.Add(undoBtn);
            toolbar1.Add(redoBtn);
            toolbar1.Add(new ToolbarSpacer());
            toolbar1.Add(new Label("Asset:"));
            toolbar1.Add(objField);
            rootVisualElement.Add(toolbar1);

            // === 第二行工具栏：调试操作 ===
            var toolbar2 = new Toolbar();
            var refreshEntitiesBtn = new ToolbarButton(RefreshEntityList) { text = "Refresh", tooltip = "刷新运行时实体列表" };
            var highlightBtn = new ToolbarButton(HighlightFromSelectedAgent) { text = "Highlight", tooltip = "高亮显示选中实体的执行状态" };
            var clearBtn = new ToolbarButton(ClearHighlight) { text = "Clear", tooltip = "清除所有高亮显示" };
            var tracePanelToggle = new Toggle("Trace") { value = false, tooltip = "显示/隐藏节点执行追踪面板" };
            tracePanelToggle.RegisterValueChangedCallback(evt => 
            {
                if (_tracePanel != null)
                {
                    _tracePanel.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                    if (evt.newValue) UpdateTracePanel();
                }
            });
            _autoRefreshToggle = new Toggle("Auto") { value = true, tooltip = "自动刷新高亮显示 (每0.2秒)" }; 
            _autoRefreshToggle.RegisterValueChangedCallback(_ => UpdateAutoRefreshRegistration());
            _showOrderToggle = new Toggle("Order") { value = true, tooltip = "显示节点执行顺序编号" };
            _focusRunningToggle = new Toggle("Focus") { value = false, tooltip = "仅突出显示运行中的分支路径" };
            _entityPopup = new PopupField<string>("Entity", new System.Collections.Generic.List<string>{_entityPopupLabel}, 0, null, null) { tooltip = "选择要调试的实体" };
            _entityPopup.RegisterValueChangedCallback(evt => OnEntityPopupChanged(evt.newValue));
            toolbar2.Add(new Label("DEBUG调试:") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginRight = 4 } });
            toolbar2.Add(_entityPopup);
            toolbar2.Add(refreshEntitiesBtn);
            toolbar2.Add(highlightBtn);
            toolbar2.Add(clearBtn);
            toolbar2.Add(new ToolbarSpacer());
            toolbar2.Add(tracePanelToggle);
            toolbar2.Add(_autoRefreshToggle);
            toolbar2.Add(_showOrderToggle);
            toolbar2.Add(_focusRunningToggle);
            rootVisualElement.Add(toolbar2);

            // 状态统计面板
            var statsPanel = new VisualElement();
            statsPanel.style.flexDirection = FlexDirection.Row;
            statsPanel.style.paddingLeft = 4;
            statsPanel.style.paddingRight = 4;
            statsPanel.style.paddingTop = 2;
            statsPanel.style.paddingBottom = 2;
            statsPanel.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            _statsLabel = new Label("统计: 等待高亮显示...");
            _statsLabel.style.fontSize = 12;
            _statsLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            _statsLabel.style.flexGrow = 1;
            statsPanel.Add(_statsLabel);
            
            // 编辑锁定状态指示器
            var lockIndicator = new Label("🔒 调试模式 - 编辑已锁定");
            lockIndicator.name = "lockIndicator";
            lockIndicator.style.fontSize = 11;
            lockIndicator.style.color = new Color(1f, 0.8f, 0.2f);
            lockIndicator.style.unityFontStyleAndWeight = FontStyle.Bold;
            lockIndicator.style.display = DisplayStyle.None; // 默认隐藏
            statsPanel.Add(lockIndicator);
            
            rootVisualElement.Add(statsPanel);

            // 主内容区域：图形视图 + Trace 面板（横向分割）
            var contentContainer = new VisualElement();
            contentContainer.style.flexGrow = 1;
            contentContainer.style.flexDirection = FlexDirection.Row;

            _graphView = new SECS.AI.BT.Editor.Graph.BTGraphView();
            _graphView.RequestSave += () => SaveGraphUndo("Graph Change");
            _graphView.style.flexGrow = 1;
            contentContainer.Add(_graphView);

            // Trace 面板（右侧，默认隐藏）
            _tracePanel = new VisualElement();
            _tracePanel.style.width = 300;
            _tracePanel.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.95f);
            _tracePanel.style.borderLeftColor = new Color(0.1f, 0.1f, 0.1f);
            _tracePanel.style.borderLeftWidth = 2;
            _tracePanel.style.paddingLeft = 8;
            _tracePanel.style.paddingRight = 8;
            _tracePanel.style.paddingTop = 8;
            _tracePanel.style.paddingBottom = 8;
            _tracePanel.style.display = DisplayStyle.None; // 默认隐藏

            var traceTitle = new Label("节点执行追踪");
            traceTitle.style.fontSize = 14;
            traceTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            traceTitle.style.marginBottom = 8;
            _tracePanel.Add(traceTitle);

            var traceAddBtn = new Button(() => AddTraceBufferToAllAgents()) { text = "添加追踪到所有AI (如缺失)" };
            traceAddBtn.style.marginBottom = 8;
            _tracePanel.Add(traceAddBtn);

            var traceCountLabel = new Label("等待选择实体...");
            traceCountLabel.name = "traceCountLabel";
            traceCountLabel.style.marginBottom = 4;
            _tracePanel.Add(traceCountLabel);

            _traceScrollView = new ScrollView(ScrollViewMode.Vertical);
            _traceScrollView.style.flexGrow = 1;
            _tracePanel.Add(_traceScrollView);

            contentContainer.Add(_tracePanel);
            rootVisualElement.Add(contentContainer);

            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDisable()
        {
            EditorApplication.update -= AutoRefreshTick;
            if (Active == this) Active = null;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        private void UpdateAutoRefreshRegistration()
        {
            EditorApplication.update -= AutoRefreshTick;
            if (_autoRefreshToggle != null && _autoRefreshToggle.value)
            {
                EditorApplication.update += AutoRefreshTick;
            }
        }

        private void AutoRefreshTick()
        {
            if (!Application.isPlaying || _asset == null || _graphView == null) return;
            if (EditorApplication.timeSinceStartup - _lastRefreshTime < BTEditorConstants.AUTO_REFRESH_INTERVAL) return;
            _lastRefreshTime = EditorApplication.timeSinceStartup;
            InternalHighlight(useStates:true, silent:true);
        }

        private void RefreshEntityList()
        {
            _entitiesCache.Clear();
            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (world == null) { _entityPopup.choices = new System.Collections.Generic.List<string>{"<no world>"}; _entityPopup.index = 0; return; }
            var em = world.EntityManager;
            using var q = em.CreateEntityQuery(Unity.Entities.ComponentType.ReadOnly<SECS.AI.BT.BTTreeRef>());
            using var ents = q.ToEntityArray(Unity.Collections.Allocator.Temp);
            var list = new System.Collections.Generic.List<string>();
            if (ents.Length == 0)
            {
                list.Add("<none>");
                _entityPopup.choices = list; _entityPopup.index = 0; _selectedEntity = Unity.Entities.Entity.Null; return;
            }
            for (int i = 0; i < ents.Length; i++)
            {
                var e = ents[i];
                bool hasTrace = em.HasBuffer<SECS.AI.BT.BTNodeTraceEntry>(e);
                string label = hasTrace ? $"Entity {e.Index}" : $"Entity {e.Index} (no-trace)";
                _entitiesCache.Add((e,label));
                list.Add(label);
            }
            _entityPopup.choices = list;
            int idx = list.FindIndex(s => _entitiesCache.Exists(p => p.ent == _selectedEntity && p.label == s));
            if (idx < 0) idx = 0;
            _entityPopup.index = idx;
            _selectedEntity = _entitiesCache.Count>idx ? _entitiesCache[idx].ent : Unity.Entities.Entity.Null;
        }

        private void OnEntityPopupChanged(string newLabel)
        {
            foreach (var (ent,label) in _entitiesCache)
            {
                if (label == newLabel) { _selectedEntity = ent; return; }
            }
            _selectedEntity = Unity.Entities.Entity.Null;
        }

        private void HighlightFromSelectedAgent()
        {
            InternalHighlight(useStates:true, silent:false);
        }

        private void ClearHighlight()
        {
            if (_graphView == null) return;
            BTGraphHighlighter.ClearHighlight(_graphView);
            SetDebugMode(false);
        }

        private void SetDebugMode(bool enabled)
        {
            _isDebugMode = enabled;
            if (_graphView != null)
            {
                // 使用精细控制：只禁用节点拖拽，保留缩放和平移
                _graphView.SetDebugMode(enabled);
            }
            
            // 显示/隐藏锁定指示器
            var lockIndicator = rootVisualElement?.Q<Label>("lockIndicator");
            if (lockIndicator != null)
            {
                lockIndicator.style.display = enabled ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void AddTraceBufferToAllAgents()
        {
            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (world == null) { EditorUtility.DisplayDialog("错误", "未找到运行时世界。请先进入 Play Mode。", "确定"); return; }
            var em = world.EntityManager;
            using var q = em.CreateEntityQuery(Unity.Entities.ComponentType.ReadOnly<SECS.AI.BT.BTTreeRef>());
            using var agents = q.ToEntityArray(Unity.Collections.Allocator.Temp);
            int added = 0;
            foreach (var e in agents)
            {
                if (!em.HasBuffer<SECS.AI.BT.BTNodeTraceEntry>(e))
                {
                    em.AddBuffer<SECS.AI.BT.BTNodeTraceEntry>(e);
                    added++;
                }
            }
            EditorUtility.DisplayDialog("完成", $"已为 {added} 个实体添加追踪缓冲区。", "确定");
        }

        private void UpdateTracePanel()
        {
            if (_traceScrollView == null || _selectedEntity == Unity.Entities.Entity.Null) return;
            
            _traceScrollView.Clear();
            
            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (world == null) return;
            var em = world.EntityManager;

            if (!em.Exists(_selectedEntity) || !em.HasBuffer<SECS.AI.BT.BTNodeTraceEntry>(_selectedEntity))
            {
                _traceScrollView.Add(new Label("实体无追踪数据"));
                return;
            }

            var trace = em.GetBuffer<SECS.AI.BT.BTNodeTraceEntry>(_selectedEntity);
            var countLabel = _tracePanel?.Q<Label>("traceCountLabel");
            if (countLabel != null)
            {
                countLabel.text = $"追踪条目数: {trace.Length}";
            }
            
            for (int i = 0; i < trace.Length; i++)
            {
                var t = trace[i];
                string stateName = t.State switch 
                { 
                    0 => "成功", 
                    1 => "失败", 
                    2 => "运行中", 
                    _ => t.State.ToString() 
                };
                
                Color stateColor = t.State switch
                {
                    0 => new Color(0.2f, 0.8f, 0.2f), 
                    1 => new Color(0.9f, 0.25f, 0.25f), 
                    2 => new Color(0.95f, 0.75f, 0.2f), 
                    _ => Color.gray
                };
                
                var entryContainer = new VisualElement();
                entryContainer.style.flexDirection = FlexDirection.Row;
                entryContainer.style.marginBottom = 2;
                entryContainer.style.paddingLeft = 4;
                entryContainer.style.paddingRight = 4;
                entryContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.5f);
                
                var indexLabel = new Label($"[{i}]");
                indexLabel.style.width = 40;
                indexLabel.style.color = Color.gray;
                
                var nodeLabel = new Label($"节点 {t.NodeIndex}");
                nodeLabel.style.flexGrow = 1;
                
                var stateLabel = new Label(stateName);
                stateLabel.style.color = stateColor;
                stateLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                stateLabel.style.width = 60;
                
                entryContainer.Add(indexLabel);
                entryContainer.Add(nodeLabel);
                entryContainer.Add(stateLabel);
                
                _traceScrollView.Add(entryContainer);
            }
        }

        private void InternalHighlight(bool useStates, bool silent)
        {
            if (_asset == null) 
            { 
                if(!silent) EditorUtility.DisplayDialog("无法高亮", 
                    "未加载行为树资产。\n\n请先：\n1. 点击 'Load' 按钮加载资产\n2. 或在下方 'Asset:' 字段拖入资产文件", 
                    "确定"); 
                return; 
            }
            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (world == null) 
            { 
                if(!silent) EditorUtility.DisplayDialog("无法高亮", 
                    "未找到运行时世界。\n\n请先进入 Play Mode 运行游戏。", 
                    "确定"); 
                return; 
            }
            var em = world.EntityManager;
            if (_selectedEntity == Unity.Entities.Entity.Null || !em.Exists(_selectedEntity))
            {
                RefreshEntityList(); // 尝试刷新一次
                if (_selectedEntity == Unity.Entities.Entity.Null) 
                { 
                    if(!silent) EditorUtility.DisplayDialog("无法高亮", 
                        "未选中实体。\n\n请先：\n1. 进入 Play Mode\n2. 点击 'Refresh Entities' 刷新列表\n3. 从 Entity 下拉列表选择目标实体", 
                        "确定"); 
                    return; 
                }
            }
            if (!em.HasBuffer<SECS.AI.BT.BTNodeTraceEntry>(_selectedEntity)) 
            { 
                if(!silent) EditorUtility.DisplayDialog("无法高亮", 
                    $"实体 {_selectedEntity.Index} 没有追踪缓冲区。\n\n可能原因：\n• 行为树尚未执行\n• 实体未启用调试追踪\n\n请稍等片刻后重试。", 
                    "确定"); 
                return; 
            }
            var trace = em.GetBuffer<SECS.AI.BT.BTNodeTraceEntry>(_selectedEntity);
            var opts = new BTGraphHighlighter.HighlightOptions{ ShowOrder = _showOrderToggle?.value ?? false, FocusRunningBranch = _focusRunningToggle?.value ?? false };
            if (!useStates)
            {
                var list = new System.Collections.Generic.List<int>(trace.Length);
                for (int i = 0; i < trace.Length; i++) list.Add(trace[i].NodeIndex);
                BTGraphHighlighter.Highlight(_graphView, _asset, list);
            }
            else
            {
                var list = new System.Collections.Generic.List<(int, byte)>(trace.Length);
                for (int i = 0; i < trace.Length; i++) list.Add((trace[i].NodeIndex, trace[i].State));
                var stats = BTGraphHighlighter.Highlight(_graphView, _asset, list, opts);
                UpdateStatsLabel(stats);
                
               
                if (_tracePanel != null && _tracePanel.style.display == DisplayStyle.Flex)
                {
                    UpdateTracePanel();
                }
            }
            
            
            SetDebugMode(true);
        }

        private void UpdateStatsLabel(BTGraphHighlighter.HighlightStats stats)
        {
            if (_statsLabel == null) return;
            _statsLabel.text = $"统计: <color=#4CAF50>✓ 成功 {stats.SuccessCount}</color>  " +
                               $"<color=#F44336>✗ 失败 {stats.FailureCount}</color>  " +
                               $"<color=#FF9800>▶ 运行中 {stats.RunningCount}</color>  " +
                               $"<color=#9E9E9E>总节点 {stats.TotalNodes}</color>";
        }

        private void CreateAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject("Create Behavior Tree", "NewBehaviorTree", "asset", "");
            if (string.IsNullOrEmpty(path)) return;
            var asset = ScriptableObject.CreateInstance<SECS.AI.BT.BehaviorTreeAsset>();
            Undo.RegisterCreatedObjectUndo(asset, "Create BehaviorTree Asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            _asset = asset;
            _graphView.Load(new SECS.AI.BT.Editor.Graph.BTGraphData());
        }

        private void Save()
        {
            if (_asset == null) return;
            SaveGraphUndo("Manual Save");
            AssetDatabase.SaveAssets();
        }

        private void SaveGraphUndo(string label)
        {
            if (_asset == null) return;
            Undo.RecordObject(_asset, label);
            var data = _graphView.Save();
            var json = JsonUtility.ToJson(data, true);
            _asset.SetSerializedGraph(json);
            EditorUtility.SetDirty(_asset);
        }

        private void LoadFromAsset()
        {
            if (_asset == null)
            {
                _graphView.Load(new SECS.AI.BT.Editor.Graph.BTGraphData());
                return;
            }
            var json = _asset.GetSerializedGraph();
            SECS.AI.BT.Editor.Graph.BTGraphData data = null;
            try
            {
                data = string.IsNullOrEmpty(json) ? new SECS.AI.BT.Editor.Graph.BTGraphData() : JsonUtility.FromJson<SECS.AI.BT.Editor.Graph.BTGraphData>(json);
            }
            catch
            {
                data = new SECS.AI.BT.Editor.Graph.BTGraphData();
            }
            _graphView.Load(data);
        }

        private void OnUndoRedoPerformed()
        {
            
            if (_asset != null && _graphView != null)
            {
                var json = _asset.GetSerializedGraph();
                SECS.AI.BT.Editor.Graph.BTGraphData data = null;
                try { data = string.IsNullOrEmpty(json) ? new SECS.AI.BT.Editor.Graph.BTGraphData() : JsonUtility.FromJson<SECS.AI.BT.Editor.Graph.BTGraphData>(json); }
                catch { data = new SECS.AI.BT.Editor.Graph.BTGraphData(); }
                _graphView.Load(data);
                Repaint(); ;
            }
        }

    }
}
#endif
